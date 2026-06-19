(function () {
    const root = document.querySelector("[data-entity-edit-lock]");
    if (!root || window.SenteEntityEditLock) return;

    const entityType = root.dataset.entityType;
    const entityId = Number(root.dataset.entityId);
    const serviceRecordId = root.dataset.serviceRecordId ? Number(root.dataset.serviceRecordId) : null;
    const canForceRelease = root.dataset.canForceRelease === "true";
    const sessionKey = `sente:edit-lock:${entityType}:${entityId}`;

    const state = {
        token: sessionStorage.getItem(sessionKey),
        isEditable: false,
        heartbeatTimer: null
    };

    window.SenteEntityEditLock = {
        getToken: () => state.token,
        isEditable: () => state.isEditable,
        retry: acquire,
        release
    };

    const originalFetch = window.fetch.bind(window);
    window.fetch = function (input, init) {
        const requestInit = init ? { ...init } : {};
        const method = (requestInit.method || "GET").toUpperCase();

        if (state.token && method !== "GET" && method !== "HEAD") {
            requestInit.headers = new Headers(requestInit.headers || {});
            requestInit.headers.set("X-Sente-Edit-Lock-Token", state.token);

            if (serviceRecordId) {
                requestInit.headers.set("X-Sente-ServiceRecord-Id", String(serviceRecordId));
            }
        }

        return originalFetch(input, requestInit);
    };

    document.addEventListener("DOMContentLoaded", acquire);
    window.addEventListener("pagehide", release);

    async function acquire() {
        const result = await postJson("/EntityEditLocks/Acquire", {
            entityType,
            entityId,
            lockToken: state.token
        });

        if (!result?.isSuccess || !result.data) {
            setReadOnly("Bu kayıt şu anda düzenlemeye açılamadı. Lütfen sayfayı yenileyip tekrar deneyin.");
            return;
        }

        applyLockState(result.data);
    }

    async function heartbeat() {
        if (!state.token || !state.isEditable) return;

        const result = await postJson("/EntityEditLocks/Heartbeat", {
            entityType,
            entityId,
            lockToken: state.token
        });

        if (!result?.isSuccess) {
            sessionStorage.removeItem(sessionKey);
            state.token = null;
            setReadOnly(result?.errorMessage || "Düzenleme kilidiniz süresi doldu. Sayfayı yenileyip tekrar deneyin.");
        }
    }

    function release() {
        if (!state.token) return;

        const payload = JSON.stringify({
            entityType,
            entityId,
            lockToken: state.token
        });

        try {
            navigator.sendBeacon?.("/EntityEditLocks/Release", new Blob([payload], { type: "application/json" }));
        } catch {
            fetch("/EntityEditLocks/Release", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: payload,
                keepalive: true
            }).catch(function () { });
        }
    }

    function applyLockState(data) {
        clearInterval(state.heartbeatTimer);

        if (data.isEditable && data.lockToken) {
            state.token = data.lockToken;
            state.isEditable = true;
            sessionStorage.setItem(sessionKey, state.token);
            removeBanner();
            root.classList.remove("sente-edit-readonly");
            setControlsDisabled(false);
            state.heartbeatTimer = setInterval(heartbeat, (data.heartbeatIntervalSeconds || 35) * 1000);
            return;
        }

        sessionStorage.removeItem(sessionKey);
        state.token = null;
        const name = data.lockedByDisplayName || "başka bir kullanıcı";
        setReadOnly(`Bu kayıt şu anda ${name} tarafından düzenleniyor.`);
    }

    function setReadOnly(message) {
        state.isEditable = false;
        clearInterval(state.heartbeatTimer);
        root.classList.add("sente-edit-readonly");
        showBanner(message);
        setControlsDisabled(true);
    }

    function setControlsDisabled(disabled) {
        root.querySelectorAll("button, input, select, textarea").forEach(function (element) {
            if (element.matches("[data-edit-lock-control]")) return;
            if (element.closest("[data-edit-lock-banner]")) return;
            element.disabled = disabled;
        });

        root.querySelectorAll("a[href]").forEach(function (element) {
            if (element.matches("[data-readonly-link]")) return;
            if (element.closest("[data-edit-lock-banner]")) return;
            if (disabled) {
                element.dataset.originalHref = element.getAttribute("href") || "";
                element.removeAttribute("href");
                element.setAttribute("aria-disabled", "true");
            } else if (element.dataset.originalHref) {
                element.setAttribute("href", element.dataset.originalHref);
                element.removeAttribute("aria-disabled");
                delete element.dataset.originalHref;
            }
        });
    }

    function showBanner(message) {
        removeBanner();

        const banner = document.createElement("div");
        banner.className = "sente-edit-lock-banner";
        banner.setAttribute("data-edit-lock-banner", "true");
        banner.innerHTML = `
            <div>
                <strong>${escapeHtml(message)}</strong>
                <span>Sayfayı görüntüleyebilirsiniz, düzenleme işlemleri kilit kalkana kadar kapalıdır.</span>
            </div>
            <div class="sente-edit-lock-actions">
                <button type="button" data-edit-lock-control data-edit-lock-retry>Kilidi tekrar kontrol et</button>
                ${canForceRelease ? '<button type="button" data-edit-lock-control data-edit-lock-force>Kilidi kaldır</button>' : ''}
            </div>
        `;

        root.prepend(banner);
        banner.querySelector("[data-edit-lock-retry]")?.addEventListener("click", acquire);
        banner.querySelector("[data-edit-lock-force]")?.addEventListener("click", forceRelease);
    }

    async function forceRelease() {
        const confirmed = window.SenteConfirm
            ? await SenteConfirm.show({
                title: "Düzenleme kilidini kaldır",
                message: "Bu kayıt başka bir kullanıcı tarafından düzenleniyor olabilir. Kilidi kaldırmak istediğinize emin misiniz?",
                confirmText: "Kilidi Kaldır",
                cancelText: "Vazgeç",
                danger: true
            })
            : window.confirm("Düzenleme kilidini kaldırmak istiyor musunuz?");

        if (!confirmed) return;

        const result = await postJson("/EntityEditLocks/ForceRelease", { entityType, entityId });

        if (result?.isSuccess) {
            await acquire();
        } else {
            showBanner(result?.errorMessage || "Kilit kaldırılamadı.");
        }
    }

    function removeBanner() {
        root.querySelector("[data-edit-lock-banner]")?.remove();
    }

    async function postJson(url, payload) {
        try {
            const response = await originalFetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const text = await response.text();
            return text ? JSON.parse(text) : { isSuccess: response.ok };
        } catch {
            return null;
        }
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }
})();
