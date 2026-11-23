(function () {
    let activeDialog = null;
    let trapHandler = null;

    function focusableElements(dialog) {
        return Array.from(dialog.querySelectorAll(
            'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )).filter(element => element.getAttribute('aria-hidden') !== 'true' && element.offsetParent !== null);
    }

    window.shopPulseModal = {
        activate: function (dialog) {
            this.deactivate();
            activeDialog = dialog;
            const focusable = focusableElements(dialog);
            (focusable[0] || dialog).focus();
            trapHandler = function (event) {
                if (event.key !== 'Tab') return;
                const items = focusableElements(dialog);
                if (!items.length) {
                    event.preventDefault();
                    dialog.focus();
                    return;
                }
                const first = items[0];
                const last = items[items.length - 1];
                if (event.shiftKey && (document.activeElement === first || !dialog.contains(document.activeElement))) {
                    event.preventDefault();
                    last.focus();
                } else if (!event.shiftKey && (document.activeElement === last || !dialog.contains(document.activeElement))) {
                    event.preventDefault();
                    first.focus();
                }
            };
            dialog.addEventListener('keydown', trapHandler);
        },
        deactivate: function () {
            if (activeDialog && trapHandler) activeDialog.removeEventListener('keydown', trapHandler);
            activeDialog = null;
            trapHandler = null;
        }
    };
})();