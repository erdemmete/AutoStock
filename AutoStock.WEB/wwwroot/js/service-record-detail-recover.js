
(function () {
    function getCards() {
        return Array.from(document.querySelectorAll(".srx-request-card[data-request-id]"));
    }

    function syncCard(card) {
        if (!card) {
            return;
        }

        const isOpen = card.dataset.expanded === "true";
        const textTargets = card.querySelectorAll('[data-role="toggle-request-text"]');

        textTargets.forEach(target => {
            target.textContent = isOpen ? "Detayı kapat" : "Detayı aç";
        });
    }

    function closeMenus(except) {
        document.querySelectorAll(".srx-request-menu[open]").forEach(menu => {
            if (menu !== except) {
                menu.removeAttribute("open");
            }
        });
    }

    function syncAllCards() {
        getCards().forEach(syncCard);
    }

    document.addEventListener("DOMContentLoaded", function () {
        // İlk açılışta kartlar kapalı kalmalı. Base JS yine data-expanded üzerinden çalışır.
        getCards().forEach(card => {
            if (card.dataset.expanded !== "true") {
                card.dataset.expanded = "false";
            }
            syncCard(card);
        });

        document.addEventListener("click", function (event) {
            const clickedMenu = event.target.closest(".srx-request-menu");
            const menuButton = event.target.closest(".srx-request-menu-list button");
            const toggleButton = event.target.closest('[data-action="toggle-request"]');

            if (!clickedMenu) {
                closeMenus();
            }

            if (menuButton) {
                menuButton.closest(".srx-request-menu")?.removeAttribute("open");
            }

            if (toggleButton) {
                closeMenus();
                window.setTimeout(syncAllCards, 0);
            }
        });

        document.addEventListener("toggle", function (event) {
            const menu = event.target;

            if (!(menu instanceof HTMLDetailsElement)) {
                return;
            }

            if (!menu.classList.contains("srx-request-menu")) {
                return;
            }

            if (menu.open) {
                closeMenus(menu);
            }
        }, true);

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeMenus();
            }
        });
    });
})();
