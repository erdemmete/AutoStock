(function () {
    function getCards() {
        return Array.from(document.querySelectorAll('.srx-request-card[data-request-id]'));
    }

    function syncCard(card) {
        if (!card) return;

        const isOpen = card.dataset.expanded === 'true';
        const textTargets = card.querySelectorAll('[data-role="toggle-request-text"]');
        const toggleButtons = card.querySelectorAll('[data-action="toggle-request"]');

        textTargets.forEach(target => {
            target.textContent = isOpen ? 'Detayı kapat' : 'Detayı aç';
        });

        toggleButtons.forEach(button => {
            button.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });
    }

    function syncAllCards() {
        getCards().forEach(syncCard);
    }

    (function () {
        function closeRequestMenus(exceptMenu) {
            document.querySelectorAll(".srx-request-menu[open]").forEach(menu => {
                if (menu !== exceptMenu) {
                    menu.removeAttribute("open");
                    menu.closest(".srx-request-card")?.classList.remove("srx-menu-open");
                }
            });
        }

        function syncMenuClasses() {
            document.querySelectorAll(".srx-request-card").forEach(card => {
                const hasOpenMenu = !!card.querySelector(".srx-request-menu[open]");
                card.classList.toggle("srx-menu-open", hasOpenMenu);
            });
        }

        document.addEventListener("DOMContentLoaded", function () {
            document.addEventListener("click", function (event) {
                const clickedMenu = event.target.closest(".srx-request-menu");

                if (!clickedMenu) {
                    closeRequestMenus();
                }

                const interactive = event.target.closest("button, a, input, select, textarea, details, summary, label");

                if (interactive) {
                    return;
                }

                const header = event.target.closest(".srx-request-card-header");

                if (!header) {
                    return;
                }

                const card = header.closest(".srx-request-card");
                const toggle = card?.querySelector(".srx-request-summary[data-action='toggle-request']");

                if (toggle) {
                    toggle.click();
                }
            }, true);

            document.addEventListener("toggle", function (event) {
                const menu = event.target;

                if (!(menu instanceof HTMLDetailsElement)) {
                    return;
                }

                if (!menu.classList.contains("srx-request-menu")) {
                    return;
                }

                if (menu.open) {
                    closeRequestMenus(menu);
                }

                syncMenuClasses();
            }, true);

            document.addEventListener("click", function (event) {
                const menuAction = event.target.closest(".srx-request-menu-list button");

                if (!menuAction) {
                    return;
                }

                const menu = menuAction.closest(".srx-request-menu");

                if (menu) {
                    menu.removeAttribute("open");
                }

                syncMenuClasses();
            });

            document.addEventListener("keydown", function (event) {
                if (event.key === "Escape") {
                    closeRequestMenus();
                }
            });
        });
    })();



    function closeRequestMenus(exceptMenu) {
        document.querySelectorAll('.srx-request-menu[open]').forEach(menu => {
            if (menu !== exceptMenu) {
                menu.removeAttribute('open');
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        syncAllCards();
        const summaryMenu = document.getElementById("summaryActionMenu");

        function closeSummaryMenu() {
            summaryMenu?.removeAttribute("open");
        }

        document.addEventListener('click', function (event) {
            const menu = event.target.closest('.srx-request-menu');
            const menuAction = event.target.closest('.srx-request-menu-list button');
            const header = event.target.closest('.srx-request-card-header');
            const directToggle = event.target.closest('.srx-request-summary[data-action="toggle-request"]');
            const summaryMenuAction = event.target.closest('#summaryActionMenu .status-btn');

            if (summaryMenuAction) {
                closeSummaryMenu();
                return;
            }

            if (summaryMenu &&
                summaryMenu.open &&
                !event.target.closest('#summaryActionMenu')) {
                closeSummaryMenu();
            }

            if (!menu) {
                closeRequestMenus();
            }

            if (menuAction) {
                menuAction.closest('.srx-request-menu')?.removeAttribute('open');
                window.setTimeout(syncAllCards, 0);
                return;
            }

            if (header && !menu && !directToggle) {
                const toggle = header.querySelector('.srx-request-summary[data-action="toggle-request"]');
                toggle?.click();
                window.setTimeout(syncAllCards, 0);
                return;
            }

            if (directToggle) {
                closeRequestMenus();
                window.setTimeout(syncAllCards, 0);
            }
        });

        document.addEventListener('toggle', function (event) {
            const menu = event.target;

            if (!(menu instanceof HTMLDetailsElement)) return;
            if (!menu.classList.contains('srx-request-menu')) return;

            if (menu.open) {
                closeRequestMenus(menu);
            }
        }, true);

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeRequestMenus();
                closeSummaryMenu();
            }
        });

        const requestList = document.getElementById('requestList');

        if (requestList) {
            const observer = new MutationObserver(function () {
                window.setTimeout(syncAllCards, 30);
            });

            observer.observe(requestList, {
                childList: true,
                subtree: true
            });
        }
    });

    /* =========================================================
       QR assign scanner modal
       Mobil-safe kamera overlay'i ve stream cleanup.
       ========================================================= */

    (function () {
        let qrModalScanner = null;
        let qrModalLocked = false;
        let qrModalVehicleId = null;
        let qrModalButton = null;
        let qrModalClosing = false;

        function getQrModal() {
            return document.getElementById("qrAssignModal");
        }

        function getQrReader() {
            return document.getElementById("qr-modal-reader");
        }

        function getQrErrorPanel() {
            return document.getElementById("qr-modal-error");
        }

        function getQrResultBox() {
            return document.getElementById("qr-modal-result");
        }

        function resetQrButton(text = "QR Tara") {
            if (!qrModalButton) {
                return;
            }

            qrModalButton.disabled = false;

            if (qrModalButton.id === "qrChangeButton") {
                qrModalButton.textContent = "Değiştir";
                return;
            }

            const title = qrModalButton.querySelector(".srx-quick-title");
            if (title) {
                title.textContent = text;
                return;
            }

            qrModalButton.textContent = text;
        }

        function setQrBoundState(isBound) {
            const stack = document.getElementById("qrActionStack");
            const scanButton = document.getElementById("qrScanButton");
            const boundControl = document.getElementById("qrBoundControl");
            const changeButton = document.getElementById("qrChangeButton");

            if (stack) {
                stack.dataset.qrBound = String(isBound);
            }

            if (scanButton) {
                scanButton.classList.toggle("is-hidden", isBound);
                scanButton.disabled = false;
                const scanTitle = scanButton.querySelector(".srx-quick-title");
                if (scanTitle) {
                    scanTitle.textContent = "QR Tara";
                } else {
                    scanButton.textContent = "QR Tara";
                }
            }

            if (boundControl) {
                boundControl.classList.toggle("is-hidden", !isBound);
            }

            if (changeButton) {
                changeButton.disabled = false;
                changeButton.textContent = "Değiştir";
            }
        }

        function getQrAssignMessageFromResponseText(text) {
            const fallback = "QR kod araca bağlanamadı.";

            if (!text) {
                return fallback;
            }

            try {
                const payload = JSON.parse(text);
                const messages = payload?.errorMessages || payload?.ErrorMessages;
                return payload?.errorMessage ||
                    payload?.ErrorMessage ||
                    (Array.isArray(messages) ? messages[0] : null) ||
                    fallback;
            } catch {
                return text;
            }
        }

        function setQrError(message) {
            const errorPanel = getQrErrorPanel();
            const resultBox = getQrResultBox();

            if (resultBox) {
                resultBox.textContent = message || "Kamera açılamadı.";
            }

            if (errorPanel) {
                errorPanel.hidden = false;
            }
        }

        function clearQrError() {
            const errorPanel = getQrErrorPanel();
            const resultBox = getQrResultBox();

            if (resultBox) {
                resultBox.textContent = "";
            }

            if (errorPanel) {
                errorPanel.hidden = true;
            }
        }

        function isQrModalVisible() {
            return getQrModal()?.classList.contains("show") === true;
        }

        function hideQrModalSurface() {
            const modal = getQrModal();

            clearQrError();

            if (modal) {
                modal.classList.remove("show");
                modal.setAttribute("aria-hidden", "true");
            }

            document.body.classList.remove("srx-qr-modal-open");
        }

        function showQrAssignError(message) {
            const text = message || "QR atanırken hata oluştu.";

            if (isQrModalVisible()) {
                setQrError(text);
                return;
            }

            if (typeof showToast === "function") {
                showToast(text, "error");
            }
        }

        function finishQrModalSession(buttonText = "QR Tara") {
            qrModalLocked = false;
            qrModalVehicleId = null;
            resetQrButton(buttonText);
            qrModalButton = null;
            clearQrError();
        }

        function stopQrReaderTracks() {
            const reader = getQrReader();

            reader?.querySelectorAll("video").forEach(video => {
                const stream = video.srcObject;

                if (stream && typeof stream.getTracks === "function") {
                    stream.getTracks().forEach(track => track.stop());
                }

                video.pause();
                video.srcObject = null;
                video.removeAttribute("src");
                video.load?.();
            });
        }

        async function stopQrModalScanner() {
            const activeScanner = qrModalScanner;
            const reader = getQrReader();

            qrModalScanner = null;

            if (activeScanner) {
                try {
                    await activeScanner.stop();
                } catch {
                }

                try {
                    await activeScanner.clear();
                } catch {
                }
            }

            stopQrReaderTracks();

            if (reader) {
                reader.innerHTML = "";
            }
        }

        function getQrCameraErrorMessage(error) {
            const name = error?.name || "";
            const message = String(error?.message || error || "");

            if (name.includes("NotAllowed") || message.includes("Permission")) {
                return "Kamera izni verilmedi.";
            }

            if (name.includes("NotFound") || message.includes("Requested device not found")) {
                return "Kamera bulunamadı.";
            }

            if (name.includes("NotReadable")) {
                return "Kamera başka bir uygulama tarafından kullanılıyor olabilir.";
            }

            return "Kamera açılamadı.";
        }

        async function assignQrCodeFromModal(code) {
            if (!qrModalVehicleId || !code) {
                showQrAssignError("QR bilgisi okunamadı.");
                qrModalLocked = false;
                resetQrButton("QR Tara");
                return;
            }

            const formData = new FormData();
            formData.append("vehicleId", qrModalVehicleId);
            formData.append("code", code.trim());

            try {
                const response = await fetch("/ServiceRecords/AssignQrCode", {
                    method: "POST",
                    body: formData,
                    credentials: "same-origin"
                });

                if (!response.ok) {
                    const errorText = await response.text();

                    showQrAssignError(getQrAssignMessageFromResponseText(errorText));
                    qrModalLocked = false;
                    resetQrButton("QR Tara");
                    return;
                }

                setQrBoundState(true);

                if (typeof showToast === "function") {
                    showToast("QR kod başarıyla eşleştirildi.", "success");
                }

                finishQrModalSession("QR Tara");
            }
            catch {
                showQrAssignError("QR atanırken hata oluştu.");
                qrModalLocked = false;
                resetQrButton("QR Tara");
            }
        }

        async function startQrModalCamera() {
            const modal = getQrModal();
            const reader = getQrReader();

            if (!modal || !reader || !modal.classList.contains("show") || qrModalScanner) {
                return;
            }

            if (typeof Html5Qrcode === "undefined") {
                setQrError("QR okuyucu yüklenemedi.");
                resetQrButton("QR Tara");
                return;
            }

            clearQrError();
            qrModalLocked = false;

            try {
                reader.innerHTML = "";
                qrModalScanner = new Html5Qrcode("qr-modal-reader");

                await qrModalScanner.start(
                    { facingMode: "environment" },
                    { fps: 10 },
                    async decodedText => {
                        if (qrModalLocked) {
                            return;
                        }

                        qrModalLocked = true;
                        await stopQrModalScanner();
                        hideQrModalSurface();
                        await assignQrCodeFromModal(decodedText);
                    }
                );
            }
            catch (error) {
                console.error(error);
                await stopQrModalScanner();
                setQrError(getQrCameraErrorMessage(error));
                qrModalLocked = false;
                resetQrButton("QR Tara");
            }
        }

        window.startQrScanner = async function (vehicleId, button) {
            const modal = getQrModal();

            if (!modal) {
                if (typeof showToast === "function") {
                    showToast("QR modalı bulunamadı.", "error");
                }

                return;
            }

            qrModalVehicleId = vehicleId;
            qrModalButton = button || null;
            qrModalLocked = false;

            if (qrModalButton) {
                qrModalButton.disabled = true;
            }

            modal.classList.add("show");
            modal.setAttribute("aria-hidden", "false");
            document.body.classList.add("srx-qr-modal-open");

            await stopQrModalScanner();
            await startQrModalCamera();
        };

        window.confirmChangeQrAndStart = async function (vehicleId, button) {
            const message = "Bu araca bağlı QR kod değiştirilecek. Yeni QR kodu okuttuğunuzda eski QR bağlantısı kaldırılır.";
            let confirmed = true;

            if (window.SenteConfirm && typeof window.SenteConfirm.show === "function") {
                confirmed = await window.SenteConfirm.show({
                    title: "QR kodu değiştir",
                    message,
                    confirmText: "Değiştir",
                    cancelText: "Vazgeç",
                    danger: false
                });
            } else {
                confirmed = window.confirm(message);
            }

            if (!confirmed) {
                return;
            }

            await window.startQrScanner(vehicleId, button);
        };

        window.closeQrAssignModal = async function () {
            const modal = getQrModal();
            const buttonToFocus = qrModalButton;

            if (qrModalClosing) {
                return;
            }

            qrModalClosing = true;

            if (modal && modal.contains(document.activeElement)) {
                document.activeElement.blur();
            }

            await stopQrModalScanner();

            qrModalLocked = false;
            qrModalVehicleId = null;

            resetQrButton("QR Tara");
            qrModalButton = null;
            hideQrModalSurface();
            qrModalClosing = false;

            if (buttonToFocus && typeof buttonToFocus.focus === "function") {
                window.setTimeout(() => {
                    buttonToFocus.focus({ preventScroll: true });
                }, 0);
            }
        };

        window.retryQrAssignScanner = async function () {
            clearQrError();
            await stopQrModalScanner();
            await startQrModalCamera();
        };

        document.addEventListener("click", function (event) {
            const modal = getQrModal();

            if (!modal || !modal.classList.contains("show")) {
                return;
            }

            if (event.target === modal) {
                closeQrAssignModal();
            }
        });

        document.addEventListener("keydown", function (event) {
            const modal = getQrModal();

            if (event.key === "Escape" && modal?.classList.contains("show")) {
                closeQrAssignModal();
            }
        });

        window.addEventListener("pagehide", function () {
            stopQrReaderTracks();
            void stopQrModalScanner();
        });

        window.addEventListener("beforeunload", function () {
            stopQrReaderTracks();
            void stopQrModalScanner();
        });
    })();
})();
