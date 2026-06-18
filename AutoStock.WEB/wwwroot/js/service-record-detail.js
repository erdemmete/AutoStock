const config = window.serviceRecordDetailConfig || {};
    const serviceRecordId = Number(config.serviceRecordId || 0);

    function escapeHtml(value) {
    if (value === null || value === undefined) return "";

    return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
    }

    function toNumber(value) {
    return window.SenteMoney
    ? SenteMoney.parse(value)
    : Number(String(value || "").replace(",", ".")) || 0;
    }

    function formatMoney(value) {
    return window.SenteMoney
    ? SenteMoney.format(value)
    : toNumber(value).toLocaleString("tr-TR") + " ₺";
    }

    function showToast(message, type = "info") {
    const toast = document.getElementById("toastMessage");

    if (!toast) return;

    toast.className = `srx-toast ${type}`;
    toast.textContent = message;

    requestAnimationFrame(() => {
    toast.classList.add("show");
    });

    setTimeout(() => {
    toast.classList.remove("show");
    }, 3200);
    }

    function showFriendlyError(error, fallbackMessage) {
    const message = error?.isUserSafe
    ? error.message
    : fallbackMessage;

    if (!error?.isUserSafe) {
    console.error(error);
    }

    showToast(message || "İşlem tamamlanamadı. Lütfen tekrar deneyin.", "error");
    }

    function setButtonLoading(button, isLoading, loadingText = "İşleniyor...") {
    if (!button) return;

    if (isLoading) {
    button.dataset.originalText = button.textContent;
    button.disabled = true;
    button.textContent = loadingText;
    return;
    }

    button.disabled = false;
    button.textContent = button.dataset.originalText || button.textContent;
    }

    async function fetchJsonOrThrow(url, formData, fallbackMessage) {
    const response = await fetch(url, {
    method: "POST",
    body: formData,
    credentials: "same-origin"
    });

    let payload = null;

    try {
    payload = await response.json();
    }
    catch {
    payload = null;
    }

    if (!response.ok) {
    const message =
    payload?.errorMessage ||
    payload?.ErrorMessage ||
    payload?.message ||
    payload?.Message ||
    fallbackMessage;

    const error = new Error(message);
    error.isUserSafe = true;
    throw error;
    }

    if (payload && payload.isSuccess === false) {
    const error = new Error(payload.errorMessage || fallbackMessage);
    error.isUserSafe = true;
    throw error;
    }

    return payload;
    }

    function updateSummaryTotal(recordTotal) {
    const subTotal = toNumber(recordTotal);
    const vat = subTotal * 0.20;
    const grandTotal = subTotal + vat;

    const totalElement = document.querySelector(".summary-total");
    const vatElement = document.querySelector(".summary-vat");
    const grandTotalElement = document.querySelector(".summary-grand-total");

    if (totalElement) {
    totalElement.dataset.total = subTotal;
    totalElement.textContent = formatMoney(subTotal);
    }

    if (vatElement) {
    vatElement.dataset.vat = vat;
    vatElement.textContent = formatMoney(vat);
    }

    if (grandTotalElement) {
    grandTotalElement.dataset.grandTotal = grandTotal;
    grandTotalElement.textContent = formatMoney(grandTotal);
    }
    }

    function updateSummaryFromPayload(summary) {
    if (!summary) return;

    const totalElement = document.querySelector(".summary-total");
    const vatElement = document.querySelector(".summary-vat");
    const grandTotalElement = document.querySelector(".summary-grand-total");

    if (totalElement) {
    totalElement.dataset.total = summary.subTotal ?? 0;
    totalElement.textContent = summary.subTotalText || formatMoney(summary.subTotal);
    }

    if (vatElement) {
    vatElement.dataset.vat = summary.vatTotal ?? 0;
    vatElement.textContent = summary.vatTotalText || formatMoney(summary.vatTotal);
    }

    if (grandTotalElement) {
    grandTotalElement.dataset.grandTotal = summary.grandTotal ?? 0;
    grandTotalElement.textContent = summary.grandTotalText || formatMoney(summary.grandTotal);
    }
    }

    function formatQuantity(value) {
    return toNumber(value).toLocaleString("tr-TR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
    });
    }

    function renderOperationCard(operation, serviceRecordId) {
    const id = operation?.id;

    if (!id) return "";

    const type = Number(operation.type || 1);
    const typeClass = operation.typeClass || (type === 1 ? "part" : "labor");
    const typeText = operation.typeText || (type === 1 ? "Parça" : "İşçilik");
    const requestId = operation.serviceRequestItemId || "";
    const unitPriceRaw = window.SenteMoney
    ? SenteMoney.toRaw(operation.unitPrice)
    : toNumber(operation.unitPrice).toFixed(2);

    return `
    <article class="srx-operation-card request-operation-item"
    id="operation-item-${id}"
    data-operation-id="${id}">

    <div class="srx-operation-display">

    <div class="srx-operation-main">

    <span class="srx-type-badge ${escapeHtml(typeClass)}">
    ${escapeHtml(typeText)}
    </span>

    <div class="srx-operation-copy">
    <strong>${escapeHtml(operation.description)}</strong>
    ${operation.note
    ? `<small>${escapeHtml(operation.note)}</small>`
    : `<small class="muted">Not yok</small>`}
    </div>

    </div>

    <div class="srx-operation-price">
    <strong>${formatMoney(operation.totalPrice)}</strong>
    <span>${formatQuantity(operation.quantity)} × ${formatMoney(operation.unitPrice)}</span>
    </div>

    <div class="srx-operation-actions">

    <button type="button"
    class="srx-secondary-mini-btn"
    data-action="edit-operation">
    Düzenle
    </button>

    <button type="button"
    class="srx-secondary-mini-btn danger"
    data-action="delete-operation"
    data-operation-id="${id}">
    Sil
    </button>

    </div>

    </div>

    <form action="/ServiceRecords/UpdateOperation"
    method="post"
    class="srx-operation-edit-form ajax-operation-edit-form"
    data-operation-id="${id}"
    hidden>

    <input type="hidden"
    name="OperationId"
    value="${id}" />

    <input type="hidden"
    name="ServiceRecordId"
    value="${escapeHtml(serviceRecordId)}" />

    <input type="hidden"
    name="ServiceRequestItemId"
    value="${escapeHtml(requestId)}" />

    <input type="hidden"
    name="StockItemId"
    class="srx-stock-item-id" />

    <div class="srx-operation-editor">

    <div class="srx-type-switch">

    <input type="radio"
    id="editPart_${id}"
    name="Type"
    value="1"
    ${type === 1 ? "checked" : ""} />

    <label for="editPart_${id}" title="Parça">
    P
    </label>

    <input type="radio"
    id="editLabor_${id}"
    name="Type"
    value="2"
    ${type === 2 ? "checked" : ""} />

    <label for="editLabor_${id}" title="İşçilik">
    İ
    </label>

    </div>

    <div class="srx-search-box">

    <input type="text"
    name="Description"
    class="srx-operation-search-input"
    value="${escapeHtml(operation.description)}"
    autocomplete="off"
    placeholder="Parça adı, kodu veya işlem açıklaması..." />

    <div class="srx-search-results"></div>

    </div>

    <input type="number"
    name="Quantity"
    class="srx-qty-input"
    value="${escapeHtml(operation.quantity)}"
    min="1"
    step="1"
    placeholder="Miktar" />

    <div class="srx-money-input">

    <input type="text"
    name="UnitPrice"
    class="srx-price-input"
    value="${escapeHtml(unitPriceRaw)}"
    inputmode="decimal"
    data-sente-money-input
    autocomplete="off"
    placeholder="Fiyat" />

    </div>

    </div>

    <input type="text"
    name="Note"
    class="srx-operation-note"
    value="${escapeHtml(operation.note || "")}"
    placeholder="İşlem notu opsiyonel" />

    <div class="srx-form-actions">
    <button type="submit"
    class="srx-primary-mini-btn">
    İşlemi Kaydet
    </button>

    <button type="button"
    class="srx-secondary-mini-btn"
    data-action="cancel-edit-operation">
    Vazgeç
    </button>
    </div>

    </form>

    </article>
    `;
    }

    function updateRequestTotals(card, request) {
    if (!card || !request) return;

    card.querySelectorAll(".srx-request-operation-count, .srx-request-detail-count")
    .forEach(element => {
    if (request.operationCount !== null && request.operationCount !== undefined) {
    element.textContent = request.operationCount;
    }
    });

    card.querySelectorAll(".srx-request-operation-total, .srx-request-detail-total")
    .forEach(element => {
    element.textContent = request.operationTotalText || formatMoney(request.operationTotal);
    });
    }

    function renderEmptyOperationState(operationList) {
    if (!operationList || operationList.querySelector(".srx-operation-card")) return;

    operationList.innerHTML = `
    <div class="srx-operation-empty">
    <strong>Henüz işlem eklenmedi.</strong>
    <span>Bu şikayetin altına parça veya işçilik ekleyin.</span>
    </div>
    `;
    }

    function replaceOperationInRequest(payload, form) {
    const operation = payload?.operation;
    const operationCard = form.closest(".srx-operation-card");
    const card = form.closest(".srx-request-card");

    if (!operation || !operationCard || !card) return;

    operationCard.outerHTML = renderOperationCard(operation, serviceRecordId);

    const newOperationCard = card.querySelector(`#operation-item-${operation.id}`);

    if (newOperationCard) {
    window.SenteMoney?.bind(newOperationCard);
    }

    updateRequestTotals(card, payload?.request);
    setRequestExpanded(card, true);
    }

    function removeOperationFromRequest(payload, operationCard) {
    const card = operationCard?.closest(".srx-request-card");
    const operationList = operationCard?.closest("[data-operation-list]");

    operationCard?.remove();

    renderEmptyOperationState(operationList);
    updateRequestTotals(card, payload?.request);
    setRequestExpanded(card, true);
    }

    function appendOperationToRequest(payload, form) {
    const operation = payload?.operation;
    const card = form.closest(".srx-request-card");
    const operationList = card?.querySelector("[data-operation-list]");

    if (!operation || !card || !operationList) return;

    operationList.querySelector(".srx-operation-empty")?.remove();
    operationList.insertAdjacentHTML(
    "beforeend",
    renderOperationCard(operation, serviceRecordId)
    );

    const operationCard = operationList.querySelector(`#operation-item-${operation.id}`);

    if (operationCard) {
    window.SenteMoney?.bind(operationCard);
    }

    updateRequestTotals(card, payload?.request);
    setRequestExpanded(card, true);
    form.reset();
    form.hidden = true;
    form.querySelector(".srx-search-results")?.replaceChildren();
    form.querySelector(".srx-stock-item-id")?.setAttribute("value", "");

    const stockInput = form.querySelector(".srx-stock-item-id");
    if (stockInput) {
    stockInput.value = "";
    }

    const trigger = card.querySelector('[data-action="show-operation-form"]');
    if (trigger) {
    trigger.hidden = false;
    }
    }

    function updateStatusBadge(status) {
    const badge = document.querySelector(".srx-status-badge");

    if (!badge) return;

    const statusMap = {
    "1": { text: "Aktif", className: "active" },
    "2": { text: "Aktif", className: "active" },
    "3": { text: "Tamamlandı", className: "completed" },
    "4": { text: "İptal", className: "cancelled" }
    };

    const next = statusMap[String(status)] || {
    text: "Bilinmiyor",
    className: "unknown"
    };

    badge.className = `srx-status-badge ${next.className}`;
    badge.textContent = next.text;
    }

    function renderStatusButtons(status) {
    status = String(status);

    if (status === "1" || status === "2") {
    return `
    <button type="button"
    class="srx-secondary-action success status-btn"
    data-status="3">
    Servisi Tamamla
    </button>
    `;
    }

    if (status === "3" || status === "4") {
    return `
    <button type="button"
    class="srx-secondary-action status-btn wide"
    data-status="1">
    Tekrar Aktif Yap
    </button>
    `;
    }

    return "";
    }

    function refreshStatusButtons(status) {
    const grid = document.querySelector(".srx-action-grid");

    if (!grid) return;

    grid.querySelectorAll(".status-btn").forEach(button => button.remove());

    grid.insertAdjacentHTML("beforeend", renderStatusButtons(status));
    }

    function setRequestExpanded(card, expanded) {
    if (!card) return;

    card.dataset.expanded = expanded ? "true" : "false";

    const textButton = card.querySelector('[data-role="toggle-request-text"]');

    if (textButton) {
    textButton.textContent = expanded ? "İşlemleri Kapat" : "İşlemleri Aç";
    }
    }

    function openRequestEditForm(card) {
    if (!card) return;

    setRequestExpanded(card, true);

    const form = card.querySelector(".srx-request-edit-form");

    if (!form) return;

    form.hidden = false;

    const input = form.querySelector('input[name="Title"]');

    setTimeout(() => input?.focus(), 50);
    }

    function closeRequestEditForm(card) {
    if (!card) return;

    const form = card.querySelector(".srx-request-edit-form");

    if (!form) return;

    form.reset();
    form.hidden = true;
    }

    function openOperationEditForm(operationCard) {
    if (!operationCard) return;

    const display = operationCard.querySelector(".srx-operation-display");
    const form = operationCard.querySelector(".srx-operation-edit-form");

    if (display) {
    display.hidden = true;
    }

    if (form) {
    form.hidden = false;
    bindOperationAutocomplete(form);

    const input = form.querySelector('input[name="Description"]');

    setTimeout(() => input?.focus(), 50);
    }
    }

    function closeOperationEditForm(operationCard) {
    if (!operationCard) return;

    const display = operationCard.querySelector(".srx-operation-display");
    const form = operationCard.querySelector(".srx-operation-edit-form");

    if (form) {
    form.reset();
    form.hidden = true;
    }

    if (display) {
    display.hidden = false;
    }
    }

    function getOperationType(form) {
    return form.querySelector('input[name="Type"]:checked')?.value || "1";
    }

    function bindOperationAutocomplete(form) {
    if (!form || form.dataset.searchBound === "true") return;

    form.dataset.searchBound = "true";

    const input = form.querySelector(".srx-operation-search-input");
    const stockInput = form.querySelector(".srx-stock-item-id");
    const results = form.querySelector(".srx-search-results");
    const priceInput = form.querySelector('input[name="UnitPrice"]');
    const typeRadios = form.querySelectorAll('input[name="Type"]');

    if (!input || !stockInput || !results) return;

    let timer = null;

    function closeResults() {
    results.innerHTML = "";
    results.classList.remove("active");
    }

    function clearStockSelection() {
    stockInput.value = "";
    }

    typeRadios.forEach(radio => {
    radio.addEventListener("change", () => {
    clearStockSelection();
    closeResults();
    });
    });

    input.addEventListener("input", () => {
    clearTimeout(timer);
    clearStockSelection();

    const query = input.value.trim();
    const type = getOperationType(form);

    if (type !== "1" || query.length < 2) {
    closeResults();
    return;
    }

    timer = setTimeout(async () => {
    try {
    const response = await fetch(
    `/ServiceRecords/SearchStockItems?q=${encodeURIComponent(query)}`,
    {
    credentials: "same-origin"
    }
    );

    if (!response.ok) {
    closeResults();
    return;
    }

    const payload = await response.json();
    const items = Array.isArray(payload)
    ? payload
    : (payload.data || []);

    results.innerHTML = "";

    if (!items.length) {
    results.innerHTML = `
    <div class="srx-search-item">
    <div>
    <strong>Sonuç bulunamadı</strong>
    <small>Bu parçayı serbest açıklama olarak ekleyebilirsiniz.</small>
    </div>
    </div>
    `;

    results.classList.add("active");
    return;
    }

    items.forEach(item => {
    const id = item.id ?? item.Id;
    const name = item.name ?? item.Name ?? "";
    const code = item.code ?? item.Code ?? "";
    const quantity = item.quantity ?? item.Quantity ?? 0;

    const salePrice =
    item.salePrice ??
    item.SalePrice ??
    item.unitPrice ??
    item.UnitPrice ??
    null;

    const element = document.createElement("div");
    element.className = "srx-search-item";

    element.innerHTML = `
    <div>
    <strong>${escapeHtml(name)}</strong>
    <small>
    ${code ? `Kod: ${escapeHtml(code)} • ` : ""}
    Stok: ${toNumber(quantity).toLocaleString("tr-TR")}
    </small>
    </div>

    <span>
    ${salePrice !== null ? formatMoney(salePrice) : ""}
    </span>
    `;

    element.addEventListener("click", () => {
    input.value = name;
    stockInput.value = id;

    if (salePrice !== null && priceInput && !priceInput.value) {
    priceInput.value = toNumber(salePrice);
    window.SenteMoney?.formatInput(priceInput);
    }

    closeResults();
    input.focus();
    });

    results.appendChild(element);
    });

    results.classList.add("active");
    }
    catch {
    closeResults();
    }
    }, 250);
    });

    input.addEventListener("keydown", e => {
    if (e.key === "Escape") {
    closeResults();
    }
    });

    document.addEventListener("click", e => {
    if (!form.contains(e.target)) {
    closeResults();
    }
    });
    }

    function bindOperationForms() {
    document.querySelectorAll(".ajax-operation-form").forEach(form => {
    bindOperationAutocomplete(form);

    if (form.dataset.bound === "true") return;

    form.dataset.bound = "true";

    form.addEventListener("submit", async e => {
    e.preventDefault();

    if (form.dataset.submitting === "true") return;

    const descriptionInput = form.querySelector('input[name="Description"]');
    const priceInput = form.querySelector('input[name="UnitPrice"]');

    if (!descriptionInput?.value.trim()) {
    showToast("Açıklama zorunludur.", "error");
    descriptionInput?.focus();
    return;
    }

    if (!priceInput?.value || toNumber(priceInput.value) < 0) {
    showToast("Birim fiyat geçerli olmalıdır.", "error");
    priceInput?.focus();
    return;
    }

    form.dataset.submitting = "true";

    const submitButton = form.querySelector('button[type="submit"]');

    setButtonLoading(submitButton, true, "Ekleniyor...");

    try {
    priceInput.value = window.SenteMoney
    ? SenteMoney.toRaw(priceInput.value)
    : toNumber(priceInput.value).toFixed(2);

    const formData = new FormData(form);

    const payload = await fetchJsonOrThrow(
    form.action,
    formData,
    "İşlem eklenirken hata oluştu."
    );

    appendOperationToRequest(payload, form);
    updateSummaryFromPayload(payload?.summary);

    showToast("İşlem başarıyla eklendi.", "success");
    }
    catch (error) {
    showFriendlyError(error, "İşlem eklenemedi. Bilgileri kontrol edip tekrar deneyin.");
    }
    finally {
    form.dataset.submitting = "false";
    setButtonLoading(submitButton, false);
    }
    });
    });
    }

    function bindInlineEditForms() {
    if (document.body.dataset.inlineEditFormsBound === "true") return;

    document.body.dataset.inlineEditFormsBound = "true";

    document.addEventListener("submit", async e => {
    const form = e.target;

    if (form.matches(".ajax-request-edit-form")) {
    e.preventDefault();

    if (form.dataset.submitting === "true") return;

    const titleInput = form.querySelector('input[name="Title"]');

    if (!titleInput?.value.trim()) {
    showToast("Şikayet başlığı zorunludur.", "error");
    titleInput?.focus();
    return;
    }

    form.dataset.submitting = "true";

    const submitButton = form.querySelector('button[type="submit"]');

    setButtonLoading(submitButton, true, "Kaydediliyor...");

    try {
    const formData = new FormData(form);

    await fetchJsonOrThrow(
    form.action,
    formData,
    "Şikayet güncellenirken hata oluştu."
    );

    showToast("Şikayet güncellendi.", "success");

    setTimeout(() => {
    window.location.reload();
    }, 450);
    }
    catch (error) {
    showFriendlyError(error, "İşlem güncellenemedi. Bilgileri kontrol edip tekrar deneyin.");
    }
    finally {
    form.dataset.submitting = "false";
    setButtonLoading(submitButton, false);
    }

    return;
    }

    if (form.matches(".ajax-operation-edit-form")) {
    e.preventDefault();

    if (form.dataset.submitting === "true") return;

    const descriptionInput = form.querySelector('input[name="Description"]');
    const priceInput = form.querySelector('input[name="UnitPrice"]');

    if (!descriptionInput?.value.trim()) {
    showToast("İşlem açıklaması zorunludur.", "error");
    descriptionInput?.focus();
    return;
    }

    if (!priceInput?.value || toNumber(priceInput.value) < 0) {
    showToast("Birim fiyat geçerli olmalıdır.", "error");
    priceInput?.focus();
    return;
    }

    form.dataset.submitting = "true";

    const submitButton = form.querySelector('button[type="submit"]');

    setButtonLoading(submitButton, true, "Kaydediliyor...");

    try {
    priceInput.value = window.SenteMoney
    ? SenteMoney.toRaw(priceInput.value)
    : toNumber(priceInput.value).toFixed(2);

    const formData = new FormData(form);

    const payload = await fetchJsonOrThrow(
    form.action,
    formData,
    "İşlem güncellenirken hata oluştu."
    );

    replaceOperationInRequest(payload, form);
    updateSummaryFromPayload(payload?.summary);

    showToast("İşlem güncellendi.", "success");
    }
    catch (error) {
    showFriendlyError(error, "İşlem silinemedi. Lütfen tekrar deneyin.");
    }
    finally {
    form.dataset.submitting = "false";
    setButtonLoading(submitButton, false);
    }

    return;
    }
    });
    }

    function bindNewRequestForm() {
    const form = document.querySelector(".ajax-request-form");

    if (!form || form.dataset.bound === "true") return;

    form.dataset.bound = "true";

    form.addEventListener("submit", async e => {
    e.preventDefault();

    if (form.dataset.submitting === "true") return;

    const titleInput = form.querySelector('input[name="Title"]');

    if (!titleInput?.value.trim()) {
    showToast("Şikayet başlığı zorunludur.", "error");
    titleInput?.focus();
    return;
    }

    form.dataset.submitting = "true";

    const submitButton = form.querySelector('button[type="submit"]');

    setButtonLoading(submitButton, true, "Kaydediliyor...");

    try {
    const formData = new FormData(form);

    await fetchJsonOrThrow(
    form.action,
    formData,
    "Şikayet eklenirken hata oluştu."
    );

    showToast("Şikayet başarıyla eklendi.", "success");

    setTimeout(() => {
    window.location.reload();
    }, 450);
    }
    catch (error) {
    showToast(error.message, "error");
    }
    finally {
    form.dataset.submitting = "false";
    setButtonLoading(submitButton, false);
    }
    });
    }

    async function deleteOperation(operationId, button) {
    const confirmed = confirm(
    "Bu işlemi pasife almak istiyor musunuz?\n\nParça işlemiyse stok iadesi oluşur."
    );

    if (!confirmed) return;

    setButtonLoading(button, true, "Siliniyor...");

    try {
    const formData = new FormData();
    formData.append("operationId", operationId);

    const payload = await fetchJsonOrThrow(
    "/ServiceRecords/DeleteOperation",
    formData,
    "İşlem silinirken hata oluştu."
    );

    const operationCard = button.closest(".srx-operation-card");
    removeOperationFromRequest(payload, operationCard);
    updateSummaryFromPayload(payload?.summary);

    showToast("İşlem pasife alındı.", "success");
    }
    catch (error) {
    showToast(error.message, "error");
    }
    finally {
    if (document.body.contains(button)) {
    setButtonLoading(button, false);
    }
    }
    }

    async function deleteRequestItem(requestItemId, button) {
    const confirmed = confirm(
    "Bu şikayeti pasife almak istiyor musunuz?\n\nŞikayete bağlı işlemler de pasife alınır. Parça işlemleri varsa stok iadesi oluşur."
    );

    if (!confirmed) return;

    setButtonLoading(button, true, "Siliniyor...");

    try {
    const formData = new FormData();
    formData.append("requestItemId", requestItemId);

    await fetchJsonOrThrow(
    "/ServiceRecords/DeleteRequestItem",
    formData,
    "Şikayet silinirken hata oluştu."
    );

    showToast("Şikayet pasife alındı.", "success");

    setTimeout(() => {
    window.location.reload();
    }, 450);
    }
    catch (error) {
    showToast(error.message, "error");
    }
    finally {
    if (document.body.contains(button)) {
    setButtonLoading(button, false);
    }
    }
    }

    async function restoreRequestItem(requestItemId, button) {
    const confirmed = confirm(
    "Bu şikayeti geri almak istiyor musunuz?\n\nBağlı parça işlemleri varsa stok tekrar düşülecek."
    );

    if (!confirmed) return;

    setButtonLoading(button, true, "Geri alınıyor...");

    try {
    const formData = new FormData();
    formData.append("requestItemId", requestItemId);

    await fetchJsonOrThrow(
    "/ServiceRecords/RestoreRequestItem",
    formData,
    "Şikayet geri alınırken hata oluştu."
    );

    showToast("Şikayet geri alındı.", "success");

    setTimeout(() => {
    window.location.reload();
    }, 450);
    }
    catch (error) {
    showToast(error.message, "error");
    }
    finally {
    if (document.body.contains(button)) {
    setButtonLoading(button, false);
    }
    }
    }

    function bindStatusButtons() {
    document.addEventListener("click", async e => {
    const button = e.target.closest(".status-btn");

    if (!button) return;

    if (button.dataset.loading === "true") return;

    button.dataset.loading = "true";
    setButtonLoading(button, true, "Güncelleniyor...");

    try {
    const status = button.dataset.status;

    const formData = new FormData();
    formData.append("ServiceRecordId", serviceRecordId);
    formData.append("Status", status);

    const response = await fetch("/ServiceRecords/UpdateStatus", {
    method: "POST",
    body: formData,
    credentials: "same-origin"
    });

    if (!response.ok) {
    showToast("Durum güncellenirken hata oluştu.", "error");
    return;
    }

    updateStatusBadge(status);
    refreshStatusButtons(status);

    showToast("Durum güncellendi.", "success");
    }
    finally {
    if (document.body.contains(button)) {
    button.dataset.loading = "false";
    setButtonLoading(button, false);
    }
    }
    });
    }

    function bindPageActions() {
    document.addEventListener("click", e => {
    const actionElement = e.target.closest("[data-action]");

    if (!actionElement) return;

    const action = actionElement.dataset.action;

    if (action === "toggle-info") {
    const card = actionElement.closest(".srx-info-card");

    if (card) {
    card.classList.toggle("collapsed");
    }

    return;
    }

    if (action === "toggle-deleted-requests") {
    const panel = document.getElementById("deletedRequestsPanel");

    if (panel) {
    panel.hidden = !panel.hidden;
    }

    return;
    }

    if (action === "toggle-request") {
    const card = actionElement.closest(".srx-request-card");

    if (!card) return;

    const isExpanded = card.dataset.expanded !== "false";

    setRequestExpanded(card, !isExpanded);

    return;
    }

    if (action === "edit-request") {
    const card = actionElement.closest(".srx-request-card");

    openRequestEditForm(card);

    return;
    }

    if (action === "cancel-edit-request") {
    const card = actionElement.closest(".srx-request-card");

    closeRequestEditForm(card);

    return;
    }

    if (action === "show-operation-form") {
    const card = actionElement.closest(".srx-request-card");
    const form = card?.querySelector(".srx-operation-form");

    if (!form) return;

    setRequestExpanded(card, true);

    form.hidden = false;
    actionElement.hidden = true;

    const input = form.querySelector(".srx-operation-search-input");

    setTimeout(() => input?.focus(), 50);

    return;
    }

    if (action === "edit-operation") {
    const operationCard = actionElement.closest(".srx-operation-card");

    openOperationEditForm(operationCard);

    return;
    }

    if (action === "cancel-edit-operation") {
    const operationCard = actionElement.closest(".srx-operation-card");

    closeOperationEditForm(operationCard);

    return;
    }

    if (action === "delete-operation") {
    const operationCard = actionElement.closest(".srx-operation-card");
    const operationId =
    actionElement.dataset.operationId ||
    operationCard?.dataset.operationId;

    if (operationId) {
    deleteOperation(operationId, actionElement);
    }

    return;
    }

    if (action === "delete-request") {
    const requestId = actionElement.dataset.requestId;

    if (requestId) {
    deleteRequestItem(requestId, actionElement);
    }

    return;
    }

    if (action === "restore-request") {
    const requestId = actionElement.dataset.requestId;

    if (requestId) {
    restoreRequestItem(requestId, actionElement);
    }

    return;
    }

    if (action === "toggle-new-request") {
    const form = document.querySelector(".srx-new-request-form");
    const topButton = document.querySelector(".srx-work-command-btn");
    const cardButton = document.querySelector(".srx-new-request-trigger");

    if (!form) return;

    form.hidden = false;

    if (topButton) topButton.hidden = true;
    if (cardButton) cardButton.hidden = true;

    const input = form.querySelector('input[name="Title"]');

    setTimeout(() => input?.focus(), 50);

    return;
    }

    if (action === "cancel-new-request") {
    const form = actionElement.closest(".srx-new-request-form");
    const topButton = document.querySelector(".srx-work-command-btn");
    const cardButton = document.querySelector(".srx-new-request-trigger");

    if (form) {
    form.reset();
    form.hidden = true;
    }

    if (topButton) topButton.hidden = false;
    if (cardButton) cardButton.hidden = false;

    return;
    }
    });
    }

    let qrScanner = null;
    let qrScanLocked = false;

    async function startQrScanner(vehicleId, button) {
    if (qrScanLocked) return;

    const reader = document.getElementById("qr-reader");
    const resultBox = document.getElementById("qr-scan-result");

    if (!reader || !resultBox) return;

    if (typeof Html5Qrcode === "undefined") {
    resultBox.textContent = "QR okuyucu yüklenemedi.";
    return;
    }

    reader.hidden = false;
    resultBox.textContent = "Kamera açılıyor.";

    button.disabled = true;
    button.textContent = "Taranıyor.";

    try {
    const cameras = await Html5Qrcode.getCameras();

    if (!cameras || cameras.length === 0) {
    resultBox.textContent = "Kamera bulunamadı.";
    button.disabled = false;
    button.textContent = "QR Tara";
    qrScanLocked = false;
    return;
    }

    qrScanner = new Html5Qrcode("qr-reader");

    await qrScanner.start(
    { facingMode: "environment" },
    {
    fps: 10,
    qrbox: {
    width: 220,
    height: 220
    }
    },
    async decodedText => {
    if (qrScanLocked) return;

    qrScanLocked = true;

    resultBox.textContent = `QR okundu: ${decodedText}`;

    await qrScanner.stop();

    reader.hidden = true;

    await assignScannedQrCode(
    vehicleId,
    decodedText,
    button,
    resultBox
    );
    }
    );
    }
    catch (error) {
    console.error(error);

    resultBox.textContent =
    "Kamera hatası: " + (error?.message || error);

    button.disabled = false;
    button.textContent = "QR Tara";
    qrScanLocked = false;
    }
    }

    async function assignScannedQrCode(vehicleId, code, button, resultBox) {
    button.textContent = "Atanıyor.";

    const formData = new FormData();
    formData.append("vehicleId", vehicleId);
    formData.append("code", code.trim());

    const response = await fetch("/ServiceRecords/AssignQrCode", {
    method: "POST",
    body: formData,
    credentials: "same-origin"
    });

    if (!response.ok) {
    const errorText = await response.text();

    resultBox.textContent = errorText || "QR atanırken hata oluştu.";

    button.disabled = false;
    button.textContent = "Tekrar Tara";
    qrScanLocked = false;

    return;
    }

    resultBox.textContent = "QR kod araca başarıyla atandı.";
    button.textContent = "✓ QR Bağlı";

    showToast("QR kod başarıyla eşleştirildi.", "success");
    }

    

/* === Service record photo modal === */

let selectedPhotoFiles = [];
let selectedPhotoPreviewUrls = [];
let activePhotoCameraStream = null;

const PHOTO_MAX_LONG_EDGE = 1920;
const PHOTO_TARGET_MAX_BYTES = 4.5 * 1024 * 1024;
const PHOTO_INITIAL_QUALITY = 0.84;
const PHOTO_MIN_QUALITY = 0.62;

async function compressImageFile(file) {
    if (!file || !file.type?.startsWith("image/")) {
        return file;
    }

    // Zaten küçükse yine de aşırı dokunmayalım.
    if (file.size <= PHOTO_TARGET_MAX_BYTES && file.type === "image/jpeg") {
        return file;
    }

    const imageBitmap = await createImageBitmap(file);

    let { width, height } = imageBitmap;

    const longEdge = Math.max(width, height);

    if (longEdge > PHOTO_MAX_LONG_EDGE) {
        const ratio = PHOTO_MAX_LONG_EDGE / longEdge;
        width = Math.round(width * ratio);
        height = Math.round(height * ratio);
    }

    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;

    const context = canvas.getContext("2d");
    context.drawImage(imageBitmap, 0, 0, width, height);

    imageBitmap.close?.();

    let quality = PHOTO_INITIAL_QUALITY;
    let blob = await canvasToJpegBlob(canvas, quality);

    while (blob.size > PHOTO_TARGET_MAX_BYTES && quality > PHOTO_MIN_QUALITY) {
        quality -= 0.08;
        blob = await canvasToJpegBlob(canvas, quality);
    }

    const originalNameWithoutExtension = (file.name || "foto")
        .replace(/\.[^/.]+$/, "");

    return new File(
        [blob],
        `${originalNameWithoutExtension}.jpg`,
        {
            type: "image/jpeg",
            lastModified: Date.now()
        });
}

function canvasToJpegBlob(canvas, quality) {
    return new Promise((resolve, reject) => {
        canvas.toBlob(
            blob => {
                if (!blob) {
                    reject(new Error("Fotoğraf sıkıştırılamadı."));
                    return;
                }

                resolve(blob);
            },
            "image/jpeg",
            quality
        );
    });
}

async function openInlineCamera() {
    const panel = document.getElementById("photoCameraPanel");
    const video = document.getElementById("photoCameraVideo");

    if (!panel || !video) return;

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        showToast("Kamera açılamadı. Fotoğraf yükleyerek devam edebilirsiniz.", "error");
        return;
    }

    try {
        await closeInlineCamera(false);

        let stream = null;

        try {
            stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: { ideal: "environment" },
                    width: { ideal: 1280 },
                    height: { ideal: 720 }
                },
                audio: false
            });
        }
        catch (environmentError) {
            console.warn("Arka kamera açılamadı, varsayılan kamera deneniyor.", environmentError);

            stream = await navigator.mediaDevices.getUserMedia({
                video: true,
                audio: false
            });
        }

        activePhotoCameraStream = stream;

        video.srcObject = activePhotoCameraStream;
        panel.hidden = false;

        await video.play();

        showToast("Kamera açıldı.", "success");
    }
    catch (error) {
        console.error("Kamera açma hatası:", error);

        showToast("Kamera açılamadı. Fotoğraf yükleyerek devam edebilirsiniz.", "error");
    }
}
async function closeInlineCamera(showInfo = true) {
    const panel = document.getElementById("photoCameraPanel");
    const video = document.getElementById("photoCameraVideo");

    if (activePhotoCameraStream) {
        activePhotoCameraStream.getTracks().forEach(track => track.stop());
        activePhotoCameraStream = null;
    }

    if (video) {
        video.srcObject = null;
    }

    if (panel) {
        panel.hidden = true;
    }

    if (showInfo) {
        showToast("Kamera kapatıldı.", "info");
    }
}

function capturePhotoFromCamera() {
    const video = document.getElementById("photoCameraVideo");
    const canvas = document.getElementById("photoCameraCanvas");

    if (!video || !canvas || !activePhotoCameraStream) {
        showToast("Önce kamerayı açmalısın.", "error");
        return;
    }

    const width = video.videoWidth;
    const height = video.videoHeight;

    if (!width || !height) {
        showToast("Kamera görüntüsü henüz hazır değil.", "error");
        return;
    }

    canvas.width = width;
    canvas.height = height;

    const context = canvas.getContext("2d");

    if (!context) {
        showToast("Fotoğraf işlenemedi.", "error");
        return;
    }

    context.drawImage(video, 0, 0, width, height);

    canvas.toBlob(async function (blob) {
        if (!blob) {
            showToast("Fotoğraf alınamadı.", "error");
            return;
        }

        const fileName = `kamera-${new Date().toISOString().replace(/[:.]/g, "-")}.jpg`;

        const file = new File([blob], fileName, {
            type: "image/jpeg"
        });

        await addSelectedPhotos([file]);

        showToast("Fotoğraf seçilenlere eklendi.", "success");
    }, "image/jpeg", 0.82);
}
function openPhotoModal() {
    const modal = document.getElementById("photoModal");

    if (!modal) return;

    modal.hidden = false;
    document.body.style.overflow = "hidden";
}

function closePhotoModal() {
    const modal = document.getElementById("photoModal");

    if (!modal) return;

    closeInlineCamera(false);

    modal.hidden = true;
    document.body.style.overflow = "";
}

function getServiceRecordId() {
    const modal = document.getElementById("photoModal");
    return modal?.dataset.serviceRecordId || "";
}

function getPhotoAntiForgeryToken() {
    return document.querySelector("#photoUploadForm input[name='__RequestVerificationToken']")?.value || "";
}

function formatFileSize(bytes) {
    if (!bytes) return "";

    if (bytes < 1024 * 1024) {
        return `${Math.max(1, Math.round(bytes / 1024))} KB`;
    }

    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}



async function addSelectedPhotos(files) {
    const validFiles = Array.from(files || [])
        .filter(file => file && file.type && file.type.startsWith("image/"));

    if (!validFiles.length) {
        showToast("Sadece fotoğraf seçebilirsin.", "error");
        return;
    }

    showToast("Fotoğraflar hazırlanıyor...", "info");

    const preparedFiles = [];

    for (const file of validFiles) {
        try {
            const compressedFile = await compressImageFile(file);
            preparedFiles.push(compressedFile);
        }
        catch (error) {
            console.error("Image compression error:", error);
            showToast(`${file.name || "Fotoğraf"} hazırlanamadı.`, "error");
        }
    }

    if (!preparedFiles.length) {
        showToast("Fotoğraf eklenemedi.", "error");
        return;
    }

    selectedPhotoFiles.push(...preparedFiles);

    renderSelectedPhotoList();

    showToast(`${preparedFiles.length} fotoğraf seçilenlere eklendi.`, "success");
}

function renderSelectedPhotoList() {
    const list = document.getElementById("photoSelectedList");

    if (!list) return;

    selectedPhotoPreviewUrls.forEach(url => URL.revokeObjectURL(url));
    selectedPhotoPreviewUrls = [];

    if (!selectedPhotoFiles.length) {
        list.innerHTML = "";
        list.hidden = true;
        return;
    }

    const html = selectedPhotoFiles.map((file, index) => {
        const previewUrl = URL.createObjectURL(file);
        selectedPhotoPreviewUrls.push(previewUrl);

        return `
            <article class="sr-photo-selected-item">
                <img src="${previewUrl}" alt="Seçilen fotoğraf" />

                <div>
                    <strong>${escapeHtml(file.name || "Fotoğraf")}</strong>
                    <span>${formatFileSize(file.size)}</span>
                </div>

                <button type="button"
                        data-action="remove-selected-photo"
                        data-index="${index}">
                    ×
                </button>
            </article>
        `;
    }).join("");

    list.innerHTML = html;
    list.hidden = false;
}

function removeSelectedPhoto(index) {
    selectedPhotoFiles.splice(index, 1);
    renderSelectedPhotoList();
}

function clearSelectedPhoto() {
    selectedPhotoFiles = [];

    selectedPhotoPreviewUrls.forEach(url => URL.revokeObjectURL(url));
    selectedPhotoPreviewUrls = [];

    const cameraInput = document.getElementById("photoCameraInput");
    const galleryInput = document.getElementById("photoGalleryInput");
    const list = document.getElementById("photoSelectedList");

    if (cameraInput) cameraInput.value = "";
    if (galleryInput) galleryInput.value = "";

    if (list) {
        list.innerHTML = "";
        list.hidden = true;
    }
}

function updatePhotoCount() {
    const count = document.querySelectorAll(".sr-photo-tile").length;

    const modalBadge = document.getElementById("photoCountBadge");
    const actionCount = document.getElementById("photoActionCount");
    const emptyState = document.getElementById("photoEmptyState");

    if (modalBadge) modalBadge.textContent = `${count} fotoğraf`;
    if (actionCount) actionCount.textContent = `${count} fotoğraf`;
    if (emptyState) emptyState.hidden = count > 0;
}

function renderPhotoTile(image) {
    const gallery = document.getElementById("photoGalleryGrid");

    if (!gallery || !image?.id) return;

    const article = document.createElement("article");
    article.className = "sr-photo-tile";
    article.dataset.photoId = image.id;

    const imageUrl = image.imageUrl || `${config.photoContentBaseUrl}/${image.id}`;
    const typeText = image.typeText || "Fotoğraf";
    const description = image.description || "";
    const createdAt = image.createdAt
        ? new Date(image.createdAt).toLocaleString("tr-TR", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        })
        : "";

    article.innerHTML = `
        <button type="button"
                class="sr-photo-delete"
                data-action="delete-photo"
                data-photo-id="${image.id}"
                title="Fotoğrafı sil">
            ×
        </button>

        <a href="${imageUrl}" target="_blank">
            <img src="${imageUrl}" alt="${escapeHtml(typeText)}" />
        </a>

        <div class="sr-photo-tile-meta">
            <strong>${escapeHtml(typeText)}</strong>
            ${description ? `<span>${escapeHtml(description)}</span>` : ""}
            ${createdAt ? `<small>${createdAt}</small>` : ""}
        </div>
    `;

    gallery.prepend(article);
    updatePhotoCount();
}

async function uploadSelectedPhoto(button) {
    if (!selectedPhotoFiles.length) {
        showToast("Önce kamera veya dosyadan fotoğraf seçmelisin.", "error");
        return;
    }

    const recordId = getServiceRecordId();

    if (!recordId) {
        showToast("Servis kaydı bilgisi bulunamadı.", "error");
        return;
    }

    const checkedType =
        document.querySelector("input[name='photoType']:checked")?.value ||
        "BeforeRepair";

    const description =
        document.getElementById("photoDescriptionInput")?.value || "";

    const formData = new FormData();

    formData.append("__RequestVerificationToken", getPhotoAntiForgeryToken());
    formData.append("type", checkedType);
    formData.append("description", description);

    selectedPhotoFiles.forEach(file => {
        formData.append("files", file);
    });

    const originalText = button?.innerText;

    if (button) {
        button.disabled = true;
        button.innerText = `${selectedPhotoFiles.length} fotoğraf yükleniyor...`;
    }

    try {
        const uploadUrl = `/ServiceRecords/${recordId}/Photos`;

        const response = await fetch(uploadUrl, {
            method: "POST",
            body: formData,
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        const contentType = response.headers.get("content-type") || "";

        if (!contentType.includes("application/json")) {
            const text = await response.text();
            console.error("Photo upload non-json response:", text);

            showToast("Fotoğraf yükleme cevabı beklenen formatta değil.", "error");
            return;
        }

        const result = await response.json();

        if (!response.ok || result.success === false) {
            showToast(result.message || "Fotoğraf yüklenemedi.", "error");
            return;
        }

        const images = result.images || [];

        images.forEach(image => renderPhotoTile(image));

        clearSelectedPhoto();

        const descriptionInput = document.getElementById("photoDescriptionInput");

        if (descriptionInput) {
            descriptionInput.value = "";
        }

        showToast(result.message || "Fotoğraflar eklendi.", "success");
    }
    catch (error) {
        console.error("Photo upload error:", error);
        showToast("Fotoğraflar yüklenirken beklenmeyen bir hata oluştu.", "error");
    }
    finally {
        if (button) {
            button.disabled = false;
            button.innerText = originalText || "Seçilen Fotoğrafları Kaydet";
        }
    }
}

async function deletePhoto(photoId, button) {
    const confirmed = confirm("Bu fotoğraf silinsin mi? Bu işlem geri alınamaz.");

    if (!confirmed) return;

    const formData = new FormData();
    formData.append("__RequestVerificationToken", getPhotoAntiForgeryToken());

    setButtonLoading(button, true, "×");

    try {
        const response = await fetch(`${config.photoDeleteBaseUrl}/${photoId}/Delete`, {
            method: "POST",
            body: formData,
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        const result = await response.json();

        if (!response.ok || result.success === false) {
            showToast(result.message || "Fotoğraf silinemedi.", "error");
            return;
        }

        document.querySelector(`.sr-photo-tile[data-photo-id="${photoId}"]`)?.remove();

        updatePhotoCount();

        showToast(result.message || "Fotoğraf silindi.", "success");
    }
    catch {
        showToast("Fotoğraf silinirken beklenmeyen bir hata oluştu.", "error");
    }
    finally {
        setButtonLoading(button, false);
    }
}

function initializePhotoModal() {
    const galleryInput = document.getElementById("photoGalleryInput");
    const cameraInput = document.getElementById("photoCameraInput");

    galleryInput?.addEventListener("change", async function () {
        const files = Array.from(this.files || []);

        if (!files.length) {
            return;
        }

        await addSelectedPhotos(files);
        this.value = "";
    });

    cameraInput?.addEventListener("change", async function () {
        const files = Array.from(this.files || []);

        if (!files.length) {
            return;
        }

        await addSelectedPhotos(files);
        this.value = "";
    });

    document.addEventListener("click", function (e) {
        const actionElement = e.target.closest("[data-action]");

        if (!actionElement) return;

        const action = actionElement.dataset.action;

        if (action === "open-photo-modal") {
            openPhotoModal();
            return;
        }

        if (action === "close-photo-modal") {
            closePhotoModal();
            return;
        }

        if (action === "pick-photo-camera") {
            cameraInput?.click();
            return;
        }

        if (action === "pick-photo-gallery") {
            galleryInput?.click();
            return;
        }

        if (action === "remove-selected-photo") {
            removeSelectedPhoto(Number(actionElement.dataset.index));
            return;
        }

        if (action === "clear-selected-photo") {
            clearSelectedPhoto();
            return;
        }

        if (action === "upload-photo") {
            uploadSelectedPhoto(actionElement);
            return;
        }

        if (action === "delete-photo") {
            deletePhoto(actionElement.dataset.photoId, actionElement);
            return;
        }
    });

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
            closePhotoModal();
        }
    });

    const urlParams = new URLSearchParams(window.location.search);

    if (urlParams.get("openPhotos") === "1") {
        openPhotoModal();

        const cleanUrl = window.location.pathname;
        window.history.replaceState({}, document.title, cleanUrl);
    }

    updatePhotoCount();
}


document.addEventListener("DOMContentLoaded", () => {
    bindPageActions();
    bindOperationForms();
    bindInlineEditForms();
    bindNewRequestForm();
    bindStatusButtons();
    initializePhotoModal();
    });
