(function () {
    const root = document.querySelector("[data-entity-edit-lock]");
    if (!root || window.SenteEntityEditLock) return;

    const entityType = root.dataset.entityType;
    const entityId = Number(root.dataset.entityId);
    const serviceRecordId = root.dataset.serviceRecordId ? Number(root.dataset.serviceRecordId) : null;
    const canForceRelease = root.dataset.canForceRelease === "true";
    const sessionKey = `sente:edit-lock:${entityType}:${entityId}`;
    const recoverableCodes = new Set(["EDIT_LOCK_MISSING", "EDIT_LOCK_EXPIRED"]);
    const lostCodes = new Set(["EDIT_LOCK_INVALID", "EDIT_LOCK_HELD_BY_ANOTHER_USER"]);

    const state = {
        token: sessionStorage.getItem(sessionKey),
        isEditable: false,
        heartbeatTimer: null,
        recoveryPromise: null,
        lockWasLost: false,
        lastLifecycleCheckAt: 0
    };

    const originalFetch = window.fetch.bind(window);

    window.SenteEntityEditLock = {
        getToken: () => state.token,
        isEditable: () => state.isEditable,
        retry: () => recover({ allowAcquire: !state.lockWasLost, manual: true }),
        release
    };

    setControlsDisabled(true);
    window.fetch = lockedFetch;

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => recover({ allowAcquire: true, initial: true }), { once: true });
    } else {
        recover({ allowAcquire: true, initial: true });
    }

    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "visible") lifecycleRecover();
    });
    window.addEventListener("pageshow", () => lifecycleRecover(true));
    window.addEventListener("online", () => lifecycleRecover(true));
    window.addEventListener("focus", () => lifecycleRecover());
    window.addEventListener("pagehide", event => {
        if (!event.persisted) release();
    });

    async function lockedFetch(input, init) {
        const requestInit = init ? { ...init } : {};
        const retrySafe = requestInit.senteLockRetrySafe === true;
        delete requestInit.senteLockRetrySafe;

        const method = (requestInit.method || (input instanceof Request ? input.method : "GET")).toUpperCase();
        const isMutation = method !== "GET" && method !== "HEAD";

        if (state.token && isMutation) {
            requestInit.headers = new Headers(requestInit.headers || (input instanceof Request ? input.headers : {}));
            requestInit.headers.set("X-Sente-Edit-Lock-Token", state.token);

            if (serviceRecordId) {
                requestInit.headers.set("X-Sente-ServiceRecord-Id", String(serviceRecordId));
            }
        }

        const response = await originalFetch(input, requestInit);
        if (!isMutation || response.status !== 409) return response;

        const payload = await response.clone().json().catch(() => null);
        const errorCode = payload?.errorCode;
        if (!errorCode || (!recoverableCodes.has(errorCode) && !lostCodes.has(errorCode))) return response;

        if (lostCodes.has(errorCode)) {
            loseLock(payload?.errorMessage);
            return response;
        }

        const recovered = await recover({ allowAcquire: true, reason: errorCode });
        if (!recovered || !retrySafe || requestInit.__senteLockRetried) return response;

        const retryInit = {
            ...requestInit,
            __senteLockRetried: true,
            headers: new Headers(requestInit.headers || {})
        };
        retryInit.headers.set("X-Sente-Edit-Lock-Token", state.token);

        return originalFetch(input, retryInit);
    }

    function lifecycleRecover(force) {
        if (document.visibilityState === "hidden" || !navigator.onLine) return;

        const now = Date.now();
        if (!force && now - state.lastLifecycleCheckAt < 1500) return;
        state.lastLifecycleCheckAt = now;

        recover({ allowAcquire: !state.lockWasLost, reason: "LIFECYCLE" });
    }

    async function recover(options) {
        if (state.recoveryPromise) return state.recoveryPromise;

        state.recoveryPromise = performRecovery(options)
            .finally(() => {
                state.recoveryPromise = null;
            });

        return state.recoveryPromise;
    }

    async function performRecovery({ allowAcquire, initial, manual } = {}) {
        setRecoveryState();

        if (state.lockWasLost && !manual) {
            setReadOnly("Bu kayıttaki düzenleme yetkiniz kaldırıldı. Güncel verileri görmek için sayfayı yenileyin.", true);
            return false;
        }

        if (state.token) {
            const heartbeatResult = await heartbeatRequest();
            if (heartbeatResult?.isSuccess) {
                enableEditing();
                return true;
            }

            const code = heartbeatResult?.errorCode;
            if (lostCodes.has(code)) {
                loseLock(heartbeatResult?.errorMessage);
                return false;
            }

            sessionStorage.removeItem(sessionKey);
            state.token = null;

            if (code && !recoverableCodes.has(code)) {
                setReadOnly("Düzenleme durumu doğrulanamadı. Bağlantınızı kontrol edip tekrar deneyin.");
                return false;
            }
        }

        const status = await getStatus();
        if (!status) {
            setReadOnly("Düzenleme durumu kontrol edilemedi. Bağlantınızı kontrol edip tekrar deneyin.");
            return false;
        }

        if (status.isSuccess && status.data?.isLockedByAnotherUser) {
            applyLockState(status.data);
            return false;
        }

        if (!allowAcquire) {
            setReadOnly("Bu kaydı yeniden düzenlemek için güncel verilerle sayfayı yenileyin.", true);
            return false;
        }

        const acquired = await postJson("/EntityEditLocks/Acquire", {
            entityType,
            entityId,
            lockToken: state.token
        });

        if (!acquired?.isSuccess || !acquired.data) {
            if (acquired?.errorCode === "EDIT_LOCK_HELD_BY_ANOTHER_USER" && acquired.data) {
                applyLockState(acquired.data);
            } else {
                setReadOnly(acquired?.errorMessage || "Bu kayıt şu anda düzenlemeye açılamadı. Lütfen tekrar deneyin.");
            }
            return false;
        }

        state.lockWasLost = false;
        applyLockState(acquired.data);
        if (!initial) {
            window.showToast?.("Düzenleme bağlantısı yenilendi.", "success");
        }
        return true;
    }

    async function heartbeat() {
        if (!state.token || !state.isEditable || document.visibilityState === "hidden") return;

        const result = await heartbeatRequest();
        if (result?.isSuccess) return;

        const code = result?.errorCode;
        if (lostCodes.has(code)) {
            loseLock(result?.errorMessage);
            return;
        }

        if (recoverableCodes.has(code)) {
            sessionStorage.removeItem(sessionKey);
            state.token = null;
            await recover({ allowAcquire: true, reason: code });
            return;
        }

        setReadOnly("Düzenleme bağlantısı yenilenemedi. İnternet bağlantınızı kontrol edip tekrar deneyin.");
    }

    function heartbeatRequest() {
        return postJson("/EntityEditLocks/Heartbeat", {
            entityType,
            entityId,
            lockToken: state.token
        });
    }

    function getStatus() {
        const url = `/EntityEditLocks/Status?entityType=${encodeURIComponent(entityType)}&entityId=${encodeURIComponent(entityId)}`;
        return getJson(url);
    }

    function release() {
        if (!state.token) return;

        clearInterval(state.heartbeatTimer);
        const payload = JSON.stringify({ entityType, entityId, lockToken: state.token });

        try {
            const sent = navigator.sendBeacon?.(
                "/EntityEditLocks/Release",
                new Blob([payload], { type: "application/json" }));
            if (!sent) throw new Error("Beacon unavailable");
        } catch {
            originalFetch("/EntityEditLocks/Release", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: payload,
                keepalive: true
            }).catch(function () { });
        }

        sessionStorage.removeItem(sessionKey);
        state.token = null;
        state.isEditable = false;
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

    function enableEditing() {
        state.isEditable = true;
        removeBanner();
        root.classList.remove("sente-edit-readonly");
        setControlsDisabled(false);
        clearInterval(state.heartbeatTimer);
        state.heartbeatTimer = setInterval(heartbeat, 35000);
    }

    function loseLock(message) {
        state.lockWasLost = true;
        sessionStorage.removeItem(sessionKey);
        state.token = null;
        setReadOnly(message || "Bu kayıttaki düzenleme yetkiniz kaldırıldı. Güncel verileri görmek için sayfayı yenileyin.", true);
    }

    function setRecoveryState() {
        state.isEditable = false;
        clearInterval(state.heartbeatTimer);
        root.classList.add("sente-edit-readonly");
        setControlsDisabled(true);
        showBanner("Düzenleme durumu kontrol ediliyor...", false, false);
    }

    function setReadOnly(message, reloadOnly) {
        state.isEditable = false;
        clearInterval(state.heartbeatTimer);
        root.classList.add("sente-edit-readonly");
        showBanner(message, !reloadOnly, reloadOnly);
        setControlsDisabled(true);
    }

    function setControlsDisabled(disabled) {
        root.querySelectorAll("button, input, select, textarea").forEach(function (element) {
            if (element.matches("[data-edit-lock-control]")) return;
            if (element.closest("[data-edit-lock-banner]")) return;
            element.disabled = disabled;
        });
    }

    function showBanner(message, allowRetry = true, reloadOnly = false) {
        removeBanner();

        const banner = document.createElement("div");
        banner.className = "sente-edit-lock-banner";
        banner.setAttribute("data-edit-lock-banner", "true");
        banner.innerHTML = `
            <div>
                <strong>${escapeHtml(message)}</strong>
                <span>Sayfayı görüntüleyebilirsiniz; güvenli düzenleme bağlantısı kurulana kadar değişiklik yapılamaz.</span>
            </div>
            <div class="sente-edit-lock-actions">
                ${allowRetry ? '<button type="button" data-edit-lock-control data-edit-lock-retry>Tekrar Kontrol Et</button>' : ''}
                ${reloadOnly ? '<button type="button" data-edit-lock-control data-edit-lock-reload>Güncel Verileri Yükle</button>' : ''}
                ${canForceRelease && allowRetry ? '<button type="button" data-edit-lock-control data-edit-lock-force>Kilidi Kaldır</button>' : ''}
            </div>`;

        root.prepend(banner);
        banner.querySelector("[data-edit-lock-retry]")?.addEventListener("click", () => recover({ allowAcquire: true, manual: true }));
        banner.querySelector("[data-edit-lock-reload]")?.addEventListener("click", () => window.location.reload());
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
            state.lockWasLost = false;
            await recover({ allowAcquire: true, manual: true });
        } else {
            setReadOnly(result?.errorMessage || "Kilit kaldırılamadı.");
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

    async function getJson(url) {
        try {
            const response = await originalFetch(url, { headers: { "Accept": "application/json" } });
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
