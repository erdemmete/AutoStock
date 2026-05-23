function showToast(message, type = "info") {
    const toast = document.getElementById("toastMessage");

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