(function () {
    "use strict";

    var STORAGE_KEY_WIDTH = "medistack_sidebar_width";
    var STORAGE_KEY_COLLAPSED = "medistack_sidebar_collapsed";
    var STORAGE_KEY_CATALOGOS = "medistack_sidebar_catalogos_open";
    var MIN_WIDTH = 200;
    var MAX_WIDTH = 450;

    // Apply saved preferences immediately to avoid layout flicker
    try {
        var savedWidth = parseInt(localStorage.getItem(STORAGE_KEY_WIDTH), 10);
        if (!isNaN(savedWidth) && savedWidth >= MIN_WIDTH && savedWidth <= MAX_WIDTH) {
            document.documentElement.style.setProperty("--sidebar-width", savedWidth + "px");
        }

        var isCollapsed = localStorage.getItem(STORAGE_KEY_COLLAPSED) === "true";
        if (isCollapsed) {
            document.documentElement.classList.add("sidebar-pref-collapsed");
        }
    } catch (e) {
        // localStorage not available
    }

    document.addEventListener("DOMContentLoaded", function () {
        var appShell = document.getElementById("appShell");
        if (!appShell) return;

        var sidebar = document.getElementById("appSidebar");
        var resizer = document.getElementById("sidebarResizer");
        var collapseBtn = document.getElementById("sidebarCollapseBtn");
        var mobileMenuBtn = document.getElementById("topbarMenuBtn");
        var backdrop = document.getElementById("sidebarBackdrop");

        // Sync collapsed preference from root
        if (document.documentElement.classList.contains("sidebar-pref-collapsed")) {
            appShell.classList.add("sidebar-collapsed");
        }

        // Toggle collapse/expand (desktop)
        var catalogosDetails = document.getElementById("sidebarCatalogos");
        if (catalogosDetails) {
            try {
                if (localStorage.getItem(STORAGE_KEY_CATALOGOS) === "true") {
                    catalogosDetails.open = true;
                }
            } catch (e) {}

            catalogosDetails.addEventListener("toggle", function () {
                try {
                    localStorage.setItem(STORAGE_KEY_CATALOGOS, catalogosDetails.open ? "true" : "false");
                } catch (e) {}
            });
        }

        if (collapseBtn) {
            collapseBtn.addEventListener("click", function () {
                var collapsed = appShell.classList.toggle("sidebar-collapsed");
                try {
                    localStorage.setItem(STORAGE_KEY_COLLAPSED, collapsed ? "true" : "false");
                } catch (e) {}
            });
        }

        // Mobile drawer toggle
        if (mobileMenuBtn) {
            mobileMenuBtn.addEventListener("click", function () {
                appShell.classList.add("sidebar-open-mobile");
            });
        }

        function closeMobileSidebar() {
            if (appShell) {
                appShell.classList.remove("sidebar-open-mobile");
            }
        }

        if (backdrop) {
            backdrop.addEventListener("click", closeMobileSidebar);
        }

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && appShell.classList.contains("sidebar-open-mobile")) {
                closeMobileSidebar();
            }
        });

        // Resizable drag & drop on sidebar border
        if (resizer && sidebar) {
            var isResizing = false;
            var startX = 0;
            var currentWidth = 260;

            resizer.addEventListener("mousedown", function (e) {
                // Don't resize if collapsed
                if (appShell.classList.contains("sidebar-collapsed")) return;

                isResizing = true;
                startX = e.clientX;
                var rect = sidebar.getBoundingClientRect();
                currentWidth = rect.width;

                document.body.classList.add("is-resizing-sidebar");
                e.preventDefault();
            });

            document.addEventListener("mousemove", function (e) {
                if (!isResizing) return;

                var newWidth = e.clientX;
                if (newWidth < MIN_WIDTH) newWidth = MIN_WIDTH;
                if (newWidth > MAX_WIDTH) newWidth = MAX_WIDTH;

                document.documentElement.style.setProperty("--sidebar-width", newWidth + "px");
                currentWidth = newWidth;
            });

            document.addEventListener("mouseup", function () {
                if (isResizing) {
                    isResizing = false;
                    document.body.classList.remove("is-resizing-sidebar");
                    try {
                        localStorage.setItem(STORAGE_KEY_WIDTH, currentWidth);
                    } catch (e) {}
                }
            });
        }

        // Highlight active navigation link
        highlightActiveLink();
        window.addEventListener("hashchange", highlightActiveLink);
    });

    function highlightActiveLink() {
        var currentPath = window.location.pathname.toLowerCase();
        var currentHash = window.location.hash.toLowerCase();
        var links = document.querySelectorAll(".sidebar-nav-link");
        if (!links || links.length === 0) return;

        for (var k = 0; k < links.length; k++) {
            links[k].classList.remove("is-active");
            links[k].removeAttribute("aria-current");
        }

        var matchedLink = null;

        for (var i = 0; i < links.length; i++) {
            var link = links[i];
            var href = link.getAttribute("href");
            if (!href) continue;

            var linkUrl;
            try {
                linkUrl = new URL(href, window.location.origin);
            } catch (e) {
                continue;
            }

            var linkPath = linkUrl.pathname.toLowerCase();
            var linkHash = linkUrl.hash.toLowerCase();

            // Match full path and hash if specified
            if (linkHash && currentHash === linkHash && currentPath === linkPath) {
                matchedLink = link;
                break;
            } else if (!linkHash && currentPath === linkPath) {
                // If there's an exact path match and no hash
                if (!matchedLink || (!matchedLink.hash && linkPath === currentPath)) {
                    matchedLink = link;
                }
            }
        }

        if (matchedLink) {
            matchedLink.classList.add("is-active");
            matchedLink.setAttribute("aria-current", "page");
            var parentDetails = matchedLink.closest("details");
            if (parentDetails) {
                parentDetails.open = true;
            }
        }
    }

    window.highlightSidebarActiveLink = highlightActiveLink;
})();
