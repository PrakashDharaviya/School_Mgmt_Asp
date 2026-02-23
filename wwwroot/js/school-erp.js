// SchoolERP site-wide JavaScript
document.addEventListener('DOMContentLoaded', function () {

    // ===== AUTO-DISMISS ALERTS =====
    var alerts = document.querySelectorAll('.alert-auto-dismiss');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            alert.style.transition = 'opacity 0.5s';
            alert.style.opacity = '0';
            setTimeout(function () { alert.remove(); }, 500);
        }, 5000);
    });

    // ===== TOGGLE PASSWORD VISIBILITY =====
    var toggleBtns = document.querySelectorAll('[data-toggle-password]');
    toggleBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var input = document.querySelector(btn.getAttribute('data-toggle-password'));
            if (input) {
                input.type = input.type === 'password' ? 'text' : 'password';
                var icon = btn.querySelector('.material-symbols-outlined');
                if (icon) icon.textContent = input.type === 'password' ? 'visibility' : 'visibility_off';
            }
        });
    });

    // ===== BULK PRESENT ALL =====
    var bulkPresentBtn = document.getElementById('bulkPresentAll');
    if (bulkPresentBtn) {
        bulkPresentBtn.addEventListener('click', function () {
            document.querySelectorAll('input[type="checkbox"][name*="IsPresent"]').forEach(function (cb) {
                cb.checked = true;
            });
        });
    }

    // ===== DARK / LIGHT THEME TOGGLE =====
    var themeToggle = document.getElementById('themeToggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            var html = document.documentElement;
            var isDark = html.classList.contains('dark');

            if (isDark) {
                html.classList.remove('dark');
                localStorage.setItem('theme', 'light');
            } else {
                html.classList.add('dark');
                localStorage.setItem('theme', 'dark');
            }
        });
    }

    // ===== MOBILE SIDEBAR TOGGLE =====
    var sidebarToggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('sidebar');
    var backdrop = document.getElementById('sidebarBackdrop');
    var hamburgerIcon = document.getElementById('hamburgerIcon');

    function openSidebar() {
        if (!sidebar || !backdrop) return;
        sidebar.classList.add('sidebar-open');
        backdrop.classList.add('backdrop-visible');
        if (hamburgerIcon) hamburgerIcon.textContent = 'close';
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        if (!sidebar || !backdrop) return;
        sidebar.classList.remove('sidebar-open');
        backdrop.classList.remove('backdrop-visible');
        if (hamburgerIcon) hamburgerIcon.textContent = 'menu';
        document.body.style.overflow = '';
    }

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            if (sidebar && sidebar.classList.contains('sidebar-open')) {
                closeSidebar();
            } else {
                openSidebar();
            }
        });
    }

    // Close sidebar when clicking backdrop
    if (backdrop) {
        backdrop.addEventListener('click', closeSidebar);
    }

    // Close sidebar when clicking a sidebar link (mobile only)
    if (sidebar) {
        sidebar.querySelectorAll('.sidebar-link').forEach(function (link) {
            link.addEventListener('click', function () {
                if (window.innerWidth < 1024) {
                    closeSidebar();
                }
            });
        });
    }

    // Close sidebar on Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeSidebar();
        }
    });

    // Close mobile sidebar on window resize to desktop
    window.addEventListener('resize', function () {
        if (window.innerWidth >= 1024) {
            closeSidebar();
        }
    });
});
