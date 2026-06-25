(function () {
    const shell = document.querySelector("[data-account-drawer-shell]");
    if (!shell) return;

    let activeDrawer = null;
    let activeTrigger = null;
    let accountLocations = [];
    let accountTaxOffices = [];

    const getFocusable = (root) => Array.from(root.querySelectorAll(
        'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
    )).filter((el) => el.offsetParent !== null || el === document.activeElement);

    const normalizeControlValue = (control) => {
        if (!control.name) return "";

        if (control.type === "checkbox") {
            return control.checked ? "true" : "false";
        }

        let value = String(control.value || "").trim();

        if (control.name.toLocaleLowerCase("tr-TR").includes("phone")) {
            value = formatPhone(value);
        }

        return value;
    };

    const getFormValues = (form) => {
        const values = {};

        form.querySelectorAll("input[name], select[name], textarea[name]").forEach((control) => {
            if (control.form && control.form !== form) return;

            if (control.type === "password") {
                values[control.name] = normalizeControlValue(control);
                return;
            }

            values[control.name] = normalizeControlValue(control);
        });

        return values;
    };

    const getFormSnapshot = (form) => JSON.stringify(getFormValues(form));

    const setFormSnapshot = (form) => {
        const values = getFormValues(form);
        form.dataset.initialSnapshot = JSON.stringify(values);
        form.dataset.initialValues = JSON.stringify(values);
    };

    const getDrawerForms = (drawer) => Array.from(drawer?.querySelectorAll("form[data-account-ajax-form]") || []);

    const getDirtyForm = (drawer) => getDrawerForms(drawer)
        .find((form) => form.dataset.initialSnapshot && form.dataset.initialSnapshot !== getFormSnapshot(form));

    const isFormDirty = (drawer) => Boolean(getDirtyForm(drawer));

    const hideUnsavedPrompt = (drawer = activeDrawer) => {
        drawer?.querySelector("[data-account-unsaved-prompt]")?.setAttribute("hidden", "hidden");
    };

    const showError = (form, messages) => {
        const box = form.querySelector("[data-account-form-error]");
        const list = Array.isArray(messages) ? messages : [messages];

        if (!box) {
            showToast(list.filter(Boolean).join(" ") || "İşlem tamamlanamadı.", "error");
            return;
        }

        box.textContent = list.filter(Boolean).join(" ");
        box.hidden = false;
    };

    const clearError = (form) => {
        const box = form.querySelector("[data-account-form-error]");
        if (!box) return;

        box.textContent = "";
        box.hidden = true;
    };

    const resetSensitiveFields = (drawer) => {
        drawer.querySelectorAll('input[type="password"]').forEach((input) => {
            input.value = "";
        });
    };

    const restoreFormSnapshot = (form) => {
        if (!form?.dataset.initialValues) return;

        const values = JSON.parse(form.dataset.initialValues || "{}");

        form.querySelectorAll("input[name], select[name], textarea[name]").forEach((control) => {
            if (!Object.prototype.hasOwnProperty.call(values, control.name)) return;

            if (control.type === "checkbox") {
                control.checked = values[control.name] === "true";
            } else {
                control.value = values[control.name] || "";
            }

            if (control.id === "workshopCityInput") {
                control.dataset.currentValue = control.value;
            }

            if (control.id === "workshopDistrictInput") {
                control.dataset.currentValue = control.value;
            }

            if (control.id === "workshopTaxOfficeInput") {
                control.dataset.currentValue = control.value;
            }
        });

        if (form.querySelector("#workshopTaxOfficeInput")) {
            syncTaxOfficeCityFromSelectedOffice();
        }

        fillWorkshopDistrictSelect();
        fillWorkshopTaxOfficeSelect();
        form.dataset.initialSnapshot = getFormSnapshot(form);
        resetSensitiveFields(form);
    };

    const ensureUnsavedPrompt = (drawer) => {
        let prompt = drawer.querySelector("[data-account-unsaved-prompt]");
        if (prompt) return prompt;

        const form = getDirtyForm(drawer) || drawer.querySelector("form[data-account-ajax-form]");
        if (!form) return null;

        prompt = document.createElement("section");
        prompt.className = "account-unsaved-prompt";
        prompt.setAttribute("data-account-unsaved-prompt", "");
        prompt.hidden = true;
        prompt.innerHTML = `
            <p>Kaydedilmemiş değişiklikleriniz var.</p>
            <div class="account-unsaved-actions">
                <button type="button" class="account-secondary-button" data-account-continue-editing>Düzenlemeye Devam Et</button>
                <button type="button" class="account-danger-link" data-account-discard-changes>Değişiklikleri Sil</button>
                <button type="button" class="account-primary-button" data-account-save-close>Kaydet ve Kapat</button>
            </div>`;

        const footer = form.querySelector(".account-drawer-footer");
        form.insertBefore(prompt, footer || null);

        prompt.querySelector("[data-account-continue-editing]")?.addEventListener("click", () => {
            hideUnsavedPrompt(drawer);
            const firstEditable = drawer.querySelector("input:not([type='hidden']), select, textarea, button[type='submit']");
            firstEditable?.focus?.();
        });

        prompt.querySelector("[data-account-discard-changes]")?.addEventListener("click", () => {
            getDrawerForms(drawer).forEach(restoreFormSnapshot);
            hideUnsavedPrompt(drawer);
            closeDrawer(true);
        });

        prompt.querySelector("[data-account-save-close]")?.addEventListener("click", () => {
            hideUnsavedPrompt(drawer);
            const submitButton = prompt.querySelector("[data-account-save-close]");
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.textContent = "Kaydediliyor...";
            }
            form.requestSubmit();
        });

        return prompt;
    };

    const showUnsavedPrompt = (drawer) => {
        const prompt = ensureUnsavedPrompt(drawer);
        if (!prompt) return;

        getDrawerForms(drawer).forEach(clearError);
        prompt.hidden = false;
        prompt.querySelector("[data-account-continue-editing]")?.focus?.();
    };

    const normalizeTr = (value) => String(value || "").toLocaleLowerCase("tr-TR").trim();

    const toTitleCase = (value) => String(value || "")
        .toLocaleLowerCase("tr-TR")
        .replace(/(^|\s)\S/g, (x) => x.toLocaleUpperCase("tr-TR"));

    const escapeHtml = (value) => String(value || "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");

    const getJsonField = (item, lowerName, upperName) => item?.[lowerName] ?? item?.[upperName] ?? null;
    const getCityName = (city) => getJsonField(city, "name", "Name");
    const getDistricts = (city) => getJsonField(city, "districts", "Districts") || [];
    const getTaxOfficeCity = (item) => getJsonField(item, "city", "City");
    const getTaxOfficeDistrict = (item) => getJsonField(item, "district", "District");
    const getTaxOfficeName = (item) => getJsonField(item, "name", "Name");

    const formatPhone = (value) => {
        let digits = String(value || "").replace(/\D/g, "");

        if (digits.startsWith("90") && digits.length === 12) {
            digits = digits.slice(2);
        }

        if (digits.startsWith("5") && digits.length === 10) {
            digits = `0${digits}`;
        }

        if (digits.length !== 11) {
            return String(value || "").trim();
        }

        return digits.replace(/(\d{4})(\d{3})(\d{2})(\d{2})/, "$1 $2 $3 $4");
    };

    const bindPhoneInput = (input) => {
        if (!input || input.dataset.accountPhoneBound === "true") return;

        input.dataset.accountPhoneBound = "true";
        input.addEventListener("input", () => {
            let digits = input.value.replace(/\D/g, "").slice(0, 12);

            if (digits.startsWith("90") && digits.length > 11) {
                digits = digits.slice(2);
            }

            if (digits.startsWith("5")) {
                digits = `0${digits}`.slice(0, 11);
            }

            if (digits.length > 9) {
                input.value = digits.replace(/(\d{4})(\d{3})(\d{2})(\d{0,2})/, "$1 $2 $3 $4").trim();
                return;
            }

            if (digits.length > 7) {
                input.value = digits.replace(/(\d{4})(\d{3})(\d{0,2})/, "$1 $2 $3").trim();
                return;
            }

            if (digits.length > 4) {
                input.value = digits.replace(/(\d{4})(\d{0,3})/, "$1 $2").trim();
                return;
            }

            input.value = digits;
        });
    };

    const updateProfileContact = (email, phone) => {
        const target = document.querySelector("[data-account-profile-contact]");
        if (!target) return;

        const parts = [email, phone].filter(x => x && String(x).trim());
        target.textContent = parts.join(" · ");
        target.hidden = parts.length === 0;
    };

    const fillCitySelect = (select, placeholder) => {
        if (!select) return;

        const currentValue = select.value || select.dataset.currentValue || "";
        select.innerHTML = `<option value="">${escapeHtml(placeholder)}</option>`;

        accountLocations
            .slice()
            .sort((a, b) => String(getCityName(a)).localeCompare(String(getCityName(b)), "tr"))
            .forEach((city) => {
                const name = getCityName(city);
                if (!name) return;

                select.innerHTML += `<option value="${escapeHtml(name)}">${escapeHtml(toTitleCase(name))}</option>`;
            });

        if (currentValue) {
            select.value = currentValue;
        }
    };

    const fillWorkshopCitySelect = () => {
        fillCitySelect(document.getElementById("workshopCityInput"), "İl seçiniz");
    };

    const fillWorkshopTaxOfficeCitySelect = () => {
        fillCitySelect(document.getElementById("workshopTaxOfficeCityInput"), "Vergi dairesi ili seçiniz");
    };

    const fillWorkshopDistrictSelect = () => {
        const city = document.getElementById("workshopCityInput")?.value;
        const districtSelect = document.getElementById("workshopDistrictInput");
        if (!districtSelect) return;

        const currentValue = districtSelect.value || districtSelect.dataset.currentValue || "";
        districtSelect.innerHTML = city
            ? `<option value="">İlçe seçiniz</option>`
            : `<option value="">Önce il seçiniz</option>`;
        districtSelect.disabled = !city;

        if (!city) return;

        const location = accountLocations.find(x => normalizeTr(getCityName(x)) === normalizeTr(city));
        if (!location) return;

        getDistricts(location)
            .slice()
            .sort((a, b) => String(getCityName(a)).localeCompare(String(getCityName(b)), "tr"))
            .forEach((district) => {
                const name = getCityName(district);
                if (!name) return;

                districtSelect.innerHTML += `<option value="${escapeHtml(name)}">${escapeHtml(toTitleCase(name))}</option>`;
            });

        if (currentValue && Array.from(districtSelect.options).some(x => normalizeTr(x.value) === normalizeTr(currentValue))) {
            districtSelect.value = currentValue;
        } else {
            districtSelect.dataset.currentValue = "";
            districtSelect.value = "";
        }
    };

    const fillWorkshopTaxOfficeSelect = () => {
        const city = document.getElementById("workshopTaxOfficeCityInput")?.value;
        const taxOfficeSelect = document.getElementById("workshopTaxOfficeInput");
        if (!taxOfficeSelect) return;

        const currentValue = taxOfficeSelect.value || taxOfficeSelect.dataset.currentValue || "";
        taxOfficeSelect.innerHTML = city
            ? `<option value="">Vergi dairesi seçiniz</option>`
            : `<option value="">Önce vergi dairesi ilini seçiniz</option>`;
        taxOfficeSelect.disabled = !city;

        if (!city) return;

        accountTaxOffices
            .filter(x => normalizeTr(getTaxOfficeCity(x)) === normalizeTr(city))
            .sort((a, b) => {
                const districtCompare = String(getTaxOfficeDistrict(a) || "").localeCompare(String(getTaxOfficeDistrict(b) || ""), "tr");
                if (districtCompare !== 0) return districtCompare;

                return String(getTaxOfficeName(a) || "").localeCompare(String(getTaxOfficeName(b) || ""), "tr");
            })
            .forEach((item) => {
                const name = getTaxOfficeName(item);
                const district = getTaxOfficeDistrict(item);
                const itemCity = getTaxOfficeCity(item);

                if (!name) return;

                taxOfficeSelect.innerHTML += `
                    <option value="${escapeHtml(name)}">
                        ${escapeHtml(name)}${district ? ` - ${escapeHtml(district)}` : ""}${itemCity ? ` / ${escapeHtml(itemCity)}` : ""}
                    </option>`;
            });

        if (currentValue && Array.from(taxOfficeSelect.options).some(x => normalizeTr(x.value) === normalizeTr(currentValue))) {
            taxOfficeSelect.value = currentValue;
        } else {
            taxOfficeSelect.dataset.currentValue = "";
            taxOfficeSelect.value = "";
        }
    };

    const syncTaxOfficeCityFromSelectedOffice = () => {
        const taxOfficeCitySelect = document.getElementById("workshopTaxOfficeCityInput");
        const taxOfficeSelect = document.getElementById("workshopTaxOfficeInput");
        if (!taxOfficeCitySelect || !taxOfficeSelect?.dataset.currentValue) return;

        const matchedTaxOffice = accountTaxOffices.find(x =>
            normalizeTr(getTaxOfficeName(x)) === normalizeTr(taxOfficeSelect.dataset.currentValue)
        );

        const inferredCity = getTaxOfficeCity(matchedTaxOffice);
        if (inferredCity) {
            taxOfficeCitySelect.dataset.currentValue = inferredCity;
            taxOfficeCitySelect.value = inferredCity;
        }
    };

    const initializeWorkshopLocationSelects = async () => {
        const citySelect = document.getElementById("workshopCityInput");
        const taxOfficeCitySelect = document.getElementById("workshopTaxOfficeCityInput");
        if (!citySelect || citySelect.dataset.accountLocationBound === "true") return;

        citySelect.dataset.accountLocationBound = "true";
        if (taxOfficeCitySelect) {
            taxOfficeCitySelect.dataset.accountLocationBound = "true";
        }

        try {
            const [locationsResponse, taxOfficesResponse] = await Promise.all([
                fetch("/data/turkey-locations.json", { credentials: "same-origin" }),
                fetch("/data/tax-offices.json", { credentials: "same-origin" })
            ]);

            if (locationsResponse.ok) {
                accountLocations = await locationsResponse.json();
            }

            if (taxOfficesResponse.ok) {
                accountTaxOffices = await taxOfficesResponse.json();
            }
        } catch {
            accountLocations = [];
            accountTaxOffices = [];
        }

        syncTaxOfficeCityFromSelectedOffice();

        fillWorkshopCitySelect();
        fillWorkshopTaxOfficeCitySelect();
        fillWorkshopDistrictSelect();
        fillWorkshopTaxOfficeSelect();

        citySelect.addEventListener("change", () => {
            document.getElementById("workshopDistrictInput")?.removeAttribute("data-current-value");
            fillWorkshopDistrictSelect();
        });

        taxOfficeCitySelect?.addEventListener("change", () => {
            document.getElementById("workshopTaxOfficeInput")?.removeAttribute("data-current-value");
            fillWorkshopTaxOfficeSelect();
        });
    };

    const openDrawer = async (drawerId, trigger) => {
        if (activeDrawer && activeDrawer.id !== drawerId && isFormDirty(activeDrawer)) {
            showUnsavedPrompt(activeDrawer);
            return;
        }

        if (activeDrawer && activeDrawer.id !== drawerId) {
            await closeDrawer(true);
        }

        const drawer = document.getElementById(drawerId);
        if (!drawer) return;

        activeDrawer = drawer;
        activeTrigger = trigger;

        shell.hidden = false;
        drawer.hidden = false;
        document.body.classList.add("account-drawer-open");
        trigger?.setAttribute("aria-expanded", "true");

        const forms = getDrawerForms(drawer);
        if (forms.length) {
            forms.forEach(clearError);
            hideUnsavedPrompt(drawer);
            await initializeWorkshopLocationSelects();
            forms.forEach(setFormSnapshot);
        }

        requestAnimationFrame(() => {
            const focusable = getFocusable(drawer);
            const firstInput = drawer.querySelector("input, textarea, button");
            (firstInput || focusable[0] || drawer).focus?.();
        });
    };

    const closeDrawer = async (force = false) => {
        if (!activeDrawer) return;

        if (!force && isFormDirty(activeDrawer)) {
            showUnsavedPrompt(activeDrawer);
            return;
        }

        resetSensitiveFields(activeDrawer);
        const forms = getDrawerForms(activeDrawer);
        if (forms.length) {
            forms.forEach(clearError);
            hideUnsavedPrompt(activeDrawer);
        }

        activeDrawer.hidden = true;
        activeTrigger?.setAttribute("aria-expanded", "false");
        activeTrigger?.focus?.();

        activeDrawer = null;
        activeTrigger = null;
        shell.hidden = true;
        document.body.classList.remove("account-drawer-open");
    };

    document.querySelectorAll("[data-account-drawer-open]").forEach((trigger) => {
        trigger.addEventListener("click", async () => {
            await openDrawer(trigger.dataset.accountDrawerOpen, trigger);
        });
    });

    shell.querySelectorAll("[data-account-drawer-close]").forEach((button) => {
        button.addEventListener("click", () => closeDrawer(false));
    });

    document.addEventListener("keydown", (event) => {
        if (!activeDrawer) return;

        if (event.key === "Escape") {
            event.preventDefault();
            closeDrawer(false);
            return;
        }

        if (event.key !== "Tab") return;

        const focusable = getFocusable(activeDrawer);
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
    });

    document.querySelector("[data-account-remove-email]")?.addEventListener("click", async () => {
        const form = document.querySelector('[data-account-form-type="email"]');
        if (!form) return;

        const confirmed = window.SenteConfirm && typeof window.SenteConfirm.show === "function"
            ? await window.SenteConfirm.show({
                title: "E-posta adresi kaldırılsın mı?",
                message: "E-posta adresiniz olmadığında otomatik şifre yenileme bağlantısı alamazsınız.",
                confirmText: "Kaldır",
                cancelText: "Vazgeç",
                danger: true
            })
            : window.confirm("E-posta adresi kaldırılsın mı?");

        if (!confirmed) return;

        const emailInput = form.querySelector('input[name="Email"]');
        if (emailInput) emailInput.value = "";
        form.dataset.allowEmptyEmail = "true";
        form.requestSubmit();
    });

    document.querySelector("[data-account-remove-phone]")?.addEventListener("click", async () => {
        const form = document.querySelector('[data-account-form-type="phone"]');
        if (!form) return;

        const confirmed = window.SenteConfirm && typeof window.SenteConfirm.show === "function"
            ? await window.SenteConfirm.show({
                title: "Telefon numarası kaldırılsın mı?",
                message: "Telefon numaranız hesabınızdan kaldırılacak.",
                confirmText: "Kaldır",
                cancelText: "Vazgeç",
                danger: true
            })
            : window.confirm("Telefon numarası kaldırılsın mı?");

        if (!confirmed) return;

        const phoneInput = form.querySelector('input[name="PhoneNumber"]');
        if (phoneInput) phoneInput.value = "";
        form.requestSubmit();
    });

    document.querySelectorAll("[data-account-phone-input]").forEach(bindPhoneInput);

    document.querySelectorAll("[data-account-bank-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const card = button.closest(".account-bank-card");
            const form = card?.querySelector(".account-bank-edit-form");
            if (!form) return;

            const willOpen = form.hidden;
            form.hidden = !willOpen;
            card.querySelectorAll("[data-account-bank-toggle]").forEach((toggle) => {
                toggle.setAttribute("aria-expanded", String(willOpen));
            });

            if (willOpen) {
                const firstInput = form.querySelector("input:not([type='hidden']), select");
                firstInput?.focus?.();
            }
        });
    });

    document.querySelectorAll("[data-account-bank-cancel]").forEach((button) => {
        button.addEventListener("click", () => {
            const form = button.closest(".account-bank-edit-form");
            const card = button.closest(".account-bank-card");
            if (!form || !card) return;

            restoreFormSnapshot(form);
            clearError(form);
            form.hidden = true;
            card.querySelectorAll("[data-account-bank-toggle]").forEach((toggle) => {
                toggle.setAttribute("aria-expanded", "false");
            });
        });
    });

    document.querySelector("[data-account-bank-add-toggle]")?.addEventListener("click", () => {
        const form = document.querySelector(".account-bank-add-form");
        if (!form) return;

        form.hidden = !form.hidden;
        if (!form.hidden) {
            const firstInput = form.querySelector("input:not([type='hidden']), select");
            firstInput?.focus?.();
        }
    });

    document.querySelector("[data-account-bank-add-cancel]")?.addEventListener("click", () => {
        const form = document.querySelector(".account-bank-add-form");
        if (!form) return;

        restoreFormSnapshot(form);
        clearError(form);
        form.hidden = true;
    });

    document.querySelectorAll("[data-account-ajax-form]").forEach((form) => {
        form.addEventListener("input", () => {
            clearError(form);
            hideUnsavedPrompt(form.closest(".account-drawer"));
        });

        form.addEventListener("submit", async (event) => {
            event.preventDefault();
            clearError(form);
            hideUnsavedPrompt(form.closest(".account-drawer"));

            if (form.dataset.accountFormType === "email") {
                const emailInput = form.querySelector('input[name="Email"]');
                const allowsRemoval = form.dataset.allowEmptyEmail === "true";

                if (!emailInput?.value.trim() && !allowsRemoval) {
                    showError(form, "Yeni e-posta adresini girin veya kaldırma işlemini kullanın.");
                    emailInput?.focus();
                    return;
                }
            }

            if (form.dataset.accountFormType === "workshop-bank-delete") {
                const confirmed = window.SenteConfirm && typeof window.SenteConfirm.show === "function"
                    ? await window.SenteConfirm.show({
                        title: "Banka hesabı kaldırılsın mı?",
                        message: "Bu IBAN artık servis belgelerinde ve hesap özetlerinde kullanılmayacak.",
                        confirmText: "Kaldır",
                        cancelText: "Vazgeç",
                        danger: true
                    })
                    : window.confirm("Banka hesabı kaldırılsın mı?");

                if (!confirmed) return;
            }

            const submitter = form.querySelector('button[type="submit"]');
            const originalText = submitter?.textContent;
            const saveCloseButton = form.querySelector("[data-account-save-close]");

            if (submitter) {
                submitter.disabled = true;
                submitter.textContent = "Kaydediliyor...";
            }

            try {
                const response = await fetch(form.action, {
                    method: form.method || "POST",
                    body: new FormData(form),
                    headers: {
                        "X-Requested-With": "XMLHttpRequest",
                        "Accept": "application/json"
                    }
                });

                const payload = await response.json().catch(() => null);

                if (!response.ok || !payload?.isSuccess) {
                    showError(form, payload?.errorMessages || payload?.errorMessage || "İşlem tamamlanamadı.");
                    return;
                }

                if (payload.redirectUrl) {
                    window.location.href = payload.redirectUrl;
                    return;
                }

                if (payload.reload) {
                    showToast(payload.message || "Bilgiler kaydedildi.", "success");
                    window.location.reload();
                    return;
                }

                if (form.dataset.accountFormType === "email") {
                    const emailInput = form.querySelector('input[name="Email"]');
                    const currentEmail = document.querySelector("[data-account-current-email]");
                    const summary = document.querySelector("[data-account-email-summary]");
                    const currentPhoneText = document.querySelector("[data-account-current-phone]")?.textContent;
                    const phone = currentPhoneText === "Tanımlı değil" ? "" : currentPhoneText;

                    if (currentEmail) currentEmail.textContent = payload.email || "Tanımlı değil";
                    if (summary) summary.textContent = payload.emailSummary || "E-posta adresi eklenmemiş";
                    if (emailInput) emailInput.value = payload.email || "";
                    updateProfileContact(payload.email, phone);
                }

                if (form.dataset.accountFormType === "phone") {
                    const phoneInput = form.querySelector('input[name="PhoneNumber"]');
                    const currentPhone = document.querySelector("[data-account-current-phone]");
                    const summary = document.querySelector("[data-account-phone-summary]");
                    const currentEmailText = document.querySelector("[data-account-current-email]")?.textContent;
                    const email = currentEmailText === "Tanımlı değil" ? "" : currentEmailText;
                    const removeButton = document.querySelector("[data-account-remove-phone]");

                    if (currentPhone) currentPhone.textContent = payload.phone || "Tanımlı değil";
                    if (summary) summary.textContent = payload.phoneSummary || "Telefon numarası eklenmemiş";
                    if (phoneInput) phoneInput.value = payload.phone || "";
                    if (removeButton) removeButton.hidden = !payload.phone;
                    updateProfileContact(email, payload.phone);
                }

                if (form.dataset.accountFormType === "workshop") {
                    const serviceSummary = document.querySelector("[data-account-service-summary]");
                    if (serviceSummary && payload.serviceSummary) {
                        serviceSummary.textContent = payload.serviceSummary;
                    }

                    const citySelect = document.getElementById("workshopCityInput");
                    const districtSelect = document.getElementById("workshopDistrictInput");
                    const taxOfficeSelect = document.getElementById("workshopTaxOfficeInput");
                    if (citySelect) citySelect.dataset.currentValue = citySelect.value;
                    if (districtSelect) districtSelect.dataset.currentValue = districtSelect.value;
                    if (taxOfficeSelect) taxOfficeSelect.dataset.currentValue = taxOfficeSelect.value;
                }

                setFormSnapshot(form);
                resetSensitiveFields(activeDrawer || form);
                showToast(payload.message || "Bilgiler kaydedildi.", "success");
                await closeDrawer(true);
            } catch {
                showError(form, "İşlem tamamlanamadı. Lütfen tekrar deneyin.");
            } finally {
                delete form.dataset.allowEmptyEmail;

                if (submitter) {
                    submitter.disabled = false;
                    submitter.textContent = originalText;
                }

                if (saveCloseButton) {
                    saveCloseButton.disabled = false;
                    saveCloseButton.textContent = "Kaydet ve Kapat";
                }
            }
        });
    });
})();
