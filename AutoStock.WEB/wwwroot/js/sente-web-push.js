(function () {
    let publicKeyCache = null;
    let serverReadyCache = null;
    let initialSyncCompleted = false;
    let subscriptionSyncFailed = false;

    document.addEventListener("DOMContentLoaded", async function () {
        if (!isAuthenticatedPage()) return;

        await registerServiceWorker();
        bindPushControls();
        bindLogoutForms();
        bindServiceWorkerMessages();

        await syncExistingSubscriptionOnce();

        await updatePushStatusUI();
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
            if (window.SentePwaInstall?.ensureServiceWorker) {
                await window.SentePwaInstall.ensureServiceWorker();
                return;
            }

            await navigator.serviceWorker.register("/sw.js", { scope: "/" });
        } catch {
            console.info("Sente360 web push service worker could not be registered.");
        }
    }

    function bindPushControls() {
        document.addEventListener("click", async function (event) {
            const openButton = event.target.closest("[data-web-push-open]");
            const closeButton = event.target.closest("[data-web-push-close-device]");

            if (openButton) {
                event.preventDefault();
                await requestAndSubscribe();
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
        if (isIosBrowserTab()) {
            await updatePushStatusUI(getIosInstallStatus(), getIosInstallHint());
            return;
        }

        if (!isPushSupported()) {
            await updatePushStatusUI("Bu cihaz desteklemiyor", getUnsupportedMessage());
            return;
        }

        if (!window.isSecureContext) {
            await updatePushStatusUI("Güvenli bağlantı gerekir", "Bildirimleri kullanmak için güvenli bağlantı gerekir.");
            return;
        }

        if (Notification.permission === "denied") {
            await updatePushStatusUI("Tarayıcı tarafından engellendi", "Bildirim izni tarayıcı ayarlarından tekrar açılabilir.");
            return;
        }

        if (!await isServerConfigured()) {
            await updatePushStatusUI("Sunucuda yapılandırılmamış", "Bildirim anahtarları henüz tanımlanmamış.");
            return;
        }

        const permission = Notification.permission === "granted"
            ? "granted"
            : await Notification.requestPermission();

        if (permission !== "granted") {
            await updatePushStatusUI(
                permission === "denied" ? "Tarayıcı tarafından engellendi" : "Kapalı",
                "Anlık bildirim almak için izin vermeniz gerekir.");
            return;
        }

        await ensureSubscription(true);
    }

    async function ensureSubscription(showMessage) {
        try {
            const registration = await navigator.serviceWorker.ready;
            const publicKey = await getPublicKey();
            let subscription = await registration.pushManager.getSubscription();

            if (subscription && isApplicationServerKeyMismatch(subscription, publicKey)) {
                await subscription.unsubscribe().catch(() => {});
                subscription = null;
            }

            if (!subscription) {
                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(publicKey)
                });
            }

            await upsertSubscription(subscription);
            subscriptionSyncFailed = false;

            await updatePushStatusUI("Açık");

            if (showMessage) {
                showStatusMessage("Bildirimler etkinleştirildi.", "success");
            }
        } catch (error) {
            console.info("Sente360 web push subscription failed.");
            if (showMessage) {
                await updatePushStatusUI(error.userMessage || "Açılamadı", "Bildirimler şu anda açılamadı. Daha sonra tekrar deneyebilirsiniz.");
            }
        }
    }

    async function syncExistingSubscriptionOnce() {
        if (initialSyncCompleted) return;
        initialSyncCompleted = true;

        if (!isPushSupported() ||
            !window.isSecureContext ||
            Notification.permission !== "granted" ||
            !await isServerConfigured()) {
            return;
        }

        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();

            if (!subscription) return;

            const publicKey = await getPublicKey();
            if (isApplicationServerKeyMismatch(subscription, publicKey)) return;

            await upsertSubscription(subscription);
            subscriptionSyncFailed = false;
        } catch {
            subscriptionSyncFailed = true;
            console.info("Sente360 web push subscription sync failed.");
        }
    }

    async function isServerConfigured() {
        if (serverReadyCache !== null) return serverReadyCache;

        try {
            const publicKey = await getPublicKey();
            serverReadyCache = Boolean(publicKey);
        } catch {
            serverReadyCache = false;
        }

        return serverReadyCache;
    }

    async function getPublicKey() {
        if (publicKeyCache) return publicKeyCache;

        const response = await fetch("/WebPush/PublicKey", {
            credentials: "same-origin"
        });

        if (!response.ok) {
            const message = await readErrorMessage(response, "Tarayıcı bildirimleri henüz yapılandırılmamış.");
            const error = new Error("Public key could not be loaded.");
            error.userMessage = message;
            throw error;
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
            const message = await readErrorMessage(response, "Tarayıcı bildirimi açılamadı.");
            const error = new Error("Subscription could not be saved.");
            error.userMessage = message;
            throw error;
        }

        subscriptionSyncFailed = false;
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

        await updatePushStatusUI("Kapalı", "Anlık destek ve fatura bildirimleri için açabilirsiniz.");
        showStatusMessage("Bu cihaz için bildirimler kapatıldı.", "info");
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

    async function updatePushStatusUI(explicitText, explicitHint) {
        const statusNodes = document.querySelectorAll("[data-web-push-status]");
        const hintNodes = document.querySelectorAll("[data-web-push-hint]");
        const openButtons = document.querySelectorAll("[data-web-push-open]");
        const closeButtons = document.querySelectorAll("[data-web-push-close-device]");
        if (!statusNodes.length && !hintNodes.length && !openButtons.length && !closeButtons.length) return;

        let text = explicitText;
        let hint = explicitHint;
        let actionText = "Bildirimleri Aç";
        let state = "default";

        if (!text) {
            if (isIosBrowserTab()) {
                text = getIosInstallStatus();
                hint = getIosInstallHint();
                state = "unsupported";
            } else if (!isPushSupported()) {
                text = "Bu cihaz desteklemiyor";
                hint = getUnsupportedMessage();
                state = "unsupported";
            } else if (Notification.permission === "denied") {
                text = "Tarayıcı tarafından engellendi";
                hint = "Bildirimler tarayıcı ayarlarından kapatılmış.";
                state = "denied";
            } else if (Notification.permission !== "granted") {
                text = "Bildirimleri Aç";
                hint = "Destek ve fatura işlemlerindeki gelişmelerden haberdar olun.";
                actionText = "Bildirimleri Aç";
                state = "default";
            } else {
                const registration = await navigator.serviceWorker.ready;
                const subscription = await registration.pushManager.getSubscription();

                if (!subscription) {
                    text = "Bildirimleri Yeniden Etkinleştir";
                    hint = "Cihaz izni açık, ancak bildirim bağlantısının yenilenmesi gerekiyor.";
                    actionText = "Yeniden Etkinleştir";
                    state = "resubscribe";
                } else {
                    const publicKey = await getPublicKey().catch(() => null);
                    const keyMismatch = publicKey && isApplicationServerKeyMismatch(subscription, publicKey);

                    if (!publicKey) {
                        text = "Sunucuda yapılandırılmamış";
                        hint = "Bildirim anahtarları henüz tanımlanmamış.";
                        state = "unsupported";
                    } else if (keyMismatch || subscriptionSyncFailed) {
                        text = "Bildirimleri Yeniden Etkinleştir";
                        hint = "Cihaz izni açık, ancak bildirim bağlantısının yenilenmesi gerekiyor.";
                        actionText = "Yeniden Etkinleştir";
                        state = "resubscribe";
                    } else {
                        text = "Açık";
                        hint = "Bu cihazda anlık bildirimler açık.";
                        state = "active";
                    }
                }
            }
        } else if (text === "Açık") {
            state = "active";
        } else if (text.includes("Yeniden")) {
            actionText = "Yeniden Etkinleştir";
            state = "resubscribe";
        } else if (text.includes("engellendi")) {
            state = "denied";
        } else if (text.includes("desteklemiyor") || text.includes("Ana Ekrana")) {
            state = "unsupported";
        }

        statusNodes.forEach(node => {
            node.textContent = text;
            node.dataset.state = text;
        });

        hintNodes.forEach(node => {
            node.textContent = hint || "Destek ve fatura işlemlerindeki gelişmelerden haberdar olun.";
        });

        openButtons.forEach(button => {
            button.textContent = actionText;
            button.hidden = state === "active" || state === "denied" || state === "unsupported";
        });

        closeButtons.forEach(button => {
            button.hidden = state !== "active";
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

    function isApplicationServerKeyMismatch(subscription, publicKey) {
        const currentKey = subscription?.options?.applicationServerKey;
        if (!currentKey || !publicKey) return false;

        const expected = urlBase64ToUint8Array(publicKey);
        const actual = new Uint8Array(currentKey);

        if (actual.length !== expected.length) return true;

        for (let i = 0; i < actual.length; i++) {
            if (actual[i] !== expected[i]) return true;
        }

        return false;
    }

    function showStatusMessage(message, type) {
        if (typeof window.showToast === "function") {
            window.showToast(message, type);
        }
    }

    async function readErrorMessage(response, fallback) {
        try {
            const data = await response.json();
            return data.message || fallback;
        } catch {
            return fallback;
        }
    }

    function getUnsupportedMessage() {
        return "Bu cihazda tarayıcı bildirimi desteklenmiyor.";
    }

    function getIosInstallStatus() {
        return "Ana Ekrana ekleyin";
    }

    function getIosInstallHint() {
        return "iPhone'da bildirim almak için Safari'deki Paylaş menüsünden Sente360'ı Ana Ekrana ekleyin ve uygulamayı oradan açın.";
    }

    function isIosBrowserTab() {
        return isIosDevice() && !isStandaloneApp();
    }

    function isIosDevice() {
        return /iPad|iPhone|iPod/.test(navigator.userAgent) ||
            (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
    }

    function isStandaloneApp() {
        return window.matchMedia?.("(display-mode: standalone)")?.matches ||
            window.navigator.standalone === true;
    }
})();
