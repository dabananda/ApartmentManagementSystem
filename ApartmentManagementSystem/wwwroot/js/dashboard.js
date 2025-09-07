document.addEventListener('DOMContentLoaded', function () {
    const sidebarLinks = document.querySelectorAll('.sidebar .nav-link');
    const currentPath = window.location.pathname.toLowerCase();
    const currentController = getCurrentController();
    const currentAction = getCurrentAction();

    // Remove any existing active classes
    sidebarLinks.forEach(link => {
        link.classList.remove('active');
    });

    // Find and set active link
    let activeLink = findActiveLink(sidebarLinks, currentPath, currentController, currentAction);

    if (activeLink) {
        activeLink.classList.add('active');
    }

    // Add click handlers for immediate feedback
    sidebarLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            // Remove active class from all links
            sidebarLinks.forEach(l => l.classList.remove('active'));
            // Add active class to clicked link
            this.classList.add('active');
        });
    });
});

function getCurrentController() {
    const path = window.location.pathname;
    const segments = path.split('/').filter(segment => segment.length > 0);
    return segments.length > 0 ? segments[0].toLowerCase() : '';
}

function getCurrentAction() {
    const path = window.location.pathname;
    const segments = path.split('/').filter(segment => segment.length > 0);
    return segments.length > 1 ? segments[1].toLowerCase() : '';
}

function findActiveLink(links, currentPath, currentController, currentAction) {
    let exactMatch = null;
    let controllerMatch = null;
    let fallbackMatch = null;

    // Get current URL without query parameters for comparison
    const currentPathWithoutQuery = currentPath.split('?')[0];

    links.forEach(link => {
        const href = link.getAttribute('href');
        if (!href || href === '#') return;

        const linkPath = href.toLowerCase();
        const linkPathWithoutQuery = linkPath.split('?')[0];

        // Extract controller and action from link (without query parameters)
        const linkSegments = linkPathWithoutQuery.split('/').filter(segment => segment.length > 0);
        const linkController = linkSegments.length > 0 ? linkSegments[0] : '';
        const linkAction = linkSegments.length > 1 ? linkSegments[1] : '';

        // Exact path match including query parameters (highest priority)
        if (currentPath === linkPath) {
            exactMatch = link;
            return;
        }

        // Path match without query parameters (high priority)
        if (currentPathWithoutQuery === linkPathWithoutQuery) {
            exactMatch = link;
            return;
        }

        // Controller and action match
        if (currentController === linkController && currentAction === linkAction) {
            if (!exactMatch) {
                exactMatch = link;
            }
            return;
        }

        // Controller match (medium priority)
        if (currentController === linkController) {
            // Prefer dashboard/index actions for controller matches
            if (linkAction === 'dashboard' || linkAction === 'index' || linkAction === '') {
                if (!controllerMatch) {
                    controllerMatch = link;
                }
            } else if (!controllerMatch && !exactMatch) {
                controllerMatch = link;
            }
        }

        // Special cases for common patterns
        if (currentPathWithoutQuery.includes('/dashboard') && linkPathWithoutQuery.includes('/dashboard')) {
            if (!fallbackMatch && !exactMatch && !controllerMatch) {
                fallbackMatch = link;
            }
        }
    });

    return exactMatch || controllerMatch || fallbackMatch;
}

// Handle browser back/forward navigation
window.addEventListener('popstate', function () {
    setTimeout(() => {
        const event = new Event('DOMContentLoaded');
        document.dispatchEvent(event);
    }, 100);
});