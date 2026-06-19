(function () {
    const dismissedKey = "sente360_web_push_dismissed";
    let publicKeyCache = null;

    document.addEventListener("DOMContentLoaded", async function () {
        if (!isAuthenticatedPage()) return;

        registerServiceWorker();
        bindOptInCard();
        bindLogoutForms();
        bindServiceWorkerMessages();

        if (isPushSupported() && Notification.permission === "granted") {
            await ensureSubscription(false);
        }

        await updatePushStatusUI();
        maybeShowOptIn();
    });

    function isAuthenticatedPage() {
        return !location.pathname.toLowerCase().startsWith("/auth");
    }

    function isPushSupported() {
        return "serviceWorker" in navigator &&
            "PushManager" in window &&
            "Notification" in window;
    }

    async function registerServiceWorker() {
        if (!("serviceWorker" in navigator)) return;

        try {
            await navigator.serviceWorker.register("/sw.js", { scope: "/" });
        } catch {
            console.info("Sente360 web push service worker could not be registered.");
        }
    }

    function bindOptInCard() {
        document.addEventListener("click", async function (event) {
            const openButton = event.target.closest("[data-web-push-open]");
            const dismissButton = event.target.closest("[data-web-push-dismiss]");
            const closeButton = event.target.closest("[data-web-push-close-device]");

            if (openButton) {
                event.preventDefault();
                await requestAndSubscribe();
            }

            if (dismissButton) {
                event.preventDefault();
                sessionStorage.setItem(dismissedKey, "1");
                hideOptIn();
            }

            if (closeButton) {
                event.preventDefault();
                await disableCurrentDevice();
            }
        });
    }

    function bindServiceWorkerMessages() {
        if (!navigator.serviceWorker) return;

        navigator.serviceWorker.addEventListener("message", async function (event) {
            const message = event.data || {};

            if (message.type === "sente360-push") {
                const payload = message.payload || {};
                if (typeof window.showToast === "function") {
                    window.showToast(payload.body || "Yeni bildiriminiz var.", "info");
                }
                await refreshNotificationBell();
            }

            if (message.type === "sente360-push-click" && message.url) {
                window.location.href = message.url;
            }
        });
    }

    function bindLogoutForms() {
        document.addEventListener("submit", function (event) {
            const form = event.target;

            if (!(form instanceof HTMLFormElement)) return;
            if (!form.action.toLowerCase().includes("/auth/logout")) return;

            deactivateSubscriptionBestEffort();
        }, true);
    }

    async function requestAndSubscribe() {
        if (!isPushSupported()) {
            showStatusMessage(getUnsupportedMessage(), "error");
            await updatePushStatusUI("Bu cihaz desteklemiyor");
            return;
        }

        if (!window.isSecureContext) {
            showStatusMessage("Tarayıcı bildirimleri için güvenli bağlantı gerekir.", "error");
            return;
        }

        if (Notification.permission === "denied") {
            showStatusMessage("Tarayıcı bildirimleri tarayıcı ayarlarından engellenmiş.", "error");
            await updatePushStatusUI("Tarayıcı tarafından engellendi");
            return;
        }

        const permission = Notification.permission === "granted"
            ? "granted"
            : await Notification.requestPermission();

        if (permission !== "granted") {
            showStatusMessage("Tarayıcı bildirimi izni verilmedi.", "error");
            await updatePushStatusUI(permission === "denied" ? "Tarayıcı tarafından engellendi" : "Kapalı");
            return;
        }

        await ensureSubscription(true);
    }

    async function ensureSubscription(showMessage) {
        try {
            const registration = await navigator.serviceWorker.ready;
            const publicKey = await getPublicKey();
            let subscription = await registration.pushManager.getSubscription();

            if (!subscription) {
                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(publicKey)
                });
            }

            await upsertSubscription(subscription);

            hideOptIn();
            await updatePushStatusUI("Açık");

            if (showMessage) {
                showStatusMessage("Tarayıcı bildirimleri açıldı.", "success");
            }
        } catch (error) {
            console.info("Sente360 web push subscription failed.", error);
            showStatusMessage("Tarayıcı bildirimi açılamadı.", "error");
        }
    }

    async function getPublicKey() {
        if (publicKeyCache) return publicKeyCache;

        const response = await fetch("/WebPush/PublicKey", {
            credentials: "same-origin"
        });

        if (!response.ok) {
            throw new Error("Public key could not be loaded.");
        }

        const data = await response.json();
        publicKeyCache = data.publicKey;
        return publicKeyCache;
    }

    async function upsertSubscription(subscription) {
        const response = await fetch("/WebPush/Subscribe", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "same-origin",
            body: JSON.stringify(toSubscriptionRequest(subscription))
        });

        if (!response.ok) {
            throw new Error("Subscription could not be saved.");
        }
    }

    async function disableCurrentDevice() {
        if (!isPushSupported()) return;

        const registration = await navigator.serviceWorker.ready;
        const subscription = await registration.pushManager.getSubscription();

        if (subscription) {
            await fetch("/WebPush/Unsubscribe", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "same-origin",
                body: JSON.stringify(toSubscriptionRequest(subscription))
            }).catch(() => {});

            await subscription.unsubscribe().catch(() => {});
        }

        await updatePushStatusUI("Kapalı");
        showStatusMessage("Bu cihaz için tarayıcı bildirimleri kapatıldı.", "info");
    }

    function deactivateSubscriptionBestEffort() {
        if (!isPushSupported()) return;

        navigator.serviceWorker.ready
            .then(registration => registration.pushManager.getSubscription())
            .then(subscription => {
                if (!subscription) return;

                const payload = JSON.stringify(toSubscriptionRequest(subscription));
                navigator.sendBeacon?.("/WebPush/Unsubscribe", new Blob([payload], { type: "application/json" }));
            })
            .catch(() => {});
    }

    async function updatePushStatusUI(explicitText) {
        const statusNodes = document.querySelectorAll("[data-web-push-status]");
        if (!statusNodes.length) return;

        let text = explicitText;

        if (!text) {
            if (!isPushSupported()) {
                text = "Bu cihaz desteklemiyor";
            } else if (Notification.permission === "denied") {
                text = "Tarayıcı tarafından engellendi";
            } else if (Notification.permission !== "granted") {
                text = "Kapalı";
            } else {
                const registration = await navigator.serviceWorker.ready;
                const subscription = await registration.pushManager.getSubscription();
                text = subscription ? "Açık" : "Kapalı";
            }
        }

        statusNodes.forEach(node => {
            node.textContent = text;
            node.dataset.state = text;
        });
    }

    async function refreshNotificationBell() {
        const shells = document.querySelectorAll("[data-s360-notification]");
        if (!shells.length) return;

        try {
            const response = await fetch("/Notifications/Header", {
                credentials: "same-origin"
            });

            if (!response.ok) return;

            const html = await response.text();
            shells.forEach(shell => {
                const wrapper = document.createElement("div");
                wrapper.innerHTML = html.trim();
                const fresh = wrapper.querySelector("[data-s360-notification]");
                if (fresh) shell.replaceWith(fresh);
            });
        } catch {
            // Header refresh is best-effort.
        }
    }

    function maybeShowOptIn() {
        const card = document.querySelector("[data-web-push-optin]");
        if (!card) return;

        if (!isPushSupported() || Notification.permission === "denied") {
            card.hidden = true;
            return;
        }

        if (Notification.permission === "default" && sessionStorage.getItem(dismissedKey) !== "1") {
            card.hidden = false;
        }
    }

    function hideOptIn() {
        document.querySelectorAll("[data-web-push-optin]").forEach(card => {
            card.hidden = true;
        });
    }

    function toSubscriptionRequest(subscription) {
        const json = subscription.toJSON();

        return {
            endpoint: json.endpoint,
            keys: {
                p256dh: json.keys?.p256dh,
                auth: json.keys?.auth
            },
            userAgent: navigator.userAgent || null
        };
    }

    function urlBase64ToUint8Array(base64String) {
        const padding = "=".repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }

        return outputArray;
    }

    function showStatusMessage(message, type) {
        if (typeof window.showToast === "function") {
            window.showToast(message, type);
        }
    }

    function getUnsupportedMessage() {
        return "Sente360 bildirimlerini kullanmak için Paylaş menüsünden Ana Ekrana Ekle seçeneğini kullanın. Ardından Ana Ekrandaki Sente360 uygulamasını açın.";
    }
})();
