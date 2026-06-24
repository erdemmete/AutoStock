(function () {
    if (window.SentePwaInstall) return;

    const installMarkerKey = "sente360:pwa-installed";
    const state = {
        deferredPrompt: null,
        installed: detectInstalled(),
        serviceWorkerPromise: null,
        activeElement: null
    };

    window.addEventListener("beforeinstallprompt", function (event) {
        event.preventDefault();
        setInstallMarker(false);
        state.deferredPrompt = event;
        state.installed = false;
        syncUi();
        notifyStateChanged();
    });

    window.addEventListener("appinstalled", function () {
        state.deferredPrompt = null;
        state.installed = true;
        setInstallMarker(true);
        syncUi();
        closeHelp();
        notifyStateChanged();
        showMessage("Sente360 cihazınıza yüklendi.");
    });

    document.addEventListener("DOMContentLoaded", function () {
        syncUi();
        bindUi();
        ensureServiceWorker();
    });

    window.SentePwaInstall = {
        getState: function () {
            return {
                installed: isInstalled(),
                canPrompt: !!state.deferredPrompt
            };
        },
        install,
        isInstalled,
        ensureServiceWorker
    };

    function bindUi() {
        document.addEventListener("click", async function (event) {
            const trigger = event.target.closest("[data-pwa-install]");

            if (trigger) {
                event.preventDefault();
                trigger.closest("details")?.removeAttribute("open");
                await install();
                return;
            }

            if (event.target.matches("[data-pwa-close]") ||
                event.target.matches("[data-pwa-overlay]")) {
                closeHelp();
            }
        });

        document.addEventListener("keydown", function (event) {
            const overlay = document.querySelector("[data-pwa-overlay]");

            if (!overlay || overlay.hidden) return;

            if (event.key === "Escape") {
                closeHelp();
                return;
            }

            if (event.key === "Tab") {
                trapFocus(event, overlay);
            }
        });
    }

    async function install() {
        if (isInstalled()) {
            syncUi();
            return false;
        }

        if (isIosDevice()) {
            showHelp(isIosSafari() ? "ios-safari" : "ios-other");
            return false;
        }

        const promptEvent = state.deferredPrompt;

        if (!promptEvent) {
            showHelp("manual");
            return false;
        }

        state.deferredPrompt = null;
        syncUi();

        try {
            await promptEvent.prompt();
            const choice = await promptEvent.userChoice;

            if (choice?.outcome === "accepted") {
                state.installed = true;
                setInstallMarker(true);
                syncUi();
                notifyStateChanged();
                showMessage("Sente360 cihazınıza yüklendi.");
                return true;
            }
        } catch {
            showHelp("manual");
        }

        syncUi();
        notifyStateChanged();
        return false;
    }

    function syncUi() {
        const installed = isInstalled();

        document.querySelectorAll("[data-pwa-install]").forEach(function (element) {
            element.hidden = installed;
            element.setAttribute("aria-hidden", installed ? "true" : "false");
        });
    }

    function isInstalled() {
        state.installed = state.installed || detectInstalled();
        return state.installed;
    }

    function detectInstalled() {
        return window.matchMedia?.("(display-mode: standalone)")?.matches === true ||
            window.navigator.standalone === true ||
            document.referrer.startsWith("android-app://") ||
            getInstallMarker();
    }

    function getInstallMarker() {
        try {
            return window.localStorage.getItem(installMarkerKey) === "true";
        } catch {
            return false;
        }
    }

    function setInstallMarker(installed) {
        try {
            if (installed) {
                window.localStorage.setItem(installMarkerKey, "true");
            } else {
                window.localStorage.removeItem(installMarkerKey);
            }
        } catch {
            // Kurulum tespiti platform sinyalleriyle çalışmaya devam eder.
        }
    }

    async function ensureServiceWorker() {
        if (!("serviceWorker" in navigator)) return null;

        if (!state.serviceWorkerPromise) {
            state.serviceWorkerPromise = (async function () {
                const existing = await navigator.serviceWorker.getRegistration("/");
                return existing || navigator.serviceWorker.register("/sw.js", { scope: "/" });
            })().catch(function () {
                state.serviceWorkerPromise = null;
                return null;
            });
        }

        return state.serviceWorkerPromise;
    }

    function showHelp(mode) {
        const overlay = ensureHelpDialog();
        const title = overlay.querySelector("[data-pwa-title]");
        const intro = overlay.querySelector("[data-pwa-intro]");
        const steps = overlay.querySelector("[data-pwa-steps]");
        const content = getHelpContent(mode);

        title.textContent = content.title;
        intro.textContent = content.intro;
        steps.replaceChildren(...content.steps.map(function (text) {
            const item = document.createElement("li");
            item.textContent = text;
            return item;
        }));

        state.activeElement = document.activeElement;
        overlay.hidden = false;
        document.body.classList.add("sente-pwa-dialog-open");
        overlay.querySelector("[data-pwa-close]")?.focus();
    }

    function closeHelp() {
        const overlay = document.querySelector("[data-pwa-overlay]");

        if (!overlay || overlay.hidden) return;

        overlay.hidden = true;
        document.body.classList.remove("sente-pwa-dialog-open");
        state.activeElement?.focus?.();
        state.activeElement = null;
    }

    function ensureHelpDialog() {
        const existing = document.querySelector("[data-pwa-overlay]");
        if (existing) return existing;

        const overlay = document.createElement("div");
        overlay.className = "sente-pwa-overlay";
        overlay.hidden = true;
        overlay.setAttribute("data-pwa-overlay", "");
        overlay.innerHTML = `
            <section class="sente-pwa-dialog"
                     role="dialog"
                     aria-modal="true"
                     aria-labelledby="sentePwaTitle">
                <header class="sente-pwa-dialog-header">
                    <h2 id="sentePwaTitle" data-pwa-title></h2>
                    <button type="button"
                            class="sente-pwa-close"
                            data-pwa-close
                            aria-label="Kapat">×</button>
                </header>
                <div class="sente-pwa-dialog-body">
                    <p data-pwa-intro></p>
                    <ol class="sente-pwa-steps" data-pwa-steps></ol>
                </div>
            </section>`;
        document.body.appendChild(overlay);
        return overlay;
    }

    function getHelpContent(mode) {
        if (mode === "ios-safari") {
            return {
                title: "Sente360’ı Ana Ekrana Ekle",
                intro: "Sente360’a ana ekranınızdan hızlıca ulaşabilirsiniz.",
                steps: [
                    "Safari’de Paylaş düğmesine dokunun.",
                    "Ana Ekrana Ekle seçeneğini seçin.",
                    "Ekle ile tamamlayın."
                ]
            };
        }

        if (mode === "ios-other") {
            return {
                title: "Sente360’ı Ana Ekrana Ekle",
                intro: "iPhone’da Sente360’ı uygulama olarak eklemek için bu sayfayı Safari’de açın.",
                steps: [
                    "Sayfayı Safari’de açın.",
                    "Paylaş düğmesine dokunun.",
                    "Ana Ekrana Ekle seçeneğini seçip Ekle ile tamamlayın."
                ]
            };
        }

        return {
            title: "Sente360’ı Yükle",
            intro: "Tarayıcı menüsünden uygulama kurulumunu tamamlayabilirsiniz.",
            steps: [
                "Tarayıcı menüsünü açın.",
                "Uygulamayı yükle veya Ana ekrana ekle seçeneğini kullanın.",
                "Kurulumu onaylayın."
            ]
        };
    }

    function isIosDevice() {
        return /iPad|iPhone|iPod/.test(navigator.userAgent) ||
            (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
    }

    function isIosSafari() {
        return isIosDevice() &&
            /Safari/i.test(navigator.userAgent) &&
            !/CriOS|FxiOS|EdgiOS|OPiOS/i.test(navigator.userAgent);
    }

    function trapFocus(event, overlay) {
        const focusable = Array.from(overlay.querySelectorAll(
            "button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex='-1'])"
        ));

        if (!focusable.length) return;

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

    function notifyStateChanged() {
        window.dispatchEvent(new CustomEvent("sente:pwa-statechange", {
            detail: window.SentePwaInstall.getState()
        }));
    }

    function showMessage(message) {
        if (typeof window.showToast === "function") {
            window.showToast(message, "success");
            return;
        }

        const toast = document.createElement("div");
        toast.className = "sente-pwa-toast";
        toast.setAttribute("role", "status");
        toast.textContent = message;
        document.body.appendChild(toast);
        window.setTimeout(function () {
            toast.remove();
        }, 3200);
    }
})();
