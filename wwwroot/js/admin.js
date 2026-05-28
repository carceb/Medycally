// Sidebar Toggle
document.addEventListener('DOMContentLoaded', function() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar       = document.getElementById('sidebar');
    const backdrop      = document.getElementById('sidebarBackdrop');

    const isMobile = () => window.innerWidth < 768;

    function closeMobileSidebar() {
        sidebar?.classList.remove('show');
        backdrop?.classList.remove('show');
    }

    function openMobileSidebar() {
        sidebar?.classList.add('show');
        backdrop?.classList.add('show');
    }

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            if (isMobile()) {
                if (sidebar.classList.contains('show')) closeMobileSidebar();
                else openMobileSidebar();
            } else {
                sidebar.classList.toggle('collapsed');
                const isCollapsed = sidebar.classList.contains('collapsed');
                localStorage.setItem('sidebarCollapsed', isCollapsed);
            }
        });

        // Restore desktop sidebar state from localStorage (only applies on desktop)
        if (!isMobile() && localStorage.getItem('sidebarCollapsed') === 'true') {
            sidebar.classList.add('collapsed');
        }
    }

    // Close mobile sidebar when tapping the backdrop
    backdrop?.addEventListener('click', closeMobileSidebar);

    // Close mobile sidebar when tapping a link (so navigation feels right)
    sidebar?.querySelectorAll('.sidebar-link').forEach(link => {
        link.addEventListener('click', () => {
            if (isMobile() && !link.hasAttribute('data-bs-toggle')) {
                closeMobileSidebar();
            }
        });
    });

    // Clean up classes when crossing the mobile/desktop boundary on resize
    let _lastIsMobile = isMobile();
    window.addEventListener('resize', () => {
        const nowMobile = isMobile();
        if (nowMobile !== _lastIsMobile) {
            closeMobileSidebar();
            if (nowMobile) sidebar?.classList.remove('collapsed');
            else if (localStorage.getItem('sidebarCollapsed') === 'true') sidebar?.classList.add('collapsed');
            _lastIsMobile = nowMobile;
        }
    });

    // Active menu item
    const currentPath = window.location.pathname;
    const sidebarLinks = document.querySelectorAll('.sidebar-link');

    sidebarLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });
});

// Dropdown toggle for Bootstrap 5
document.addEventListener('DOMContentLoaded', function() {
    const dropdownElementList = [].slice.call(document.querySelectorAll('[data-bs-toggle="dropdown"]'));
    dropdownElementList.map(function (dropdownToggleEl) {
        return new bootstrap.Dropdown(dropdownToggleEl);
    });
});
