/**
 * EMS Student Portal — site.js
 * Handles: dark mode toggle (persist via localStorage), 
 *          theme flash prevention (applied before DOMContentLoaded via inline script in _Layout).
 */

(function () {
    'use strict';

    /* ─── Dark Mode Toggle ────────────────────────────────────── */
    var THEME_KEY = 'ems-theme';
    var DARK      = 'dark';
    var LIGHT     = 'light';

    /**
     * Apply theme to <html> element and update toggle button icon/label.
     * @param {string} theme - 'dark' | 'light'
     */
    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);

        var btn   = document.getElementById('themeToggleBtn');
        var icon  = document.getElementById('themeToggleIcon');
        var label = document.getElementById('themeToggleLabel');

        if (!btn) return;

        if (theme === DARK) {
            if (icon)  icon.className  = 'ri-sun-line';
            if (label) label.textContent = 'Sáng';
            btn.setAttribute('aria-label', 'Chuyển sang chế độ sáng');
            btn.setAttribute('title', 'Chuyển sang chế độ sáng');
        } else {
            if (icon)  icon.className  = 'ri-moon-line';
            if (label) label.textContent = 'Tối';
            btn.setAttribute('aria-label', 'Chuyển sang chế độ tối');
            btn.setAttribute('title', 'Chuyển sang chế độ tối');
        }
    }

    function toggleTheme() {
        var current = document.documentElement.getAttribute('data-theme') || LIGHT;
        applyTheme(current === DARK ? LIGHT : DARK);
    }

    /* ─── Bootstrap Toggle Button (after DOM ready) ───────────── */
    document.addEventListener('DOMContentLoaded', function () {
        // Re-apply to sync button icon state after DOM loads
        var saved = localStorage.getItem(THEME_KEY) || LIGHT;
        applyTheme(saved);

        var btn = document.getElementById('themeToggleBtn');
        if (btn) {
            btn.addEventListener('click', toggleTheme);
        }
    });

})();
