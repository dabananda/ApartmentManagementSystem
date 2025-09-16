(function (global) {
    if (!global) return;

    function toast(icon, title, ms) {
        if (!global.Swal || !title) return;
        global.Swal.fire({
            toast: true,
            icon: icon || "info",
            title,
            position: "top-end",
            showConfirmButton: false,
            timer: ms || 4000,
            timerProgressBar: true
        });
    }

    // Guard that at least one checkbox is selected inside a form before submitting
    function requireSelectionGuard(opts) {
        if (!opts) return null;
        const { formId, checkboxSelector, emptyMessage } = opts;
        return function (btn) {
            const form = btn && btn.form;
            if (!form) return true;
            const isTargetForm =
                (formId && form.id === formId) ||
                (!formId && true); // if no formId, just check the nearest form

            if (!isTargetForm) return true;
            const any = !!form.querySelector(`${checkboxSelector}:checked`);
            if (!any) toast("error", emptyMessage || "Select at least one item.", 3000);
            return any;
        };
    }

    function submitWithButton(btn) {
        const form = btn.form;
        if (!form) return;

        const origAction = form.getAttribute("action");
        const origMethod = (form.getAttribute("method") || "get");

        form.setAttribute("action", btn.formAction || origAction || global.location.href);
        form.setAttribute("method", (btn.formMethod || "post").toLowerCase());

        // Preserve button name/value (e.g., role)
        let tmp;
        if (btn.name && btn.value) {
            tmp = document.createElement("input");
            tmp.type = "hidden"; tmp.name = btn.name; tmp.value = btn.value;
            form.appendChild(tmp);
        }

        form.submit();

        // Restore (helps if client-side nav keeps the DOM around)
        if (tmp) tmp.remove();
        if (origAction !== null) form.setAttribute("action", origAction); else form.removeAttribute("action");
        form.setAttribute("method", origMethod);
    }

    function confirmAndSubmit(btn, cfg) {
        if (!global.Swal) { submitWithButton(btn); return; }
        global.Swal.fire({
            icon: cfg.icon || "warning",
            title: cfg.title || "Are you sure?",
            text: cfg.text || "",
            showCancelButton: true,
            reverseButtons: true,
            confirmButtonText: cfg.confirmText || "Yes",
            cancelButtonText: cfg.cancelText || "Cancel",
            confirmButtonColor: cfg.confirmColor || (cfg.icon === "warning" ? "#d33" : undefined)
        }).then(res => { if (res.isConfirmed) submitWithButton(btn); });
    }

    // Public: bind a click-confirm handler on any matching buttons
    function bindConfirm(selector, options) {
        const guard = requireSelectionGuard(options && options.requireSelection);

        document.addEventListener("click", function (ev) {
            const btn = ev.target && ev.target.closest(selector);
            if (!btn) return;
            // If a selection is required, enforce it
            if (guard && !guard(btn)) { ev.preventDefault(); return; }

            ev.preventDefault();
            confirmAndSubmit(btn, options || {});
        });
    }

    global.SwalHelpers = { bindConfirm, toast };

})(window);
