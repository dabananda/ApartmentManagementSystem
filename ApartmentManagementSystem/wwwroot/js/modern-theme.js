// Modern Theme JavaScript Enhancements
// SweetAlert2 Custom Configuration
const AMS = {
    // SweetAlert2 Toast Configuration
    Toast: Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer)
            toast.addEventListener('mouseleave', Swal.resumeTimer)
        },
        customClass: {
            popup: 'modern-toast'
        }
    }),

    // Confirmation Dialog Configuration
    confirm: (title = 'Are you sure?', text = '', icon = 'warning') => {
        return Swal.fire({
            title: title,
            text: text,
            icon: icon,
            showCancelButton: true,
            confirmButtonColor: '#15b79e',
            cancelButtonColor: '#64748b',
            confirmButtonText: 'Yes, proceed',
            cancelButtonText: 'Cancel',
            customClass: {
                popup: 'modern-dialog',
                confirmButton: 'btn btn-primary',
                cancelButton: 'btn btn-outline-secondary'
            },
            buttonsStyling: false
        });
    },

    // Success Alert
    success: (title = 'Success!', text = '') => {
        return Swal.fire({
            title: title,
            text: text,
            icon: 'success',
            confirmButtonColor: '#15b79e',
            customClass: {
                popup: 'modern-dialog',
                confirmButton: 'btn btn-primary'
            },
            buttonsStyling: false
        });
    },

    // Error Alert
    error: (title = 'Error!', text = '') => {
        return Swal.fire({
            title: title,
            text: text,
            icon: 'error',
            confirmButtonColor: '#ef4444',
            customClass: {
                popup: 'modern-dialog',
                confirmButton: 'btn btn-danger'
            },
            buttonsStyling: false
        });
    },

    // Loading Dialog
    loading: (title = 'Processing...') => {
        return Swal.fire({
            title: title,
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            customClass: {
                popup: 'modern-dialog'
            },
            didOpen: () => {
                Swal.showLoading();
            }
        });
    }
};

// Initialize modern theme features when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    
    // Add fade-in animation to main content
    const mainContent = document.querySelector('main');
    if (mainContent) {
        mainContent.classList.add('fade-in');
    }

    // Add slide-up animation to cards
    const cards = document.querySelectorAll('.card');
    cards.forEach((card, index) => {
        setTimeout(() => {
            card.classList.add('slide-up');
        }, index * 100);
    });

    // Enhance form submission with loading states
    const forms = document.querySelectorAll('form[method="post"]');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.disabled = true;
                const originalText = submitBtn.textContent || submitBtn.value;
                const loadingHTML = '<span class="loading-spinner me-2"></span>Processing...';
                
                if (submitBtn.tagName === 'BUTTON') {
                    submitBtn.innerHTML = loadingHTML;
                } else {
                    submitBtn.value = 'Processing...';
                }

                // Re-enable after 5 seconds as fallback
                setTimeout(() => {
                    submitBtn.disabled = false;
                    if (submitBtn.tagName === 'BUTTON') {
                        submitBtn.textContent = originalText;
                    } else {
                        submitBtn.value = originalText;
                    }
                }, 5000);
            }
        });
    });

    // Enhanced delete confirmations
    const deleteButtons = document.querySelectorAll('[data-confirm-delete]');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const itemName = this.getAttribute('data-item-name') || 'this item';
            const form = this.closest('form');
            
            AMS.confirm(
                'Delete Confirmation',
                `Are you sure you want to delete ${itemName}? This action cannot be undone.`,
                'warning'
            ).then((result) => {
                if (result.isConfirmed && form) {
                    form.submit();
                }
            });
        });
    });

    // Enhanced table row hover effects
    const tableRows = document.querySelectorAll('table tbody tr');
    tableRows.forEach(row => {
        row.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.001)';
        });
        
        row.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
        });
    });

    // Auto-hide alerts after delay
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        if (!alert.querySelector('.btn-close')) {
            setTimeout(() => {
                alert.style.opacity = '0';
                alert.style.transform = 'translateY(-20px)';
                setTimeout(() => {
                    alert.remove();
                }, 300);
            }, 5000);
        }
    });

    // Enhanced navigation active state
    updateActiveNavigation();

    // Add smooth scrolling for anchor links
    const anchorLinks = document.querySelectorAll('a[href^="#"]');
    anchorLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = this.getAttribute('href').substring(1);
            const targetElement = document.getElementById(targetId);
            
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Add loading state to AJAX-enabled links
    const ajaxLinks = document.querySelectorAll('[data-ajax="true"]');
    ajaxLinks.forEach(link => {
        link.addEventListener('click', function() {
            this.classList.add('loading');
            const originalHTML = this.innerHTML;
            this.innerHTML = '<span class="loading-spinner me-2"></span>' + this.textContent.trim();
            
            setTimeout(() => {
                this.classList.remove('loading');
                this.innerHTML = originalHTML;
            }, 2000);
        });
    });
});

// Function to update active navigation based on current URL
function updateActiveNavigation() {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.sidebar .nav-link');
    
    navLinks.forEach(link => {
        link.classList.remove('active');
        const href = link.getAttribute('href');
        
        if (href && (currentPath === href || (currentPath.includes(href) && href !== '/'))) {
            link.classList.add('active');
        }
    });
}

// Utility function for AJAX requests with modern loading states
function modernAjax(options) {
    const defaults = {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        onStart: () => AMS.loading(),
        onSuccess: (data) => AMS.success('Success', 'Operation completed successfully'),
        onError: (error) => AMS.error('Error', 'Something went wrong. Please try again.')
    };
    
    const config = { ...defaults, ...options };
    
    config.onStart();
    
    fetch(config.url, {
        method: config.method,
        headers: config.headers,
        body: config.data ? JSON.stringify(config.data) : undefined
    })
    .then(response => {
        Swal.close();
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        config.onSuccess(data);
    })
    .catch(error => {
        Swal.close();
        config.onError(error);
    });
}

// Enhanced DataTable initialization with modern styling
function initializeModernDataTable(selector, options = {}) {
    const defaultOptions = {
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
             '<"row"<"col-sm-12"tr>>' +
             '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        responsive: true,
        stateSave: true,
        order: [[0, 'desc']],
        language: {
            search: "Search:",
            lengthMenu: "Show _MENU_ entries",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            infoEmpty: "No entries available",
            infoFiltered: "(filtered from _MAX_ total entries)",
            paginate: {
                first: "First",
                last: "Last",
                next: "Next",
                previous: "Previous"
            }
        },
        drawCallback: function() {
            // Add fade-in animation to new rows
            $(this.api().table().body()).find('tr').addClass('fade-in');
        }
    };
    
    const finalOptions = { ...defaultOptions, ...options };
    
    if ($.fn.DataTable) {
        return $(selector).DataTable(finalOptions);
    }
}

// Enhanced form validation with modern styling
function enhanceFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
                
                // Focus on first invalid field
                const firstInvalid = form.querySelector(':invalid');
                if (firstInvalid) {
                    firstInvalid.focus();
                    firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                
                AMS.error('Validation Error', 'Please correct the highlighted fields and try again.');
            }
            
            form.classList.add('was-validated');
        });
    });
}

// Initialize enhanced form validation
document.addEventListener('DOMContentLoaded', enhanceFormValidation);

// Add to global window object for external access
window.AMS = AMS;
window.modernAjax = modernAjax;
window.initializeModernDataTable = initializeModernDataTable;

// Progressive Enhancement: Add modern features if supported
if ('IntersectionObserver' in window) {
    // Lazy loading animation for cards
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
                observer.unobserve(entry.target);
            }
        });
    });
    
    document.querySelectorAll('.card, .stat-card').forEach(card => {
        observer.observe(card);
    });
}

// Service Worker registration for PWA features (optional)
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        // Uncomment if you add a service worker
        // navigator.serviceWorker.register('/sw.js');
    });
}