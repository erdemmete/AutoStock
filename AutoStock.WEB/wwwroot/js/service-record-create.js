const DRAFT_KEY = "sente360_create_service_draft_v3";
const REQUEST_ITEMS_DRAFT_KEY = "sente360_request_items_draft_v3";

let customerSearchTimeout;
let vehicleSearchTimeout;
let requestItems = [];
let currentStep = 1;

let isServiceRecordSaving = false;
let isServiceRecordCreated = false;
let createdServiceRecordId = null;
let suppressDraftSave = false;

let turkeyLocations = [];
let taxOffices = [];
let vehicleVariants = [];

const formatter = new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY"
});

async function loadVehicleVariants(modelId) {
    const select = get("VehicleVariantId");
    if (!select) return;

    select.innerHTML = '<option value="">Versiyon seçmek zorunlu değil</option>';
    vehicleVariants = [];
    clearSelectedVariantTechnicalInfo();

    if (!modelId) {
        updateVehicleTechnicalPreview(null);
        return;
    }

    try {
        const response = await fetch(`/ServiceRecords/GetVariants?modelId=${encodeURIComponent(modelId)}`);

        if (!response.ok) {
            select.innerHTML = '<option value="">Versiyon bulunamadı</option>';
            updateVehicleTechnicalPreview(null);
            return;
        }

        const result = await response.json();
        vehicleVariants = Array.isArray(result?.data)
            ? result.data
            : Array.isArray(result)
                ? result
                : [];

        if (!vehicleVariants.length) {
            select.innerHTML = '<option value="">Bu model için versiyon yok</option>';
            updateVehicleTechnicalPreview(null);
            return;
        }

        vehicleVariants.forEach(variant => {
            const option = document.createElement("option");
            option.value = variant.id;
            option.textContent = buildVariantOptionText(variant);
            select.appendChild(option);
        });
    } catch {
        select.innerHTML = '<option value="">Versiyonlar yüklenemedi</option>';
        vehicleVariants = [];
        updateVehicleTechnicalPreview(null);
    }
}

function buildVariantOptionText(variant) {
    if (!variant) return "";

    const details = [
        variant.fuelType,
        variant.transmissionType,
        variant.enginePowerHp ? `${variant.enginePowerHp} hp` : "",
        variant.modelYearFrom && variant.modelYearTo
            ? `${variant.modelYearFrom}-${variant.modelYearTo}`
            : variant.modelYearFrom
                ? `${variant.modelYearFrom}+`
                : ""
    ].filter(Boolean);

    return details.length
        ? `${variant.name} · ${details.join(" / ")}`
        : variant.name;
}

function clearSelectedVariantTechnicalInfo() {
    ["FuelType", "TransmissionType", "BodyType", "EngineCapacityCc", "EnginePowerHp", "EngineCode"]
        .forEach(id => setHidden(id, ""));

    updateVehicleTechnicalPreview(null);
}

function applySelectedVariantTechnicalInfo() {
    const variantId = Number(get("VehicleVariantId")?.value || 0);
    const variant = vehicleVariants.find(x => Number(x.id) === variantId);

    setHidden("FuelType", variant?.fuelType || "");
    setHidden("TransmissionType", variant?.transmissionType || "");
    setHidden("BodyType", variant?.bodyType || "");
    setHidden("EngineCapacityCc", variant?.engineCapacityCc || "");
    setHidden("EnginePowerHp", variant?.enginePowerHp || "");
    setHidden("EngineCode", variant?.engineCode || "");

    updateVehicleTechnicalPreview(variant);
}

function updateVehicleTechnicalPreview(variant) {
    const preview = get("vehicleTechnicalPreview");

    if (!preview) return;

    if (!variant) {
        preview.hidden = true;
        return;
    }

    setPreviewText("vehiclePreviewFuelType", variant.fuelType, "-");
    setPreviewText("vehiclePreviewTransmissionType", variant.transmissionType, "-");
    setPreviewText(
        "vehiclePreviewEngine",
        [
            variant.engineCapacityCc ? `${variant.engineCapacityCc} cc` : "",
            variant.enginePowerHp ? `${variant.enginePowerHp} hp` : "",
            variant.engineCode || ""
        ].filter(Boolean).join(" / "),
        "-"
    );
    setPreviewText("vehiclePreviewBodyType", variant.bodyType, "-");

    preview.hidden = false;
}

function setHidden(id, value) {
    const input = document.getElementById(id);
    if (input) input.value = value || "";
}



function openMissingVehicleHelpModal() {
    const modal = get("missingVehicleHelpModal");

    if (!modal) return;

    modal.hidden = false;
    modal.setAttribute("aria-hidden", "false");

    requestAnimationFrame(() => {
        modal.classList.add("active");
        get("MissingVehicleNote")?.focus();
    });
}

function closeMissingVehicleHelpModal() {
    const modal = get("missingVehicleHelpModal");

    if (!modal) return;

    modal.classList.remove("active");
    modal.setAttribute("aria-hidden", "true");

    setTimeout(() => {
        modal.hidden = true;
    }, 180);
}

function acceptMissingVehicleNote() {
    const note = get("MissingVehicleNote")?.value?.trim();

    closeMissingVehicleHelpModal();

    if (note) {
        showToast("Araç notu saklandı. En yakın modelle kayda devam edebilirsin.", "success");
    }
}


function get(id) {
    return document.getElementById(id);
}

function firstByName(name) {
    return document.querySelector(`[name="${name}"]`);
}

function escapeHtml(value) {
    return (value || "")
        .toString()
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function normalizeTr(text) {
    return (text || "")
        .toLocaleLowerCase("tr-TR")
        .trim();
}

function toTitleCase(text) {
    return (text || "")
        .toLocaleLowerCase("tr-TR")
        .split(" ")
        .filter(x => x !== "")
        .map(word =>
            word.charAt(0).toLocaleUpperCase("tr-TR") + word.slice(1)
        )
        .join(" ");
}

function formatPhone(digits) {
    digits = (digits || "").replace(/\D/g, "").slice(0, 11);

    if (digits.length > 9) {
        return digits.replace(/(\d{4})(\d{3})(\d{2})(\d{0,2})/, "$1 $2 $3 $4").trim();
    }

    if (digits.length > 7) {
        return digits.replace(/(\d{4})(\d{3})(\d{0,2})/, "$1 $2 $3").trim();
    }

    if (digits.length > 4) {
        return digits.replace(/(\d{4})(\d{0,3})/, "$1 $2").trim();
    }

    return digits;
}

function formatMileage(value) {
    const digits = (value || "").replace(/\D/g, "").slice(0, 8);
    return digits.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
}

function normalizePlate(value) {
    return (value || "")
        .toUpperCase()
        .replace(/[^A-Z0-9]/g, "")
        .slice(0, 12);
}

function getFuelLevelText(value = null) {
    const fuelValue = value ?? get("FuelLevel")?.value;

    switch (fuelValue) {
        case "Empty":
            return "Boş";
        case "Quarter":
            return "1/4";
        case "Half":
            return "1/2";
        case "ThreeQuarters":
            return "3/4";
        case "Full":
            return "Dolu";
        default:
            return "";
    }
}

function getSelectedOptionText(selectId) {
    const select = get(selectId);

    if (!select || !select.value) return "";

    const text = select.options[select.selectedIndex]?.text?.trim() || "";

    if (
        normalizeTr(text).includes("seçiniz") ||
        normalizeTr(text).includes("önce")
    ) {
        return "";
    }

    return text;
}

function setPreviewText(id, value, fallback = "Belirtilmedi") {
    const element = get(id);

    if (!element) return;

    const cleanedValue = value?.toString().trim();

    element.innerText = cleanedValue || fallback;
}

function showTemporaryInvalid(input) {
    if (!input) return;

    input.classList.add("input-invalid");

    setTimeout(() => {
        input.classList.remove("input-invalid");
    }, 1500);
}

function showToast(message, type = "info") {
    const toast = get("toastMessage");

    if (!toast) return;

    toast.className = `toast-message ${type}`;
    toast.innerText = message;

    requestAnimationFrame(() => {
        toast.classList.add("show");
    });

    setTimeout(() => {
        toast.classList.remove("show");
    }, 3200);
}

function clearValidationErrors() {
    document.querySelectorAll("[data-valmsg-for]").forEach(x => {
        x.innerText = "";
    });
}

function showValidationErrors(errors) {
    clearValidationErrors();

    if (!errors) return;

    Object.keys(errors).forEach(key => {
        const messageEl = document.querySelector(`[data-valmsg-for="${key}"]`);

        if (messageEl && errors[key]?.length) {
            messageEl.innerText = errors[key][0];
        }
    });
}

function getActiveCustomerContainer() {
    const extraPanel = get("customerExtraPanel");

    if (extraPanel?.classList.contains("active")) {
        if (get("corporateFields")?.classList.contains("active")) {
            return get("corporateFields");
        }

        if (get("soleFields")?.classList.contains("active")) {
            return get("soleFields");
        }

        if (get("individualFields")?.classList.contains("active")) {
            return get("individualFields");
        }
    }

    return get("quickCustomerFields") || document;
}

function getValueFromActiveCustomer(name) {
    const container = getActiveCustomerContainer();
    const input = container?.querySelector(`[name="${name}"]`);

    return input?.value?.trim() || "";
}

function setValueInActiveCustomer(name, value) {
    const container = getActiveCustomerContainer();
    const input = container?.querySelector(`[name="${name}"]`);

    if (input) {
        input.value = value || "";
    }
}

function getUiCustomerType() {
    return Number(get("CustomerType")?.dataset.uiType || get("CustomerType")?.value || 1);
}

function getFinalCustomerTypeValue() {
    const uiType = getUiCustomerType();
    const subType = get("CustomerSubType")?.value || "individual";

    if (uiType === 2) {
        return 3; // Kurumsal
    }

    if (subType === "sole") {
        return 2; // Şahıs firması
    }

    return 1; // Bireysel
}

function toggleCustomerExtra() {
    get("quickCustomerFields")?.classList.add("hidden");
    get("customerExtraPanel")?.classList.add("active");

    setCustomerType(1, document.querySelector('.customer-type-switch button[data-type="1"]'));

    saveCreateFormDraft();
    updateServiceFormPreview();
}

function backToQuickCustomer() {
    get("quickCustomerFields")?.classList.remove("hidden");
    get("customerExtraPanel")?.classList.remove("active");

    saveCreateFormDraft();
    updateServiceFormPreview();
}

function setCustomerType(type, button) {
    const input = get("CustomerType");
    const individualSubType = get("individualSubType");
    const individualFields = get("individualFields");
    const soleFields = get("soleFields");
    const corporateFields = get("corporateFields");

    if (input) {
        input.dataset.uiType = type.toString();
        input.value = type.toString();
    }

    document.querySelectorAll(".customer-type-switch button")
        .forEach(x => x.classList.remove("active"));

    button?.classList.add("active");

    const isIndividualGroup = type === 1;

    individualSubType?.classList.toggle("active", isIndividualGroup);
    corporateFields?.classList.toggle("active", !isIndividualGroup);

    if (isIndividualGroup) {
        const activeSubType = get("CustomerSubType")?.value || "individual";

        setCustomerSubType(
            activeSubType,
            document.querySelector(`.customer-subtype-switch button[data-subtype="${activeSubType}"]`)
        );
    } else {
        individualFields?.classList.remove("active");
        soleFields?.classList.remove("active");
    }

    saveCreateFormDraft();
    updateServiceFormPreview();
}

function setCustomerSubType(subType, button) {
    const input = get("CustomerSubType");
    const individualFields = get("individualFields");
    const soleFields = get("soleFields");

    if (input) {
        input.value = subType;
    }

    document.querySelectorAll(".customer-subtype-switch button")
        .forEach(x => x.classList.remove("active"));

    button?.classList.add("active");

    individualFields?.classList.toggle("active", subType === "individual");
    soleFields?.classList.toggle("active", subType === "sole");

    saveCreateFormDraft();
    updateServiceFormPreview();
}

async function loadLocationData() {
    try {
        const [locationsResponse, taxOfficesResponse] = await Promise.all([
            fetch("/data/turkey-locations.json"),
            fetch("/data/tax-offices.json")
        ]);

        turkeyLocations = await locationsResponse.json();
        taxOffices = await taxOfficesResponse.json();

        [
            "IndividualAddressCity",
            "SoleAddressCity",
            "CorporateAddressCity",
            "SoleTaxOfficeCity",
            "CorporateTaxOfficeCity"
        ].forEach(fillCitySelect);
    } catch {
        console.warn("Lokasyon/vergi dairesi datası yüklenemedi.");
    }
}

function fillCitySelect(selectId) {
    const select = get(selectId);

    if (!select) return;

    const currentValue = select.value;

    select.innerHTML = `<option value="">İl seçiniz</option>`;

    turkeyLocations
        .sort((a, b) => a.name.localeCompare(b.name, "tr"))
        .forEach(city => {
            select.innerHTML += `
                <option value="${city.name}">
                    ${toTitleCase(city.name)}
                </option>
            `;
        });

    if (currentValue) {
        select.value = currentValue;
    }
}

function fillDistrictSelect(citySelectId, districtSelectId) {
    const city = get(citySelectId)?.value;
    const districtSelect = get(districtSelectId);

    if (!districtSelect) return;

    const currentValue = districtSelect.value;

    districtSelect.innerHTML = `<option value="">İlçe seçiniz</option>`;

    const location = turkeyLocations.find(x => x.name === city);

    if (!location) return;

    location.districts
        .sort((a, b) => a.name.localeCompare(b.name, "tr"))
        .forEach(district => {
            districtSelect.innerHTML += `
                <option value="${district.name}">
                    ${toTitleCase(district.name)}
                </option>
            `;
        });

    if (currentValue) {
        districtSelect.value = currentValue;
    }
}

function fillTaxOfficeSelect(citySelectId, taxOfficeSelectId) {
    const city = get(citySelectId)?.value;
    const taxOfficeSelect = get(taxOfficeSelectId);

    if (!taxOfficeSelect) return;

    const currentValue = taxOfficeSelect.value;

    taxOfficeSelect.innerHTML = `<option value="">Vergi dairesi seçiniz</option>`;

    taxOffices
        .filter(x => !city || normalizeTr(x.city) === normalizeTr(city))
        .sort((a, b) => {
            const districtCompare = (a.district || "").localeCompare(b.district || "", "tr");

            if (districtCompare !== 0) return districtCompare;

            return (a.name || "").localeCompare(b.name || "", "tr");
        })
        .forEach(item => {
            taxOfficeSelect.innerHTML += `
                <option value="${item.name}">
                    ${item.name} - ${item.district} / ${item.city}
                </option>
            `;
        });

    if (currentValue) {
        taxOfficeSelect.value = currentValue;
    }
}

async function loadModelsByBrand(brandId) {
    const modelSelect = get("modelSelect");

    if (!modelSelect) return;

    modelSelect.innerHTML = '<option value="">Model yükleniyor...</option>';
    await loadVehicleVariants("");

    if (!brandId) {
        modelSelect.innerHTML = '<option value="">Önce marka seçiniz</option>';
        return;
    }

    try {
        const response = await fetch(`/ServiceRecords/GetModels?brandId=${encodeURIComponent(brandId)}`);

        if (!response.ok) {
            modelSelect.innerHTML = '<option value="">Model bulunamadı</option>';
            return;
        }

        const models = await response.json();

        modelSelect.innerHTML = '<option value="">Model seçiniz</option>';

        models.forEach(model => {
            const option = document.createElement("option");
            option.value = model.id;
            option.textContent = model.name;
            modelSelect.appendChild(option);
        });
    } catch {
        modelSelect.innerHTML = '<option value="">Model yüklenemedi</option>';
    }
}

function saveCreateFormDraft() {
    if (isServiceRecordCreated || suppressDraftSave) return;

    const form = get("serviceCreateForm");

    if (!form) return;

    const data = {};

    form.querySelectorAll("input, textarea, select").forEach(el => {
        if (!el.id && !el.name) return;

        const key = el.id || el.name;

        if (el.type === "checkbox") {
            data[key] = el.checked;
        } else {
            data[key] = el.value;
        }
    });

    data.__version = 3;
    data.__currentStep = currentStep;
    data.__uiCustomerType = getUiCustomerType();
    data.__customerSubType = get("CustomerSubType")?.value || "individual";
    data.__isExtraPanelOpen = get("customerExtraPanel")?.classList.contains("active") || false;

    localStorage.setItem(DRAFT_KEY, JSON.stringify(data));
    localStorage.setItem(REQUEST_ITEMS_DRAFT_KEY, JSON.stringify(requestItems));
}

async function restoreCreateFormDraft() {
    const raw = localStorage.getItem(DRAFT_KEY);

    if (!raw) return;

    let data;

    try {
        data = JSON.parse(raw);
    } catch {
        return;
    }

    if (data.__version !== 3) {
        localStorage.removeItem(DRAFT_KEY);
        localStorage.removeItem(REQUEST_ITEMS_DRAFT_KEY);
        return;
    }

    if (data.__isExtraPanelOpen) {
        get("quickCustomerFields")?.classList.add("hidden");
        get("customerExtraPanel")?.classList.add("active");
    }

    Object.keys(data).forEach(key => {
        if (key.startsWith("__")) return;

        const el = get(key);

        if (!el) return;

        if (el.type === "checkbox") {
            el.checked = data[key];
        } else {
            el.value = data[key];
        }
    });

    setCustomerType(
        Number(data.__uiCustomerType || 1),
        document.querySelector(`.customer-type-switch button[data-type="${data.__uiCustomerType || 1}"]`)
    );

    setCustomerSubType(
        data.__customerSubType || "individual",
        document.querySelector(`.customer-subtype-switch button[data-subtype="${data.__customerSubType || "individual"}"]`)
    );

    restoreDependentSelects(data);

    await restoreVehicleModelSelection(data);

    updateSelectedCustomerCard();
    updateSelectedVehicleCard();

    const targetStep = Number(data.__currentStep || 1);
    goToStep(targetStep);
}

function restoreDependentSelects(data) {
    const districtPairs = [
        ["IndividualAddressCity", "IndividualAddressDistrict"],
        ["SoleAddressCity", "SoleAddressDistrict"],
        ["CorporateAddressCity", "CorporateAddressDistrict"]
    ];

    districtPairs.forEach(([citySelectId, districtSelectId]) => {
        if (!get(citySelectId)?.value) return;

        fillDistrictSelect(citySelectId, districtSelectId);

        if (data[districtSelectId] && get(districtSelectId)) {
            get(districtSelectId).value = data[districtSelectId];
        }
    });

    const taxOfficePairs = [
        ["SoleTaxOfficeCity", "SoleTaxOfficeSelect"],
        ["CorporateTaxOfficeCity", "CorporateTaxOfficeSelect"]
    ];

    taxOfficePairs.forEach(([citySelectId, taxOfficeSelectId]) => {
        if (!get(citySelectId)?.value) return;

        fillTaxOfficeSelect(citySelectId, taxOfficeSelectId);

        if (data[taxOfficeSelectId] && get(taxOfficeSelectId)) {
            get(taxOfficeSelectId).value = data[taxOfficeSelectId];
        }
    });
}

async function restoreVehicleModelSelection(data) {
    const brandSelect = get("brandSelect");
    const modelSelect = get("modelSelect");

    if (!brandSelect || !modelSelect) return;

    const brandValue = data.brandSelect || data.VehicleBrandId;
    const modelValue = data.modelSelect || data.VehicleModelId;

    if (!brandValue || !modelValue) return;

    brandSelect.value = brandValue;

    await loadModelsByBrand(brandValue);

    modelSelect.value = modelValue;

    await loadVehicleVariants(modelValue);

    const variantValue = data.VehicleVariantId;

    if (variantValue && get("VehicleVariantId")) {
        get("VehicleVariantId").value = variantValue;
        applySelectedVariantTechnicalInfo();
    }
}

function restoreRequestItemsDraft() {
    const raw = localStorage.getItem(REQUEST_ITEMS_DRAFT_KEY);

    if (!raw) return;

    try {
        const restoredItems = JSON.parse(raw);

        if (Array.isArray(restoredItems)) {
            requestItems = restoredItems;
        }
    } catch {
        requestItems = [];
    }

    renderRequestItems();
}

function clearDrafts() {
    localStorage.removeItem(DRAFT_KEY);
    localStorage.removeItem(REQUEST_ITEMS_DRAFT_KEY);
}

function resetCreateFormDraft() {
    if (isServiceRecordSaving) {
        showToast("Kayıt işlemi devam ederken form sıfırlanamaz.", "error");
        return;
    }

    const hasDraft =
        localStorage.getItem(DRAFT_KEY) ||
        localStorage.getItem(REQUEST_ITEMS_DRAFT_KEY) ||
        document.querySelector("#serviceCreateForm input:not([type='hidden'])")?.value ||
        requestItems.length > 0;

    if (hasDraft) {
        const confirmed = confirm(
            "Bu ekrandaki tüm girilen bilgiler ve kaydedilmemiş talepler silinecek. Yeni servis kaydı başlatmak istiyor musunuz?"
        );

        if (!confirmed) return;
    }

    suppressDraftSave = true;

    clearDrafts();

    requestItems = [];
    currentStep = 1;
    isServiceRecordSaving = false;
    isServiceRecordCreated = false;
    createdServiceRecordId = null;

    window.location.href = `${window.location.pathname}?fresh=${Date.now()}`;
}

function initializeDraftAutoSave() {
    const form = get("serviceCreateForm");

    if (!form) return;

    form.addEventListener("input", () => {
        saveCreateFormDraft();
        updateServiceFormPreview();
    });

    form.addEventListener("change", () => {
        saveCreateFormDraft();
        updateServiceFormPreview();
    });
}

function addRequestItem(titleFromDraft = null, noteFromDraft = null, estimatedFromDraft = null) {
    const titleInput = get("requestInput");
    const noteInput = get("requestNoteInput");
    const estimatedInput = get("requestEstimatedInput");

    if (!titleInput || !noteInput || !estimatedInput) return;

    const title = (titleFromDraft ?? titleInput.value).trim();
    const note = (noteFromDraft ?? noteInput.value).trim();

    const estimatedRaw = estimatedFromDraft ?? estimatedInput.value.replace(/\D/g, "");
    const estimatedAmount = Number(estimatedRaw || 0);

    if (!title) {
        showTemporaryInvalid(titleInput);
        titleInput.focus();
        return;
    }

    requestItems.push({
        title,
        note,
        estimatedAmount
    });

    titleInput.value = "";
    noteInput.value = "";
    estimatedInput.value = "";

    renderRequestItems();
    updateRequestSummary();
    saveCreateFormDraft();
}

function removeRequestItem(index) {
    requestItems.splice(index, 1);

    renderRequestItems();
    updateRequestSummary();
    saveCreateFormDraft();
}

function renderRequestItems() {
    const container = get("requestItemsContainer");

    if (!container) return;

    container.innerHTML = "";

    requestItems.forEach((item, index) => {
        const element = document.createElement("div");
        element.className = "request-item";

        element.dataset.title = item.title || "";
        element.dataset.note = item.note || "";
        element.dataset.estimatedAmount = item.estimatedAmount || 0;

        const noteHtml = item.note
            ? `<span>${escapeHtml(item.note)}</span>`
            : "";

        const amountHtml = Number(item.estimatedAmount || 0) > 0
            ? `<small>Tahmini: ${formatter.format(Number(item.estimatedAmount || 0))}</small>`
            : "";

        element.innerHTML = `
            <div>
                <strong>${escapeHtml(item.title)}</strong>
                ${noteHtml}
                ${amountHtml}

                <input type="hidden"
                       name="RequestItems[${index}].Title"
                       value="${escapeHtml(item.title)}" />

                <input type="hidden"
                       name="RequestItems[${index}].Note"
                       value="${escapeHtml(item.note || "")}" />

                <input type="hidden"
                       name="RequestItems[${index}].EstimatedAmount"
                       value="${Number(item.estimatedAmount || 0)}" />
            </div>

            <button type="button"
                    class="request-remove"
                    onclick="removeRequestItem(${index})">
                ✕
            </button>
        `;

        container.appendChild(element);
    });
}

function updateRequestSummary() {
    const requestCountText = get("requestCountText");
    const estimatedTotalText = get("estimatedTotalText");
    const estimatedTotalBox = get("estimatedTotalBox");

    const total = requestItems.reduce((sum, item) => {
        return sum + Number(item.estimatedAmount || 0);
    }, 0);

    if (requestCountText) requestCountText.innerText = requestItems.length;
    if (estimatedTotalText) estimatedTotalText.innerText = formatter.format(total);
    if (estimatedTotalBox) estimatedTotalBox.innerText = formatter.format(total);

    updateServiceFormPreview();
}

function getRequestItemsForPreview() {
    return requestItems.map(x => ({
        title: x.title || "",
        note: x.note || "",
        estimatedAmount: Number(x.estimatedAmount || 0)
    }));
}

function updateServiceFormPreview() {
    const today = new Date().toLocaleDateString("tr-TR");

    const brand = getSelectedOptionText("brandSelect");
    const model = getSelectedOptionText("modelSelect");

    const brandModel = [brand, model]
        .filter(x => x && x.trim())
        .join(" / ");

    setPreviewText("previewDateText", today, "-");

    setPreviewText("previewCustomerName", getValueFromActiveCustomer("CustomerName"));
    setPreviewText("previewCustomerPhone", getValueFromActiveCustomer("CustomerPhoneNumber"));
    setPreviewText("previewDeliveredBy", getValueFromActiveCustomer("VehicleDeliveredBy"));

    setPreviewText("previewPlate", get("Plate")?.value?.trim()?.toUpperCase(), "---");
    setPreviewText("previewBrandModel", brandModel, "---");
    setPreviewText("previewMileage", get("Mileage")?.value?.trim(), "---");
    setPreviewText("previewFuelLevel", getFuelLevelText(), "---");
    setPreviewText("previewChassis", get("ChassisNumber")?.value?.trim()?.toUpperCase(), "---");

    const table = get("previewRequestTable");
    const tableHead = get("previewRequestTableHead");
    const tableBody = get("previewRequestTableBody");

    if (!table || !tableHead || !tableBody) return;

    const previewItems = getRequestItemsForPreview();
    const hasAnyPrice = previewItems.some(item => Number(item.estimatedAmount || 0) > 0);

    table.classList.toggle("has-price", hasAnyPrice);

    const colGroup = table.querySelector("colgroup");

    if (colGroup) {
        colGroup.innerHTML = hasAnyPrice
            ? `
                <col style="width:34px;" />
                <col />
                <col class="compact-price-col" />
            `
            : `
                <col style="width:34px;" />
                <col />
            `;
    }

    tableHead.innerHTML = hasAnyPrice
        ? `
            <tr>
                <th>No</th>
                <th>İşlem / Talep</th>
                <th class="compact-request-price">Tahmini</th>
            </tr>
        `
        : `
            <tr>
                <th>No</th>
                <th>İşlem / Talep</th>
            </tr>
        `;

    if (!previewItems.length) {
        tableBody.innerHTML = `
            <tr class="compact-empty-row">
                <td colspan="${hasAnyPrice ? 3 : 2}">
                    İşlem belirtilmedi.
                </td>
            </tr>
        `;

        return;
    }

    let total = 0;

    const rows = previewItems.map((item, index) => {
        const amount = Number(item.estimatedAmount || 0);
        total += amount;

        const noteHtml = item.note
            ? `<span class="compact-request-note">${escapeHtml(item.note)}</span>`
            : "";

        const priceCell = hasAnyPrice
            ? `<td class="compact-request-price">${amount > 0 ? formatter.format(amount) : ""}</td>`
            : "";

        return `
            <tr>
                <td class="compact-request-no">${index + 1}</td>

                <td>
                    <span class="compact-request-title">${escapeHtml(item.title)}</span>
                    ${noteHtml}
                </td>

                ${priceCell}
            </tr>
        `;
    }).join("");

    const totalRow = hasAnyPrice
        ? `
            <tr class="compact-total-row">
                <td colspan="2" class="compact-request-price">Tahmini Toplam</td>
                <td class="compact-request-price">${formatter.format(total)}</td>
            </tr>
        `
        : "";

    tableBody.innerHTML = rows + totalRow;
}

function goToStep(step) {
    currentStep = step;

    if (step === 4) {
        updateServiceFormPreview();
    }

    const layout = get("createLayout");

    if (layout) {
        layout.classList.toggle("preview-mode", step === 4);
    }

    document.querySelectorAll(".service-step").forEach(section => {
        section.classList.toggle("active", Number(section.dataset.step) === step);
    });

    document.querySelectorAll(".step-tab").forEach(tab => {
        const tabStep = Number(tab.dataset.step);

        tab.classList.toggle("active", tabStep === step);
        tab.classList.toggle("completed", tabStep < step);
    });

    window.scrollTo({
        top: 0,
        behavior: "smooth"
    });

    saveCreateFormDraft();
}

function initializeInputMasks() {
    document.querySelectorAll(`[name="CustomerPhoneNumber"]`).forEach(phoneInput => {
        phoneInput.addEventListener("beforeinput", function (e) {
            if (e.inputType === "insertText" && !/^\d$/.test(e.data)) {
                e.preventDefault();
                showTemporaryInvalid(phoneInput);
            }
        });

        phoneInput.addEventListener("paste", function (e) {
            const pastedText = (e.clipboardData || window.clipboardData).getData("text");
            const digits = pastedText.replace(/\D/g, "").slice(0, 11);

            e.preventDefault();
            this.value = formatPhone(digits);
            saveCreateFormDraft();
            updateServiceFormPreview();
        });

        phoneInput.addEventListener("input", function () {
            this.value = formatPhone(this.value);
        });
    });

    document.querySelectorAll(".js-email-lower").forEach(input => {
        input.addEventListener("input", function () {
            this.value = this.value.toLocaleLowerCase("tr-TR");
        });
    });

    document.querySelectorAll(".js-title-case").forEach(input => {
        input.addEventListener("blur", function () {
            this.value = toTitleCase(this.value);
            saveCreateFormDraft();
            updateServiceFormPreview();
        });
    });

    const chassisNumberInput = get("ChassisNumber");

    chassisNumberInput?.addEventListener("input", function () {
        this.value = this.value
            .toUpperCase()
            .replace(/[^A-Z0-9]/g, "")
            .slice(0, 17);
    });

    const plateInput = get("Plate");

    plateInput?.addEventListener("beforeinput", function (e) {
        if (e.inputType === "insertText" && !/^[a-zA-Z0-9]$/.test(e.data)) {
            e.preventDefault();
            showTemporaryInvalid(this);
        }
    });

    plateInput?.addEventListener("input", function () {
        this.value = normalizePlate(this.value);
    });

    const modelYearInput = get("ModelYear");

    modelYearInput?.addEventListener("beforeinput", function (e) {
        if (e.inputType === "insertText" && !/^\d$/.test(e.data)) {
            e.preventDefault();
            showTemporaryInvalid(this);
        }
    });

    modelYearInput?.addEventListener("input", function () {
        this.value = this.value.replace(/\D/g, "").slice(0, 4);
    });

    const mileageInput = get("Mileage");

    mileageInput?.addEventListener("beforeinput", function (e) {
        if (e.inputType === "insertText" && !/^\d$/.test(e.data)) {
            e.preventDefault();
            showTemporaryInvalid(this);
        }
    });

    mileageInput?.addEventListener("input", function () {
        this.value = formatMileage(this.value);
    });

    const requestEstimatedInput = get("requestEstimatedInput");

    requestEstimatedInput?.addEventListener("input", function () {
        const digits = this.value.replace(/\D/g, "");

        if (!digits) {
            this.value = "";
            return;
        }

        this.value = Number(digits).toLocaleString("tr-TR");
    });
}

function getCustomerField(customer, keys) {
    for (const key of keys) {
        if (customer && customer[key] !== undefined && customer[key] !== null && customer[key] !== "") {
            return customer[key];
        }
    }

    return "";
}

function getCustomerFinalType(customer) {
    const rawType = getCustomerField(customer, [
        "type",
        "customerType",
        "customerTypeValue",
        "typeValue"
    ]);

    if (typeof rawType === "number") {
        return rawType;
    }

    const text = normalizeTr(rawType?.toString());

    if (text.includes("corporate") || text.includes("kurumsal")) {
        return 3;
    }

    if (text.includes("sole") || text.includes("şahıs") || text.includes("sahis")) {
        return 2;
    }

    return 1;
}

function setInputValue(id, value) {
    const input = get(id);

    if (!input) return;

    input.value = value || "";
}

function normalizeSelectLookup(value) {
    return (value || "")
        .toString()
        .trim()
        .toLocaleLowerCase("tr-TR")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replaceAll("ı", "i")
        .replaceAll("İ", "i")
        .replace(/\s+/g, "");
}

function setSelectValue(id, value) {
    const select = get(id);

    if (!select) return;

    const cleanValue = value?.toString().trim() || "";

    if (!cleanValue) {
        select.value = "";
        return;
    }

    select.value = cleanValue;

    if (select.value === cleanValue) {
        return;
    }

    const wanted = normalizeSelectLookup(cleanValue);

    const matchedOption = Array.from(select.options).find(option => {
        const optionValue = normalizeSelectLookup(option.value);
        const optionText = normalizeSelectLookup(option.textContent);

        return optionValue === wanted || optionText === wanted;
    });

    if (matchedOption) {
        select.value = matchedOption.value;
        return;
    }

    const fallbackOption = document.createElement("option");
    fallbackOption.value = cleanValue;
    fallbackOption.textContent = cleanValue;
    fallbackOption.selected = true;

    select.appendChild(fallbackOption);
}

function inferTaxOfficeCity(taxOfficeName) {
    if (!taxOfficeName) return "";

    const office = taxOffices.find(x =>
        normalizeTr(x.name) === normalizeTr(taxOfficeName)
    );

    return office?.city || "";
}

function fillAddressFields(cityId, districtId, city, district) {
    setSelectValue(cityId, city);

    if (get(cityId)?.value) {
        fillDistrictSelect(cityId, districtId);
    }

    setSelectValue(districtId, district);
}

function fillTaxOfficeFields(cityId, taxOfficeId, taxOfficeCity, taxOffice) {
    const finalCity = taxOfficeCity || inferTaxOfficeCity(taxOffice);

    setSelectValue(cityId, finalCity);

    if (get(cityId)?.value) {
        fillTaxOfficeSelect(cityId, taxOfficeId);
    }

    setSelectValue(taxOfficeId, taxOffice);
}

function applyCustomerDataToCreateForm(customer) {
    const finalType = getCustomerFinalType(customer);

    const customerName = getCustomerField(customer, [
        "customerName",
        "name",
        "fullName",
        "title",
        "displayName"
    ]);

    const companyName = getCustomerField(customer, [
        "companyName",
        "company",
        "businessName"
    ]);

    const authorizedPersonName = getCustomerField(customer, [
        "authorizedPersonName",
        "authorizedName",
        "representativeName"
    ]);

    const phone = getCustomerField(customer, [
        "phoneNumber",
        "customerPhone",
        "phone",
        "gsm"
    ]);

    const email = getCustomerField(customer, [
        "email",
        "customerEmail",
        "mail"
    ]);

    const nationalIdentityNumber = getCustomerField(customer, [
        "nationalIdentityNumber",
        "tckn",
        "tcIdentityNumber"
    ]);

    const taxOfficeCity = getCustomerField(customer, [
        "taxOfficeCity"
    ]);

    const taxOffice = getCustomerField(customer, [
        "taxOffice",
        "taxOfficeName"
    ]);

    const taxNumber = getCustomerField(customer, [
        "taxNumber",
        "vkn",
        "taxNo"
    ]);

    const addressCity = getCustomerField(customer, [
        "addressCity",
        "city"
    ]);

    const addressDistrict = getCustomerField(customer, [
        "addressDistrict",
        "district"
    ]);

    const customerAddress = getCustomerField(customer, [
        "customerAddress",
        "address",
        "addressText",
        "fullAddress"
    ]);

    const normalizedName = toTitleCase(customerName);
    const normalizedPhone = formatPhone(phone);
    const normalizedEmail = (email || "").toLocaleLowerCase("tr-TR");

    setInputValue("MainCustomerName", normalizedName);
    setInputValue("MainCustomerPhoneNumber", normalizedPhone);
    setInputValue("MainCustomerEmail", normalizedEmail);

    if (finalType === 1) {
        setCustomerType(1, document.querySelector('.customer-type-switch button[data-type="1"]'));
        setCustomerSubType("individual", document.querySelector('.customer-subtype-switch button[data-subtype="individual"]'));

        setInputValue("IndividualCustomerName", normalizedName);
        setInputValue("IndividualCustomerPhoneNumber", normalizedPhone);
        setInputValue("IndividualCustomerEmail", normalizedEmail);
        setInputValue("IndividualNationalIdentityNumber", nationalIdentityNumber);

        fillAddressFields(
            "IndividualAddressCity",
            "IndividualAddressDistrict",
            addressCity,
            addressDistrict
        );

        setInputValue("IndividualCustomerAddress", customerAddress);
    }

    if (finalType === 2) {
        setCustomerType(1, document.querySelector('.customer-type-switch button[data-type="1"]'));
        setCustomerSubType("sole", document.querySelector('.customer-subtype-switch button[data-subtype="sole"]'));

        setInputValue("SoleCustomerName", normalizedName);
        setInputValue("SoleCompanyName", toTitleCase(companyName));
        setInputValue("SoleNationalIdentityNumber", nationalIdentityNumber);

        fillTaxOfficeFields(
            "SoleTaxOfficeCity",
            "SoleTaxOfficeSelect",
            taxOfficeCity,
            taxOffice
        );

        setInputValue("SoleTaxNumber", taxNumber);

        fillAddressFields(
            "SoleAddressCity",
            "SoleAddressDistrict",
            addressCity,
            addressDistrict
        );

        setInputValue("SoleCustomerAddress", customerAddress);

        setInputValue("SoleCustomerPhoneNumber", normalizedPhone);
        setInputValue("SoleCustomerEmail", normalizedEmail);
    }

    if (finalType === 3) {
        setCustomerType(2, document.querySelector('.customer-type-switch button[data-type="2"]'));

        setInputValue("CorporateCustomerName", normalizedName);
        setInputValue("CorporateCompanyName", toTitleCase(companyName));
        setInputValue("CorporateAuthorizedPersonName", toTitleCase(authorizedPersonName));

        fillTaxOfficeFields(
            "CorporateTaxOfficeCity",
            "CorporateTaxOfficeSelect",
            taxOfficeCity,
            taxOffice
        );

        setInputValue("CorporateTaxNumber", taxNumber);

        fillAddressFields(
            "CorporateAddressCity",
            "CorporateAddressDistrict",
            addressCity,
            addressDistrict
        );

        setInputValue("CorporateCustomerAddress", customerAddress);

        setInputValue("CorporateCustomerPhoneNumber", normalizedPhone);
        setInputValue("CorporateCustomerEmail", normalizedEmail);
    }

    updateServiceFormPreview();
}

function initializeCustomerSearch() {
    const searchInput = get("MainCustomerName");
    const searchResults = get("customerSearchResults");

    if (!searchInput || !searchResults) return;

    searchInput.addEventListener("input", function () {
        clearTimeout(customerSearchTimeout);

        get("SelectedCustomerId").value = "";

        const query = this.value.trim();

        if (query.length < 2) {
            searchResults.style.display = "none";
            searchResults.innerHTML = "";
            return;
        }

        customerSearchTimeout = setTimeout(async () => {
            const customerUrl = `/ServiceRecords/SearchCustomers?query=${encodeURIComponent(query)}`;
            const vehicleUrl = `/ServiceRecords/SearchVehicles?plate=${encodeURIComponent(normalizePlate(query))}`;

            const [customerResult, vehicleResult] = await Promise.allSettled([
                fetch(customerUrl),
                fetch(vehicleUrl)
            ]);

            let customers = [];
            let vehicles = [];

            if (customerResult.status === "fulfilled" && customerResult.value.ok) {
                customers = await customerResult.value.json();
            }

            if (vehicleResult.status === "fulfilled" && vehicleResult.value.ok) {
                vehicles = await vehicleResult.value.json();
            }

            renderUnifiedSearchResults(customers, vehicles, searchResults);
        }, 280);
    });

   
}


document.addEventListener("click", function (e) {
    const customerResults = get("customerSearchResults");
    const customerSearchInput = get("MainCustomerName");

    if (
        customerResults &&
        customerSearchInput &&
        !customerResults.contains(e.target) &&
        e.target !== customerSearchInput
    ) {
        customerResults.style.display = "none";
    }
});

document.addEventListener("keydown", function (e) {
    const customerSearchInput = get("MainCustomerName");

    if (
        customerSearchInput &&
        e.ctrlKey &&
        e.key.toLocaleLowerCase("tr-TR") === "k"
    ) {
        e.preventDefault();
        customerSearchInput.focus();
    }
});



function renderUnifiedSearchResults(customers, vehicles, container) {
    container.innerHTML = "";

    const hasCustomers = Array.isArray(customers) && customers.length > 0;
    const hasVehicles = Array.isArray(vehicles) && vehicles.length > 0;

    if (!hasCustomers && !hasVehicles) {
        container.innerHTML = `
            <div class="customer-search-empty">
                Kayıtlı müşteri veya araç bulunamadı. Bilgileri manuel girerek devam edebilirsin.
            </div>
        `;

        container.style.display = "block";
        return;
    }

    if (hasCustomers) {
        container.appendChild(createSearchGroupTitle("MÜŞTERİLER / CARİLER"));

        customers.forEach(customer => {
            container.appendChild(createCustomerSearchItem(customer));
        });
    }

    if (hasVehicles) {
        container.appendChild(createSearchGroupTitle("ARAÇLAR / PLAKALAR"));

        vehicles.forEach(vehicle => {
            container.appendChild(createVehicleSearchItem(vehicle));
        });
    }

    container.style.display = "block";
}

function createSearchGroupTitle(text) {
    const title = document.createElement("div");
    title.className = "search-group-title";
    title.innerText = text;
    return title;
}

function createCustomerSearchItem(customer) {
    const item = document.createElement("div");
    item.className = "customer-search-item is-customer";

    const customerId = customer.id ?? customer.customerId ?? "";
    const customerName =
        customer.name ??
        customer.customerName ??
        customer.fullName ??
        customer.companyName ??
        "";

    const phone = customer.phoneNumber ?? customer.phone ?? "";
    const email = customer.email ?? "";

    item.innerHTML = `
        <strong>${escapeHtml(customerName || "İsimsiz müşteri")}</strong>
        <span>${escapeHtml([phone, email].filter(Boolean).join(" • ") || "-")}</span>
    `;

    item.addEventListener("click", () => {
        get("SelectedCustomerId").value = customerId;

        applyCustomerDataToCreateForm(customer);

        get("customerSearchResults").style.display = "none";

        saveCreateFormDraft();
        updateServiceFormPreview();

        showToast("Kayıtlı müşteri bilgileri forma aktarıldı.", "success");
    });

    return item;
}

function createVehicleSearchItem(vehicle) {
    const item = document.createElement("div");
    item.className = "customer-search-item is-vehicle";

    const plate = vehicle.plate ?? "";
    const brand = vehicle.brandName ?? vehicle.brand ?? "";
    const model = vehicle.modelName ?? vehicle.model ?? "";
    const customerName = vehicle.customerName ?? vehicle.ownerName ?? "";

    item.innerHTML = `
        <strong>${escapeHtml([plate, brand, model].filter(Boolean).join(" • "))}</strong>
        <span>Kayıtlı müşteri/cari: ${escapeHtml(customerName || "Belirtilmedi")}</span>
    `;

    item.addEventListener("click", async () => {
        await selectVehicleFromUnifiedSearch(vehicle);

        get("customerSearchResults").style.display = "none";

        saveCreateFormDraft();
        updateServiceFormPreview();

        showToast("Araç bilgileri forma aktarıldı.", "success");
    });

    return item;
}

async function selectVehicleFromUnifiedSearch(vehicle) {
    await selectVehicleFromSearch(vehicle);

    const customerId = vehicle.customerId ?? "";

    if (customerId) {
        get("SelectedCustomerId").value = customerId;

        const customerFromVehicle = {
            id: vehicle.customerId,
            type: vehicle.customerType,

            name: vehicle.customerName,
            customerName: vehicle.customerName,

            phoneNumber: vehicle.customerPhone,
            email: vehicle.customerEmail,

            companyName: vehicle.companyName,
            authorizedPersonName: vehicle.authorizedPersonName,

            nationalIdentityNumber: vehicle.nationalIdentityNumber,

            taxOffice: vehicle.taxOffice,
            taxNumber: vehicle.taxNumber,

            addressCity: vehicle.addressCity,
            addressDistrict: vehicle.addressDistrict,
            customerAddress: vehicle.customerAddress
        };

        applyCustomerDataToCreateForm(customerFromVehicle);
    }

    goToStep(2);
}

function updateSelectedCustomerCard() {
    const card = get("selectedCustomerCard");
    const idInput = get("SelectedCustomerId");

    if (!card || !idInput) return;

    const hasSelectedCustomer = !!idInput.value;

    card.classList.toggle("active", hasSelectedCustomer);

    if (!hasSelectedCustomer) return;

    const name = getValueFromActiveCustomer("CustomerName");
    const phone = getValueFromActiveCustomer("CustomerPhoneNumber");
    const email = getValueFromActiveCustomer("CustomerEmail");

    setPreviewText("selectedCustomerNameText", name, "Seçili müşteri");
    setPreviewText("selectedCustomerInfoText", [phone, email].filter(Boolean).join(" • "), "-");
}

function clearSelectedCustomer() {
    get("SelectedCustomerId").value = "";

    updateSelectedCustomerCard();
    saveCreateFormDraft();
}

function initializeVehicleSearch() {
    const plateInput = get("Plate");
    const vehicleResults = get("vehicleSearchResults");

    if (!plateInput || !vehicleResults) return;

    plateInput.addEventListener("input", function () {
        clearTimeout(vehicleSearchTimeout);

        get("SelectedVehicleId").value = "";
        get("VehicleOwnershipConfirmed").value = "false";
        get("VehicleOwnershipAction").value = "";

        updateSelectedVehicleCard();

        const plate = normalizePlate(this.value);

        if (plate.length < 2) {
            vehicleResults.style.display = "none";
            vehicleResults.innerHTML = "";
            return;
        }

        vehicleSearchTimeout = setTimeout(async () => {
            const response = await fetch(`/ServiceRecords/SearchVehicles?plate=${encodeURIComponent(plate)}`);

            if (!response.ok) {
                vehicleResults.style.display = "none";
                return;
            }

            const vehicles = await response.json();

            vehicleResults.innerHTML = "";

            if (!vehicles.length) {
                vehicleResults.style.display = "none";
                return;
            }

            vehicles.forEach(vehicle => {
                const item = document.createElement("div");
                item.className = "vehicle-search-item";

                const vehiclePlate = vehicle.plate ?? "";
                const brand = vehicle.brandName ?? vehicle.brand ?? "";
                const model = vehicle.modelName ?? vehicle.model ?? "";
                const customerName = vehicle.customerName ?? vehicle.ownerName ?? "";

                item.innerHTML = `
                    <strong>${escapeHtml(vehiclePlate)} ${escapeHtml([brand, model].filter(Boolean).join(" / "))}</strong>
                    <span>Kayıtlı müşteri: ${escapeHtml(customerName || "Belirtilmedi")}</span>
                `;

                item.addEventListener("click", async () => {
                    await selectVehicleFromSearch(vehicle);
                    vehicleResults.style.display = "none";
                });

                vehicleResults.appendChild(item);
            });

            vehicleResults.style.display = "block";
        }, 300);
    });

    document.addEventListener("click", function (e) {
        if (!vehicleResults.contains(e.target) && e.target !== plateInput) {
            vehicleResults.style.display = "none";
        }
    });
}

async function selectVehicleFromSearch(vehicle) {
    const vehicleId = vehicle.id ?? vehicle.vehicleId ?? "";
    const customerId = vehicle.customerId ?? "";
    const plate = vehicle.plate ?? "";
    const brandId = vehicle.brandId ?? vehicle.vehicleBrandId ?? "";
    const modelId = vehicle.modelId ?? vehicle.vehicleModelId ?? "";

    get("SelectedVehicleId").value = vehicleId;
    get("Plate").value = normalizePlate(plate);

    if (brandId && get("brandSelect")) {
        get("brandSelect").value = brandId;
        await loadModelsByBrand(brandId);
    }

    if (modelId && get("modelSelect")) {
        get("modelSelect").value = modelId;
        await loadVehicleVariants(modelId);

        const variantId = vehicle.variantId ?? vehicle.vehicleVariantId ?? "";

        if (variantId && get("VehicleVariantId")) {
            get("VehicleVariantId").value = variantId;
            applySelectedVariantTechnicalInfo();
        }
    }

    if (vehicle.modelYear && get("ModelYear")) {
        get("ModelYear").value = vehicle.modelYear;
    }

    if (vehicle.mileage && get("Mileage")) {
        get("Mileage").value = formatMileage(vehicle.mileage.toString());
    }

    if (vehicle.chassisNumber && get("ChassisNumber")) {
        get("ChassisNumber").value = vehicle.chassisNumber.toUpperCase();
    }

    if (customerId && !get("SelectedCustomerId").value) {
        get("SelectedCustomerId").value = customerId;
    }

    updateSelectedVehicleCard();
    openOwnershipModal(vehicle);

    saveCreateFormDraft();
    updateServiceFormPreview();
}

function updateSelectedVehicleCard() {
    const card = get("selectedVehicleCard");
    const idInput = get("SelectedVehicleId");

    if (!card || !idInput) return;

    const hasSelectedVehicle = !!idInput.value;

    card.classList.toggle("active", hasSelectedVehicle);

    if (!hasSelectedVehicle) return;

    const plate = get("Plate")?.value || "";
    const brand = getSelectedOptionText("brandSelect");
    const model = getSelectedOptionText("modelSelect");

    setPreviewText("selectedVehiclePlateText", plate, "Seçili araç");
    setPreviewText("selectedVehicleInfoText", [brand, model].filter(Boolean).join(" / "), "-");
}

function clearSelectedVehicle() {
    get("SelectedVehicleId").value = "";
    get("VehicleOwnershipConfirmed").value = "false";
    get("VehicleOwnershipAction").value = "";

    updateSelectedVehicleCard();
    saveCreateFormDraft();
}

function openOwnershipModal(vehicle) {
    const modal = get("ownershipModalBackdrop");

    if (!modal) return;

    const plate = vehicle.plate ?? get("Plate")?.value ?? "";
    const brand = vehicle.brandName ?? vehicle.brand ?? "";
    const model = vehicle.modelName ?? vehicle.model ?? "";
    const customerName = vehicle.customerName ?? vehicle.ownerName ?? "";
    const customerPhone = vehicle.customerPhone ?? vehicle.phoneNumber ?? "";

    setPreviewText(
        "ownershipModalVehicleText",
        [plate, brand, model].filter(Boolean).join(" • "),
        "Araç"
    );

    setPreviewText(
        "ownershipModalCustomerText",
        customerName
            ? `Kayıtlı müşteri/cari: ${customerName}${customerPhone ? " • " + customerPhone : ""}`
            : "Kayıtlı müşteri/cari bilgisi bulunamadı.",
        "Kayıtlı müşteri/cari bilgisi bulunamadı."
    );

    modal.dataset.customerId = vehicle.customerId ?? "";
    modal.dataset.customerName = customerName || "";
    modal.dataset.customerPhone = customerPhone || "";
    modal.dataset.customerEmail = vehicle.customerEmail ?? "";

    modal.classList.add("active");
}

function closeOwnershipModal() {
    get("ownershipModalBackdrop")?.classList.remove("active");
}

function continueWithRegisteredVehicleCustomer() {
    const modal = get("ownershipModalBackdrop");

    if (!modal) return;

    const customerId = modal.dataset.customerId || "";
    const customerName = modal.dataset.customerName || "";
    const customerPhone = modal.dataset.customerPhone || "";
    const customerEmail = modal.dataset.customerEmail || "";

    if (customerId) {
        get("SelectedCustomerId").value = customerId;
    }

    if (customerName) {
        get("MainCustomerName").value = toTitleCase(customerName);
    }

    if (customerPhone) {
        get("MainCustomerPhoneNumber").value = formatPhone(customerPhone);
    }

    if (customerEmail) {
        get("MainCustomerEmail").value = customerEmail.toLocaleLowerCase("tr-TR");
    }

    get("VehicleOwnershipConfirmed").value = "true";
    get("VehicleOwnershipAction").value = "RegisteredCustomer";

    updateSelectedCustomerCard();
    closeOwnershipModal();

    saveCreateFormDraft();
    updateServiceFormPreview();
}

function chooseAnotherCustomerForVehicle() {
    get("VehicleOwnershipConfirmed").value = "true";
    get("VehicleOwnershipAction").value = "AnotherCustomer";

    closeOwnershipModal();
    goToStep(1);

    get("MainCustomerName")?.focus();

    showToast("Bu servis kaydı için farklı müşteri seçebilirsin.", "info");
}

function createNewCustomerForVehicle() {
    get("SelectedCustomerId").value = "";
    get("VehicleOwnershipConfirmed").value = "true";
    get("VehicleOwnershipAction").value = "NewCustomer";

    clearSelectedCustomer();
    closeOwnershipModal();
    goToStep(1);

    get("MainCustomerName")?.focus();

    showToast("Yeni müşteri bilgilerini girerek devam edebilirsin.", "info");
}

function initializeVehicleBrandModel() {
    const brandSelect = get("brandSelect");
    const modelSelect = get("modelSelect");
    const variantSelect = get("VehicleVariantId");

    brandSelect?.addEventListener("change", async function () {
        await loadModelsByBrand(this.value);

        saveCreateFormDraft();
        updateServiceFormPreview();
    });

    modelSelect?.addEventListener("change", async function () {
        await loadVehicleVariants(this.value);

        saveCreateFormDraft();
        updateServiceFormPreview();
    });

    variantSelect?.addEventListener("change", function () {
        applySelectedVariantTechnicalInfo();

        saveCreateFormDraft();
        updateServiceFormPreview();
    });
}

function initializeLocationEvents() {
    const districtPairs = [
        ["IndividualAddressCity", "IndividualAddressDistrict"],
        ["SoleAddressCity", "SoleAddressDistrict"],
        ["CorporateAddressCity", "CorporateAddressDistrict"]
    ];

    districtPairs.forEach(([citySelectId, districtSelectId]) => {
        get(citySelectId)?.addEventListener("change", function () {
            fillDistrictSelect(citySelectId, districtSelectId);
            saveCreateFormDraft();
        });

        get(districtSelectId)?.addEventListener("change", function () {
            saveCreateFormDraft();
        });
    });

    const taxOfficePairs = [
        ["SoleTaxOfficeCity", "SoleTaxOfficeSelect"],
        ["CorporateTaxOfficeCity", "CorporateTaxOfficeSelect"]
    ];

    taxOfficePairs.forEach(([citySelectId, taxOfficeSelectId]) => {
        get(citySelectId)?.addEventListener("change", function () {
            fillTaxOfficeSelect(citySelectId, taxOfficeSelectId);
            saveCreateFormDraft();
        });

        get(taxOfficeSelectId)?.addEventListener("change", function () {
            saveCreateFormDraft();
        });
    });
}

function prepareFormDataForSubmit(form) {
    document.querySelectorAll(".js-title-case").forEach(input => {
        input.value = toTitleCase(input.value);
    });

    document.querySelectorAll(".js-email-lower").forEach(input => {
        input.value = input.value.toLocaleLowerCase("tr-TR");
    });

    renderRequestItems();

    const formData = new FormData(form);

    const activeCustomerName = getValueFromActiveCustomer("CustomerName");
    const activePhone = getValueFromActiveCustomer("CustomerPhoneNumber");
    const activeEmail = getValueFromActiveCustomer("CustomerEmail");
    const activeVehicleDeliveredBy = getValueFromActiveCustomer("VehicleDeliveredBy");

    formData.set("CustomerName", activeCustomerName);
    formData.set("CustomerPhoneNumber", activePhone);
    formData.set("CustomerEmail", activeEmail || "");
    formData.set("VehicleDeliveredBy", activeVehicleDeliveredBy || "");
    formData.set("CompanyName", getValueFromActiveCustomer("CompanyName") || "");
    formData.set("AuthorizedPersonName", getValueFromActiveCustomer("AuthorizedPersonName") || "");
    formData.set("NationalIdentityNumber", getValueFromActiveCustomer("NationalIdentityNumber") || "");
    formData.set("TaxOffice", getValueFromActiveCustomer("TaxOffice") || "");
    formData.set("TaxNumber", getValueFromActiveCustomer("TaxNumber") || "");
    formData.set("AddressCity", getValueFromActiveCustomer("AddressCity") || "");
    formData.set("AddressDistrict", getValueFromActiveCustomer("AddressDistrict") || "");
    formData.set("CustomerAddress", getValueFromActiveCustomer("CustomerAddress") || "");

    const mileageInput = get("Mileage");

    if (mileageInput) {
        formData.set("Mileage", mileageInput.value.replace(/\D/g, ""));
    }

    formData.set("CustomerType", getFinalCustomerTypeValue().toString());
    formData.set("FuelLevel", get("FuelLevel")?.value || "");

    return formData;
}

function validateBeforeSubmit(formData) {
    const customerName = formData.get("CustomerName")?.toString().trim();
    const customerPhone = formData.get("CustomerPhoneNumber")?.toString().trim();
    const plate = formData.get("Plate")?.toString().trim();

    if (!customerName) {
        showToast("Müşteri / cari adı zorunludur.", "error");
        goToStep(1);
        showTemporaryInvalid(getActiveCustomerContainer()?.querySelector(`[name="CustomerName"]`));
        return false;
    }

    if (!customerPhone) {
        showToast("İletişim telefonu zorunludur.", "error");
        goToStep(1);
        showTemporaryInvalid(getActiveCustomerContainer()?.querySelector(`[name="CustomerPhoneNumber"]`));
        return false;
    }

    if (!plate) {
        showToast("Plaka zorunludur.", "error");
        goToStep(2);
        showTemporaryInvalid(get("Plate"));
        return false;
    }

    if (requestItems.length <= 0) {
        showToast("En az bir şikayet / talep eklemelisin.", "error");
        goToStep(3);
        showTemporaryInvalid(get("requestInput"));
        return false;
    }

    const finalCustomerType = Number(formData.get("CustomerType"));

    if (
        (finalCustomerType === 2 || finalCustomerType === 3) &&
        !formData.get("VehicleDeliveredBy")?.toString().trim()
    ) {
        showToast("Şahıs/kurumsal müşterilerde aracı getiren / ilgili kişi zorunludur.", "error");
        goToStep(1);
        showTemporaryInvalid(getActiveCustomerContainer()?.querySelector(`[name="VehicleDeliveredBy"]`));
        return false;
    }

    return true;
}

async function saveServiceRecord(action = "detail", clickedButton = null) {
    saveCreateFormDraft();

    if (isServiceRecordSaving || isServiceRecordCreated) {
        return;
    }

    const form = get("serviceCreateForm");

    if (!form) return;

    const formData = prepareFormDataForSubmit(form);

    if (!validateBeforeSubmit(formData)) {
        return;
    }

    isServiceRecordSaving = true;

    const saveButton = clickedButton || document.querySelector(".js-create-action");
    const originalText = saveButton?.innerText;

    if (saveButton) {
        saveButton.disabled = true;
        saveButton.innerText =
            action === "print"
                ? "Kaydediliyor ve PDF hazırlanıyor..."
                : action === "photos"
                    ? "Kaydediliyor..."
                    : "Kaydediliyor...";
    }

    try {
        const response = await fetch(form.action, {
            method: "POST",
            body: formData,
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        const result = await response.json();

        if (!response.ok || result.success === false) {
            if (result.validationErrors) {
                showValidationErrors(result.validationErrors);
            }

            showToast(
                result.message ||
                result.errorMessage ||
                "Servis kaydı oluşturulamadı.",
                "error"
            );

            return;
        }

        clearValidationErrors();

        createdServiceRecordId =
            result.serviceRecordId ||
            result.ServiceRecordId ||
            result.data?.serviceRecordId ||
            result.data?.ServiceRecordId;

        isServiceRecordCreated = true;

        clearDrafts();
        lockCreatedForm();

        showCreatedResult(action);

        showToast(result.message || "Servis kaydı başarıyla oluşturuldu.", "success");

        if (action === "photos" && createdServiceRecordId) {
            window.location.href = `/ServiceRecords/Detail/${createdServiceRecordId}?openPhotos=1`;
            return;
        }

        if (action === "print" && createdServiceRecordId) {
            window.open(`/service-pdf/create/${createdServiceRecordId}`, "_blank");
        }
    }
    catch {
        showToast("Beklenmeyen bir hata oluştu.", "error");
    }
    finally {
        isServiceRecordSaving = false;

        if (!isServiceRecordCreated && saveButton) {
            saveButton.disabled = false;
            saveButton.innerText = originalText || "Kaydı Oluştur";
        }
    }
}

function lockCreatedForm() {
    document.body.classList.add("form-created-lock");

    const form = get("serviceCreateForm");

    if (!form) return;

    form.querySelectorAll("input, select, textarea").forEach(element => {
        element.setAttribute("readonly", "readonly");

        if (element.tagName === "SELECT") {
            element.setAttribute("disabled", "disabled");
        }
    });

    document.querySelectorAll(".js-create-action").forEach(button => {
        button.disabled = true;

        const actionCard = button.closest(
            ".quick-action-card, .preview-action-card, .summary-action-card, .action-card, .service-action-card"
        );

        if (actionCard) {
            actionCard.style.display = "none";
        } else {
            button.style.display = "none";
        }
    });
}

function showCreatedResult(action) {
    const resultBox = get("createdResult");
    const resultText = get("createdResultText");
    const detailLink = get("goDetailLink");
    const pdfButton = get("openCreatedPdfButton");

    if (!resultBox || !createdServiceRecordId) return;

    resultBox.classList.add("active");

    const detailUrl = `/ServiceRecords/Detail/${createdServiceRecordId}`;
    const photoUrl = `${detailUrl}#photos`;
    const pdfUrl = `/service-pdf/create/${createdServiceRecordId}`;

    if (detailLink) {
        detailLink.href = action === "photos" ? photoUrl : detailUrl;
        detailLink.innerText = action === "photos"
            ? "Fotoğraf Eklemeye Git"
            : "Servis Kaydına Git";
    }

    if (resultText) {
        resultText.innerText = action === "photos"
            ? "Fotoğraf eklemek için servis kaydı detayına geçebilirsin."
            : "Servis kaydı başarıyla oluşturuldu. Artık bu ekran pasif.";
    }

    if (pdfButton) {
        pdfButton.onclick = () => {
            window.open(pdfUrl, "_blank");
        };
    }
}

function initializeKeyboardShortcuts() {
    document.addEventListener("keydown", function (e) {
        if (e.ctrlKey && e.key === "Enter") {
            e.preventDefault();
            saveServiceRecord("detail", document.querySelector(".js-create-action"));
        }
    });
}

document.addEventListener("DOMContentLoaded", async function () {
    suppressDraftSave = true;

    const urlParams = new URLSearchParams(window.location.search);

    if (urlParams.has("fresh")) {
        clearDrafts();

        const cleanUrl = window.location.pathname;
        window.history.replaceState({}, document.title, cleanUrl);
    }

    const customerTypeInput = get("CustomerType");

    if (customerTypeInput && !customerTypeInput.value) {
        customerTypeInput.value = "1";
        customerTypeInput.dataset.uiType = "1";
    }

    setCustomerType(
        1,
        document.querySelector(`.customer-type-switch button[data-type="1"]`)
    );

    await loadLocationData();

    initializeDraftAutoSave();
    initializeInputMasks();
    initializeCustomerSearch();
   
    initializeVehicleBrandModel();
    initializeLocationEvents();
    initializeKeyboardShortcuts();

    await restoreCreateFormDraft();
    restoreRequestItemsDraft();

    suppressDraftSave = false;

    renderRequestItems();
    updateRequestSummary();
    updateServiceFormPreview();
    saveCreateFormDraft();
});