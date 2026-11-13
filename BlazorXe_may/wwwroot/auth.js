document.addEventListener("submit", async event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.action.startsWith(`${location.origin}/auth/`)) {
        return;
    }

    const token = form.querySelector('input[name="__RequestVerificationToken"]');
    if (!token || token.value || form.dataset.csrfReady === "true") {
        return;
    }

    event.preventDefault();
    const submitter = event.submitter;
    try {
        const response = await fetch("/auth/csrf", {
            credentials: "same-origin",
            cache: "no-store"
        });
        if (!response.ok) {
            throw new Error("Không thể khởi tạo phiên bảo mật.");
        }

        const result = await response.json();
        token.value = result.requestToken;
        form.dataset.csrfReady = "true";
        form.requestSubmit(submitter);
    } catch {
        const message = document.createElement("div");
        message.className = "alert alert-danger mt-3";
        message.setAttribute("role", "alert");
        message.textContent = "Không thể xác minh phiên bảo mật. Hãy tải lại trang và thử lại.";
        form.prepend(message);
    }
}, true);
