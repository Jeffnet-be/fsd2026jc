/*
 * site.js — global client-side helpers for CL Tickets
 * Curriculum ref: client-side development (section 1.1 of course notes)
 */

'use strict';

// ── Cart badge refresh ────────────────────────────────────────────────────
// The navbar badge count is updated via the AJAX AddToCart response.
// On page load we read the stored count from sessionStorage so the badge
// persists across navigation without an extra server round-trip.
(function initCartBadge() {
    var stored = sessionStorage.getItem('cartCount');
    var badge  = document.getElementById('cart-badge');
    if (badge && stored) {
        badge.textContent = stored;
    }
})();

// Helper called by the AJAX success handler on the match detail page
function updateCartBadge(count) {
    sessionStorage.setItem('cartCount', count);
    var badge = document.getElementById('cart-badge');
    if (badge) badge.textContent = count;
}

// ── Auto-hide alerts after 5 seconds ────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    var alerts = document.querySelectorAll('.alert.alert-success, .alert.alert-info');
    alerts.forEach(function (el) {
        setTimeout(function () {
            el.style.transition = 'opacity .5s';
            el.style.opacity    = '0';
            setTimeout(function () { el.remove(); }, 500);
        }, 5000);
    });
});
