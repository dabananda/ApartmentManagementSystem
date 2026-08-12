document.addEventListener('DOMContentLoaded', function () {
    setActiveSidebarLinks();
    addClickHandlers();
});

function setActiveSidebarLinks() {
    const sidebarLinks = document.querySelectorAll('.sidebar-nav-link');
    const currentPath = window.location.pathname.toLowerCase();
    const currentController = getCurrentController();
    const currentAction = getCurrentAction();

    sidebarLinks.forEach(link => {
        link.classList.remove('active');
    });

    const activeLinks = findAllActiveLinks(sidebarLinks, currentPath, currentController, currentAction);

    activeLinks.forEach(link => {
        link.classList.add('active');
    });
}

function addClickHandlers() {
    const sidebarLinks = document.querySelectorAll('.sidebar-nav-link');

    sidebarLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            const clickedHref = this.getAttribute('href');

            sidebarLinks.forEach(l => l.classList.remove('active'));

            sidebarLinks.forEach(l => {
                if (l.getAttribute('href') === clickedHref) {
                    l.classList.add('active');

                    l.style.animation = 'none';
                    setTimeout(() => {
                        l.style.animation = 'pulse 0.3s ease-in-out';
                    }, 10);
                }
            });
        });
    });
}

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

function findAllActiveLinks(links, currentPath, currentController, currentAction) {
    let exactMatches = [];
    let controllerMatches = [];
    let fallbackMatches = [];

    const currentPathWithoutQuery = currentPath.split('?')[0];

    links.forEach(link => {
        const href = link.getAttribute('href');
        if (!href || href === '#') return;

        const linkPath = href.toLowerCase();
        const linkPathWithoutQuery = linkPath.split('?')[0];

        const linkSegments = linkPathWithoutQuery.split('/').filter(segment => segment.length > 0);
        const linkController = linkSegments.length > 0 ? linkSegments[0] : '';
        const linkAction = linkSegments.length > 1 ? linkSegments[1] : '';

        if (currentPath === linkPath) {
            exactMatches.push(link);
            return;
        }

        if (currentPathWithoutQuery === linkPathWithoutQuery) {
            exactMatches.push(link);
            return;
        }

        if (currentController === linkController && currentAction === linkAction) {
            if (exactMatches.length === 0) {
                exactMatches.push(link);
            }
            return;
        }

        if (currentController === linkController) {
            if (linkAction === 'dashboard' || linkAction === 'index' || linkAction === '') {
                if (controllerMatches.length === 0) {
                    controllerMatches.push(link);
                }
            } else if (controllerMatches.length === 0 && exactMatches.length === 0) {
                controllerMatches.push(link);
            }
        }

        if (currentPathWithoutQuery.includes('/dashboard') && linkPathWithoutQuery.includes('/dashboard')) {
            if (fallbackMatches.length === 0 && exactMatches.length === 0 && controllerMatches.length === 0) {
                fallbackMatches.push(link);
            }
        }
    });

    if (exactMatches.length > 0) return exactMatches;
    if (controllerMatches.length > 0) return controllerMatches;
    if (fallbackMatches.length > 0) return fallbackMatches;
    return [];
}

window.addEventListener('popstate', function () {
    setTimeout(() => {
        setActiveSidebarLinks();
    }, 100);
});

const style = document.createElement('style');
style.textContent = `
    @keyframes pulse {
        0% { transform: scale(1); }
        50% { transform: scale(0.98); }
        100% { transform: scale(1); }
    }
`;
document.head.appendChild(style);