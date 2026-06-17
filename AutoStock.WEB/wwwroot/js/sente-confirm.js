(function () {
    const ids = {
        backdrop: "senteConfirmBackdrop",
        title: "senteConfirmTitle",
        message: "senteConfirmMessage",
        confirm: "senteConfirmConfirm",
        cancel: "senteConfirmCancel",
        close: "senteConfirmClose"
    };

    let activeResolve = null;
    let previousFocus = null;
    let isResolving = false;

    function ensureModal() {
        let backdrop = document.getElementById(ids.backdrop);

        if (backdrop) {
            return backdrop;
        }

        backdrop = document.createElement("div");
        backdrop.id = ids.backdrop;
        backdrop.className = "sente-confirm-backdrop";
        backdrop.hidden = true;
        backdrop.innerHTML = `
            <div class="sente-confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="${ids.title}" aria-describedby="${ids.message}">
                <div class="sente-confirm-head">
                    <h2 class="sente-confirm-title" id="${ids.title}"></h2>
                    <button type="button" class="sente-confirm-close" id="${ids.close}" aria-label="Kapat">×</button>
                </div>
                <div class="sente-confirm-body">
                    <p class="sente-confirm-message" id="${ids.message}"></p>
                </div>
                <div class="sente-confirm-actions">
                    <button type="button" class="sente-confirm-btn sente-confirm-cancel" id="${ids.cancel}">Vazgeç</button>
                    <button type="button" class="sente-confirm-btn sente-confirm-ok" id="${ids.confirm}">Onayla</button>
                </div>
            </div>
        `;

        document.body.appendChild(backdrop);

        backdrop.addEventListener("click", function (event) {
            if (event.target === backdrop) {
                resolveConfirm(false);
            }
        });

        document.getElementById(ids.close).addEventListener("click", function () {
            resolveConfirm(false);
        });

        document.getElementById(ids.cancel).addEventListener("click", function () {
            resolveConfirm(false);
        });

        document.getElementById(ids.confirm).addEventListener("click", function () {
            const confirmButton = document.getElementById(ids.confirm);

            if (isResolving || confirmButton.disabled) {
                return;
            }

            isResolving = true;
            confirmButton.disabled = true;
            resolveConfirm(true);
        });

        document.addEventListener("keydown", handleKeydown);

        return backdrop;
    }

    function getFocusableElements() {
        const backdrop = document.getElementById(ids.backdrop);

        if (!backdrop || backdrop.hidden) {
            return [];
        }

        return Array.from(
            backdrop.querySelectorAll("button, [href], input, select, textarea, [tabindex]:not([tabindex='-1'])")
        ).filter(element => !element.disabled && element.offsetParent !== null);
    }

    function handleKeydown(event) {
        const backdrop = document.getElementById(ids.backdrop);

        if (!backdrop || backdrop.hidden) {
            return;
        }

        if (event.key === "Escape") {
            event.preventDefault();
            resolveConfirm(false);
            return;
        }

        if (event.key !== "Tab") {
            return;
        }

        const focusable = getFocusableElements();

        if (!focusable.length) {
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function resolveConfirm(value) {
        const backdrop = document.getElementById(ids.backdrop);
        const confirmButton = document.getElementById(ids.confirm);

        if (!activeResolve) {
            return;
        }

        const resolve = activeResolve;
        activeResolve = null;

        if (backdrop) {
            backdrop.hidden = true;
        }

        if (confirmButton) {
            confirmButton.disabled = false;
        }

        document.documentElement.classList.remove("sente-confirm-lock");
        document.body.classList.remove("sente-confirm-lock");

        if (previousFocus && typeof previousFocus.focus === "function") {
            previousFocus.focus();
        }

        previousFocus = null;
        isResolving = false;
        resolve(value);
    }

    async function show(options) {
        const backdrop = ensureModal();
        const dialog = backdrop.querySelector(".sente-confirm-dialog");
        const title = document.getElementById(ids.title);
        const message = document.getElementById(ids.message);
        const confirmButton = document.getElementById(ids.confirm);
        const cancelButton = document.getElementById(ids.cancel);

        if (activeResolve) {
            resolveConfirm(false);
        }

        previousFocus = document.activeElement;
        isResolving = false;

        title.textContent = options?.title || "Onay gerekiyor";
        message.textContent = options?.message || "Bu işlemi onaylıyor musunuz?";
        confirmButton.textContent = options?.confirmText || "Onayla";
        cancelButton.textContent = options?.cancelText || "Vazgeç";
        confirmButton.disabled = false;
        dialog.classList.toggle("is-danger", Boolean(options?.danger));

        backdrop.hidden = false;
        document.documentElement.classList.add("sente-confirm-lock");
        document.body.classList.add("sente-confirm-lock");

        const focusTarget = options?.danger ? cancelButton : confirmButton;
        window.setTimeout(() => focusTarget.focus(), 20);

        return new Promise(resolve => {
            activeResolve = resolve;
        });
    }

    function bindConfirmForms() {
        document.querySelectorAll("form[data-sente-confirm]").forEach(form => {
            if (form.dataset.senteConfirmBound === "true") {
                return;
            }

            form.dataset.senteConfirmBound = "true";

            form.addEventListener("submit", async function (event) {
                if (form.dataset.senteConfirmSubmitting === "true") {
                    return;
                }

                event.preventDefault();

                const confirmed = await show({
                    title: form.dataset.senteConfirmTitle,
                    message: form.dataset.senteConfirmMessage,
                    confirmText: form.dataset.senteConfirmText,
                    cancelText: form.dataset.senteConfirmCancelText,
                    danger: form.dataset.senteConfirmDanger === "true"
                });

                if (!confirmed) {
                    return;
                }

                form.dataset.senteConfirmSubmitting = "true";
                HTMLFormElement.prototype.submit.call(form);
            });
        });
    }

    window.SenteConfirm = {
        show
    };

    document.addEventListener("DOMContentLoaded", bindConfirmForms);
})();
