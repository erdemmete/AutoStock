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

        document.addEventListener('click', function (event) {
            const menu = event.target.closest('.srx-request-menu');
            const menuAction = event.target.closest('.srx-request-menu-list button');
            const header = event.target.closest('.srx-request-card-header');
            const directToggle = event.target.closest('.srx-request-summary[data-action="toggle-request"]');

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
   QR scanner modal override
   Eski inline QR alanı yerine kapatılabilir modal kullanır.
   ========================================================= */

    (function () {
        let qrModalScanner = null;
        let qrModalLocked = false;
        let qrModalVehicleId = null;
        let qrModalButton = null;

        function getQrModal() {
            return document.getElementById("qrAssignModal");
        }

        function getQrResultBox() {
            return document.getElementById("qr-modal-result");
        }

        function setQrResult(message) {
            const resultBox = getQrResultBox();

            if (resultBox) {
                resultBox.textContent = message;
            }
        }

        function resetQrButton(text = "QR Tara") {
            if (!qrModalButton) {
                return;
            }

            qrModalButton.disabled = false;
            qrModalButton.textContent = text;
        }

        async function stopQrModalScanner() {
            if (!qrModalScanner) {
                return;
            }

            try {
                await qrModalScanner.stop();
            } catch {
                // Kamera zaten kapalı olabilir.
            }

            try {
                qrModalScanner.clear();
            } catch {
                // Kritik değil.
            }

            qrModalScanner = null;
        }

        async function assignQrCodeFromModal(code) {
            if (!qrModalVehicleId || !code) {
                setQrResult("QR bilgisi okunamadı.");
                qrModalLocked = false;
                resetQrButton("Tekrar Tara");
                return;
            }

            setQrResult("QR kod atanıyor...");

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

                    setQrResult(errorText || "QR atanırken hata oluştu.");
                    qrModalLocked = false;
                    resetQrButton("Tekrar Tara");
                    return;
                }

                setQrResult("QR kod araca başarıyla atandı.");
                resetQrButton("QR Atandı");

                if (typeof showToast === "function") {
                    showToast("QR kod başarıyla eşleştirildi.", "success");
                }

                window.setTimeout(() => {
                    closeQrAssignModal();
                }, 750);
            }
            catch {
                setQrResult("QR atanırken beklenmeyen bir hata oluştu.");
                qrModalLocked = false;
                resetQrButton("Tekrar Tara");
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

            if (typeof Html5Qrcode === "undefined") {
                setQrResult("QR okuyucu yüklenemedi.");

                if (typeof showToast === "function") {
                    showToast("QR okuyucu yüklenemedi.", "error");
                }

                return;
            }

            qrModalVehicleId = vehicleId;
            qrModalButton = button || null;
            qrModalLocked = false;

            if (qrModalButton) {
                qrModalButton.disabled = true;
                qrModalButton.textContent = "Kamera açılıyor...";
            }

            modal.classList.add("show");
            modal.setAttribute("aria-hidden", "false");
            document.body.style.overflow = "hidden";

            setQrResult("Kamera açılıyor...");

            await stopQrModalScanner();

            try {
                qrModalScanner = new Html5Qrcode("qr-modal-reader");

                await qrModalScanner.start(
                    { facingMode: "environment" },
                    {
                        fps: 10,
                        qrbox: {
                            width: 240,
                            height: 240
                        }
                    },
                    async decodedText => {
                        if (qrModalLocked) {
                            return;
                        }

                        qrModalLocked = true;

                        setQrResult(`QR okundu: ${decodedText}`);

                        await stopQrModalScanner();

                        await assignQrCodeFromModal(decodedText);
                    }
                );

                if (qrModalButton) {
                    qrModalButton.textContent = "Taranıyor...";
                }

                setQrResult("Kamera açık. QR kodu kameraya gösterin.");
            }
            catch (error) {
                console.error(error);

                setQrResult("Kamera hatası: " + (error?.message || error));
                qrModalLocked = false;
                resetQrButton("Tekrar Tara");
            }
        };

        window.closeQrAssignModal = async function () {
            const modal = getQrModal();
            const buttonToFocus = qrModalButton;

            // Modal içindeki kapatma butonu focus'ta kalmasın.
            if (modal && modal.contains(document.activeElement)) {
                document.activeElement.blur();
            }

            await stopQrModalScanner();

            qrModalLocked = false;
            qrModalVehicleId = null;

            resetQrButton("QR Tara");
            qrModalButton = null;

            setQrResult("Kamera hazırlanıyor.");

            if (modal) {
                modal.classList.remove("show");
                modal.setAttribute("aria-hidden", "true");
            }

            document.body.style.overflow = "";

            // Focus'u modal dışındaki QR butonuna geri ver.
            if (buttonToFocus && typeof buttonToFocus.focus === "function") {
                window.setTimeout(() => {
                    buttonToFocus.focus({ preventScroll: true });
                }, 0);
            }
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
            if (event.key === "Escape") {
                closeQrAssignModal();
            }
        });
    })();
})();
