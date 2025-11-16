using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Text;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize]
    [Route("payments")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _email;
        private readonly StripeClient _stripe;
        private readonly StripeOptions _opts;
        private readonly ILogger<PaymentsController> _log;
        private readonly IConfiguration _cfg;

        public PaymentsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users,
            IEmailSender email,
            StripeClient stripe,
            IOptions<StripeOptions> opts,
            ILogger<PaymentsController> log,
            IConfiguration cfg)
        {
            _db = db; _users = users; _email = email; _stripe = stripe;
            _opts = opts.Value; _log = log; _cfg = cfg;
        }

        [HttpPost("tenant/checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TenantCheckout(Guid billId, decimal? amount = null)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var bill = await _db.TenantBills
                .Include(b => b.Flat)!.ThenInclude(f => f.Building)
                .FirstOrDefaultAsync(b => b.Id == billId && b.TenantUserId == me.Id);
            if (bill == null) return NotFound("Bill not found.");

            var paidNow = await _db.TenantPayments
                .Where(p => p.TenantBillId == bill.Id && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var dueNow = bill.Amount - paidNow;
            if (dueNow <= 0m)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No due on this bill.";
                return RedirectToAction("Bills", "TenantPortal");
            }

            var take = Math.Min(amount ?? dueNow, dueNow);
            if (take <= 0m)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Nothing to pay.";
                return RedirectToAction("Bills", "TenantPortal");
            }

            await tx.CommitAsync();

            var cents = (long)Math.Round(take * 100m, MidpointRounding.AwayFromZero);
            //var currency = string.IsNullOrWhiteSpace(_opts.Currency) ? "usd" : _opts.Currency.ToLowerInvariant();
            var currency = string.IsNullOrWhiteSpace(_opts.Currency) ? "bdt" : _opts.Currency.ToLowerInvariant();

            var sessionService = new SessionService(_stripe);
            var successUrl = Url.Action(nameof(Success), "Payments", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = Url.Action(nameof(Cancel), "Payments", null, Request.Scheme);

            var meta = new Dictionary<string, string>
            {
                ["kind"] = "tenant",
                ["tenantBillId"] = bill.Id.ToString(),
                ["tenantUserId"] = me.Id,
            };

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                CustomerEmail = me.Email,
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = cents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Rent — {bill.Title} ({bill.BillDate:yyyy-MM})",
                                Description = $"{bill.Flat!.Building!.Name} / Flat {bill.Flat!.FlatNumber}"
                            }
                        }
                    }
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Description = $"Rent {bill.BillDate:yyyy-MM} — {bill.Title}",
                    Metadata = meta
                },
                Metadata = meta
            };

            var session = await sessionService.CreateAsync(options);
            return Redirect(session.Url);
        }

        [HttpPost("owner/checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OwnerCheckout(Guid commonBillId)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var isAdmin = User.IsInRole("President") || User.IsInRole("SuperAdmin");
            var ownerId = isAdmin ? Request.Form["ownerId"].FirstOrDefault() ?? me.Id : me.Id;

            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var alloc = await _db.ExpenseAllocations
                .Include(a => a.CommonBill)!.ThenInclude(cb => cb.Building)
                .Where(a => a.CommonBillId == commonBillId && a.OwnerId == ownerId)
                .FirstOrDefaultAsync();
            if (alloc == null) return NotFound("Allocation not found.");

            var paid = await _db.ExpenseAllocationPayments
                .Where(p => p.CommonBillId == commonBillId && p.OwnerId == ownerId && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var due = alloc.AmountDue - paid;
            if (due <= 0m)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No due on this bill.";
                return RedirectToAction("View", "OwnerBilling", new { ownerId });
            }

            await tx.CommitAsync();

            var cents = (long)Math.Round(due * 100m, MidpointRounding.AwayFromZero);
            //var currency = string.IsNullOrWhiteSpace(_opts.Currency) ? "usd" : _opts.Currency.ToLowerInvariant();
            var currency = string.IsNullOrWhiteSpace(_opts.Currency) ? "bdt" : _opts.Currency.ToLowerInvariant();

            var sessionService = new SessionService(_stripe);
            var successUrl = Url.Action(nameof(Success), "Payments", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = Url.Action(nameof(Cancel), "Payments", null, Request.Scheme);

            var meta = new Dictionary<string, string>
            {
                ["kind"] = "owner",
                ["ownerId"] = ownerId,
                ["commonBillId"] = alloc.CommonBillId.ToString()
            };

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                CustomerEmail = me.Email,
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = cents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Common Bill — {alloc.CommonBill!.Name}",
                                Description = $"{alloc.CommonBill!.Building!.Name} — Owner payment"
                            }
                        }
                    }
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Description = $"Common Bill — {alloc.CommonBill!.Name}",
                    Metadata = meta
                },
                Metadata = meta
            };

            var session = await sessionService.CreateAsync(options);
            return Redirect(session.Url);
        }

        // -----------------------
        // Simple result pages
        // -----------------------
        [HttpGet("success")]
        public async Task<IActionResult> Success([FromQuery(Name = "session_id")] string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return View(model: null);

            var sessionSvc = new SessionService(_stripe);
            var session = await sessionSvc.GetAsync(sessionId);

            string? piId = session.PaymentIntentId ?? session.PaymentIntent?.Id;
            string? status = null;

            if (!string.IsNullOrWhiteSpace(piId))
            {
                var piSvc = new PaymentIntentService(_stripe);
                var pi = await piSvc.GetAsync(piId);
                status = pi.Status;
            }

            var vm = new CheckoutResultVm
            {
                SessionId = session.Id,
                PaymentIntentId = piId,
                Status = status,
                Amount = (session.AmountTotal ?? 0) / 100m,
                Currency = (session.Currency ?? "usd").ToUpperInvariant()
            };

            return View(vm);
        }

        [HttpGet("cancel")]
        public IActionResult Cancel() => View();

        // -----------------------
        // STRIPE WEBHOOK
        // -----------------------
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var endpointSecret = _cfg["Stripe:WebhookSecret"];
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            Event stripeEvent;

            try
            {
                var sig = Request.Headers["Stripe-Signature"];
                stripeEvent = EventUtility.ConstructEvent(json, sig, endpointSecret);
                _log.LogInformation("Stripe webhook received: {Type} ({Id})", stripeEvent.Type, stripeEvent.Id);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Stripe webhook signature verification failed.");
                return BadRequest();
            }

            try
            {
                var t = stripeEvent.Type?.ToLowerInvariant();

                if (t == "payment_intent.succeeded")
                {
                    var intent = (PaymentIntent)stripeEvent.Data.Object;
                    await HandlePaymentIntentSucceeded(intent);
                }
                else if (t == "checkout.session.completed")
                {
                    var session = (Stripe.Checkout.Session)stripeEvent.Data.Object;
                    await HandleCheckoutSessionCompleted(session);
                }
                else
                {
                    _log.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing Stripe event {Id}", stripeEvent.Id);
                return StatusCode(500);
            }

            return Ok();
        }

        private async Task HandleCheckoutSessionCompleted(Stripe.Checkout.Session session)
        {
            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                _log.LogInformation("Checkout session not paid. Status: {Status}", session.PaymentStatus);
                return;
            }

            string? piId = session.PaymentIntentId;
            if (string.IsNullOrWhiteSpace(piId) && session.PaymentIntent != null)
                piId = session.PaymentIntent.Id;

            if (string.IsNullOrWhiteSpace(piId))
            {
                _log.LogWarning("checkout.session.completed without PaymentIntent id.");
                return;
            }

            var amount = (session.AmountTotal ?? 0) / 100m;

            IDictionary<string, string> meta = session.Metadata ?? new Dictionary<string, string>();
            if (!meta.ContainsKey("kind"))
            {
                var piSvc = new PaymentIntentService(_stripe);
                var intent = await piSvc.GetAsync(piId);
                meta = intent.Metadata ?? meta;
                if (meta.ContainsKey("kind"))
                {
                    long cents = intent.AmountReceived;
                    if (cents <= 0) cents = intent.Amount;
                    if (amount <= 0) amount = cents / 100m;
                }
            }

            await ProcessPaymentFromMetaAsync(meta, amount, piId);
        }

        private async Task HandlePaymentIntentSucceeded(PaymentIntent intent)
        {
            long cents = intent.AmountReceived;
            if (cents <= 0) cents = intent.Amount;
            var amountReceived = cents / 100m;

            var meta = intent.Metadata ?? new Dictionary<string, string>();
            await ProcessPaymentFromMetaAsync(meta, amountReceived, intent.Id);
        }

        private async Task SendTenantPaymentEmail(string tenantUserId, IEnumerable<TenantPayment> payments)
        {
            var user = await _users.FindByIdAsync(tenantUserId);
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

            var list = payments.ToList();
            if (list.Count == 0) return;

            var billIds = list.Select(x => x.TenantBillId).ToList();
            var bills = await _db.TenantBills
                .AsNoTracking()
                .Include(x => x.Flat)!.ThenInclude(f => f.Building)
                .Where(x => billIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var rows = new StringBuilder();
            foreach (var p in list)
            {
                var b = bills[p.TenantBillId];
                rows.AppendLine($@"<tr>
                    <td>{b.Title}</td><td>{b.BillDate:yyyy-MM-dd}</td>
                    <td style=""text-align:right"">{p.Amount:C}</td><td>{(string.IsNullOrWhiteSpace(p.Reference) ? "-" : p.Reference)}</td>
                </tr>");
            }

            var total = list.Sum(x => x.Amount);
            var html = $@"
                <p>Hello {System.Net.WebUtility.HtmlEncode(user.Fullname ?? user.UserName)},</p>
                <p>We’ve recorded your rent payment{(list.Count > 1 ? "s" : "")}:</p>
                <table cellpadding=""6"" border=""1"" style=""border-collapse:collapse;"">
                    <thead><tr><th>Bill</th><th>Bill Date</th><th>Amount</th><th>Reference</th></tr></thead>
                    <tbody>{rows}</tbody>
                    <tfoot><tr><td colspan=""2"" style=""text-align:right""><strong>Total</strong></td>
                    <td style=""text-align:right""><strong>{total:C}</strong></td><td></td></tr></tfoot>
                </table>
                <p>Thank you.</p>";
            await _email.SendEmailAsync(user.Email!, "Rent payment receipt", html);
        }

        private async Task SendOwnerPaymentEmail(string ownerUserId, IEnumerable<ExpenseAllocationPayment> payments)
        {
            var user = await _users.FindByIdAsync(ownerUserId);
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

            var list = payments.ToList();
            if (list.Count == 0) return;

            var commonBillIds = list.Select(x => x.CommonBillId).Distinct().ToList();
            var bills = await _db.CommonBills
                .AsNoTracking()
                .Include(cb => cb.Building)
                .Where(cb => commonBillIds.Contains(cb.Id))
                .ToDictionaryAsync(cb => cb.Id);

            var rows = new StringBuilder();
            foreach (var p in list)
            {
                var cb = bills[p.CommonBillId];
                rows.AppendLine($@"<tr>
                    <td>{cb.Name}</td><td>{cb.BillDate:yyyy-MM-dd}</td>
                    <td style=""text-align:right"">{p.Amount:C}</td><td>{(string.IsNullOrWhiteSpace(p.Reference) ? "-" : p.Reference)}</td>
                </tr>");
            }

            var total = list.Sum(x => x.Amount);
            var html = $@"
                <p>Hello {System.Net.WebUtility.HtmlEncode(user.Fullname ?? user.UserName)},</p>
                <p>We’ve recorded your common bill payment{(list.Count > 1 ? "s" : "")}:</p>
                <table cellpadding=""6"" border=""1"" style=""border-collapse:collapse;"">
                    <thead><tr><th>Bill</th><th>Bill Date</th><th>Amount</th><th>Reference</th></tr></thead>
                    <tbody>{rows}</tbody>
                    <tfoot><tr><td colspan=""2"" style=""text-align:right""><strong>Total</strong></td>
                    <td style=""text-align:right""><strong>{total:C}</strong></td><td></td></tr></tfoot>
                </table>
                <p>Thank you.</p>";
            await _email.SendEmailAsync(user.Email!, "Common bill payment receipt", html);
        }

        // Centralized post-payment reconciliation for both tenant & owner paths
        private async Task ProcessPaymentFromMetaAsync(
            IDictionary<string, string> meta,
            decimal amountReceived,
            string paymentRef)
        {
            if (!meta.TryGetValue("kind", out var kind) || string.IsNullOrWhiteSpace(kind))
                return;

            // ---- Tenant rent ----
            if (string.Equals(kind, "tenant", StringComparison.OrdinalIgnoreCase) &&
                meta.TryGetValue("tenantBillId", out var billIdStr) &&
                Guid.TryParse(billIdStr, out var billId))
            {
                await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                var exists = await _db.TenantPayments.AnyAsync(p => p.IdempotencyKey == paymentRef);
                if (exists) { await tx.RollbackAsync(); return; }

                var bill = await _db.TenantBills
                    .Include(b => b.Flat)
                    .FirstOrDefaultAsync(b => b.Id == billId);
                if (bill == null) { await tx.RollbackAsync(); return; }

                var paid = await _db.TenantPayments
                    .Where(p => p.TenantBillId == bill.Id && p.Status == PaymentStatus.Succeeded)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var dueNow = bill.Amount - paid;
                var take = Math.Min(amountReceived, dueNow);
                if (take <= 0m) { await tx.RollbackAsync(); return; }

                var entity = new TenantPayment
                {
                    TenantBillId = bill.Id,
                    Amount = take,
                    PaymentDate = DateTime.Today,
                    Reference = $"Stripe {paymentRef}",
                    IdempotencyKey = paymentRef,
                    ExternalRef = paymentRef,
                    Gateway = PaymentGateway.Stripe,
                    Status = PaymentStatus.Succeeded
                };

                await _db.TenantPayments.AddAsync(entity);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await SendTenantPaymentEmail(bill.TenantUserId, new[] { entity });
            }
            // ---- Owner common bill ----
            else if (string.Equals(kind, "owner", StringComparison.OrdinalIgnoreCase) &&
                     meta.TryGetValue("ownerId", out var ownerId) &&
                     meta.TryGetValue("commonBillId", out var cbStr) &&
                     Guid.TryParse(cbStr, out var commonBillId))
            {
                await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                var exists = await _db.ExpenseAllocationPayments.AnyAsync(p => p.IdempotencyKey == paymentRef);
                if (exists) { await tx.RollbackAsync(); return; }

                var alloc = await _db.ExpenseAllocations
                    .Include(a => a.CommonBill)!.ThenInclude(cb => cb.Building)
                    .FirstOrDefaultAsync(a => a.CommonBillId == commonBillId && a.OwnerId == ownerId);
                if (alloc == null) { await tx.RollbackAsync(); return; }

                var paid = await _db.ExpenseAllocationPayments
                    .Where(p => p.CommonBillId == commonBillId && p.OwnerId == ownerId && p.Status == PaymentStatus.Succeeded)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var dueNow = alloc.AmountDue - paid;
                var take = Math.Min(amountReceived, dueNow);
                if (take <= 0m) { await tx.RollbackAsync(); return; }

                var e = new ExpenseAllocationPayment
                {
                    ExpenseAllocationId = alloc.Id,
                    CommonBillId = commonBillId,
                    OwnerId = ownerId,
                    Amount = take,
                    PaymentDate = DateTime.Today,
                    Reference = $"Stripe {paymentRef}",
                    IdempotencyKey = paymentRef,
                    ExternalRef = paymentRef,
                    Gateway = PaymentGateway.Stripe,
                    Status = PaymentStatus.Succeeded,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _db.ExpenseAllocationPayments.AddAsync(e);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await SendOwnerPaymentEmail(ownerId, new[] { e });
            }
        }
    }
}