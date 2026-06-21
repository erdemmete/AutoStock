(function () {
    "use strict";

    const modal = document.querySelector("[data-release-announcement]");

    if (!modal) {
        return;
    }

    const announcementKey = modal.dataset.announcementKey;
    const userId = modal.dataset.userId;

    if (!announcementKey || !userId) {
        return;
    }

    const storageKey = `sente360:release-announcement:${announcementKey}:${userId}`;
    const dialog = modal.querySelector(".s360-release-announcement__dialog");
    const seenButtons = modal.querySelectorAll("[data-release-seen]");
    const laterButtons = modal.querySelectorAll("[data-release-later]");
    let previouslyFocused = null;

    function canUseStorage() {
        try {
            const testKey = "sente360:release-announcement:test";
            window.localStorage.setItem(testKey, "1");
            window.localStorage.removeItem(testKey);
            return true;
        } catch {
            return false;
        }
    }

    const storageAvailable = canUseStorage();

    function hasSeen() {
        if (!storageAvailable) {
            return false;
        }

        try {
            return window.localStorage.getItem(storageKey) === "seen";
        } catch {
            return false;
        }
    }

    function markSeen() {
        if (!storageAvailable) {
            return;
        }

        try {
            window.localStorage.setItem(storageKey, "seen");
        } catch {
            // LocalStorage can be blocked in private mode. Closing the modal is still allowed.
        }
    }

    function getFocusableElements() {
        if (!dialog) {
            return [];
        }

        return Array.from(dialog.querySelectorAll(
            "a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex='-1'])"
        )).filter(element => {
            return element.offsetWidth > 0 || element.offsetHeight > 0 || element === document.activeElement;
        });
    }

    function openModal() {
        previouslyFocused = document.activeElement instanceof HTMLElement
            ? document.activeElement
            : null;

        modal.hidden = false;
        modal.setAttribute("aria-hidden", "false");

        requestAnimationFrame(function () {
            const primaryButton = modal.querySelector("[data-release-seen]");

            if (primaryButton instanceof HTMLElement) {
                primaryButton.focus();
            }
        });
    }

    function closeModal() {
        modal.hidden = true;
        modal.setAttribute("aria-hidden", "true");

        if (previouslyFocused && document.contains(previouslyFocused)) {
            previouslyFocused.focus();
        }
    }

    function closeAndMarkSeen() {
        markSeen();
        closeModal();
    }

    function handleKeydown(event) {
        if (modal.hidden) {
            return;
        }

        if (event.key === "Escape") {
            event.preventDefault();
            closeModal();
            return;
        }

        if (event.key !== "Tab") {
            return;
        }

        const focusable = getFocusableElements();

        if (!focusable.length) {
            event.preventDefault();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
            return;
        }

        if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    seenButtons.forEach(button => {
        button.addEventListener("click", closeAndMarkSeen);
    });

    laterButtons.forEach(button => {
        button.addEventListener("click", closeModal);
    });

    document.addEventListener("keydown", handleKeydown);

    window.addEventListener("DOMContentLoaded", function () {
        if (hasSeen()) {
            return;
        }

        window.setTimeout(openModal, 350);
    });
})();
