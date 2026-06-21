(function () {
    "use strict";

    function showContactToast(message, type) {
        if (typeof window.showToast === "function") {
            window.showToast(message, type || "success");
            return;
        }

        const toast = document.getElementById("toastMessage");
        if (!toast) {
            return;
        }

        toast.textContent = message;
        toast.className = `toast-message ${type || "success"} show`;
        setTimeout(function () {
            toast.classList.remove("show");
        }, 2600);
    }

    async function copyText(text) {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return;
        }

        const textarea = document.createElement("textarea");
        textarea.value = text;
        textarea.setAttribute("readonly", "");
        textarea.style.position = "fixed";
        textarea.style.left = "-9999px";
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand("copy");
        textarea.remove();
    }

    function handleAction(button) {
        const root = button.closest("[data-sente-contact]");

        if (!root) {
            return;
        }

        const action = button.dataset.senteContactAction;
        const phone = root.dataset.phone;
        const tel = root.dataset.tel;

        if (!phone || !tel) {
            showContactToast("Geçerli telefon numarası bulunamadı.", "error");
            return;
        }

        if (action === "call") {
            window.location.href = `tel:${tel}`;
            return;
        }

        if (action === "whatsapp") {
            window.open(`https://wa.me/${phone}`, "_blank", "noopener");
            return;
        }

        if (action === "copy") {
            copyText(tel)
                .then(function () {
                    showContactToast("Telefon numarası kopyalandı.", "success");
                })
                .catch(function () {
                    showContactToast("Telefon numarası kopyalanamadı.", "error");
                });
        }
    }

    document.addEventListener("click", function (event) {
        const button = event.target.closest("[data-sente-contact-action]");

        if (!button) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        handleAction(button);
    }, true);

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Enter" && event.key !== " ") {
            return;
        }

        const button = event.target.closest("[data-sente-contact-action]");

        if (!button) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        handleAction(button);
    });
})();
