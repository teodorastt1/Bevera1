// Bevera UX helpers (no external deps)

document.addEventListener("DOMContentLoaded", function () {

    // =============================
    // Mega menu (Categories)
    // =============================
    const megaWrapper = document.getElementById("megaWrapper");

    if (megaWrapper) {
        const megaToggle = megaWrapper.querySelector(".mega-toggle");
        const megaMenu = megaWrapper.querySelector(".bevera-mega-menu");
        const megaCloseBtn = megaWrapper.querySelector(".mega-close-btn");

        function openMegaMenu() {
            megaWrapper.classList.add("mega-open");

            if (megaToggle) {
                megaToggle.setAttribute("aria-expanded", "true");
            }
        }

        function closeMegaMenu() {
            megaWrapper.classList.remove("mega-open");

            if (megaToggle) {
                megaToggle.setAttribute("aria-expanded", "false");
            }
        }

        function toggleMegaMenu() {
            if (megaWrapper.classList.contains("mega-open")) {
                closeMegaMenu();
            } else {
                openMegaMenu();
            }
        }

        if (megaToggle && megaMenu) {
            megaToggle.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                toggleMegaMenu();
            });

            if (megaCloseBtn) {
                megaCloseBtn.addEventListener("click", function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    closeMegaMenu();
                });
            }

            megaMenu.addEventListener("click", function (e) {
                e.stopPropagation();
            });

            document.addEventListener("click", function (e) {
                if (!megaWrapper.contains(e.target)) {
                    closeMegaMenu();
                }
            });

            document.addEventListener("keydown", function (e) {
                if (e.key === "Escape") {
                    closeMegaMenu();
                }
            });
        }
    }

    // =============================
    // Lightweight JS messages
    // Use: <div class="js-flash" data-message="..." data-type="success"></div>
    // =============================
    const flashEl = document.querySelector(".js-flash");

    if (flashEl) {
        const message = flashEl.getAttribute("data-message");
        const type = flashEl.getAttribute("data-type") || "info";

        if (message) {
            let alertClass = "alert-info";

            if (type === "success") {
                alertClass = "alert-success";
            } else if (type === "danger") {
                alertClass = "alert-danger";
            } else if (type === "warning") {
                alertClass = "alert-warning";
            }

            const box = document.createElement("div");
            box.className = `alert ${alertClass} shadow-sm rounded-4 js-flash-box`;
            box.setAttribute("role", "alert");

            box.innerHTML = `
                <div class="d-flex align-items-center justify-content-between gap-3">
                    <div>${message}</div>
                    <button type="button" class="btn-close" aria-label="Close"></button>
                </div>
            `;

            document.body.appendChild(box);

            setTimeout(function () {
                box.classList.add("show");
            }, 10);

            function closeFlash() {
                box.classList.remove("show");

                setTimeout(function () {
                    box.remove();
                }, 150);
            }

            const closeBtn = box.querySelector(".btn-close");
            if (closeBtn) {
                closeBtn.addEventListener("click", closeFlash);
            }

            setTimeout(closeFlash, 3500);
        }
    }

    // =============================
    // Promo countdown timers
    // =============================
    const discountTimers = document.querySelectorAll(".promo-timer");

    discountTimers.forEach(timer => {
        const endValue = timer.getAttribute("data-discount-end");
        const span = timer.querySelector("span");

        if (!endValue || !span) return;

        function updateTimer() {
            const end = new Date(endValue).getTime();
            const now = new Date().getTime();
            const diff = end - now;

            if (diff <= 0) {
                span.textContent = "изтекла";
                return;
            }

            const days = Math.floor(diff / (1000 * 60 * 60 * 24));
            const hours = Math.floor((diff / (1000 * 60 * 60)) % 24);
            const minutes = Math.floor((diff / (1000 * 60)) % 60);
            const seconds = Math.floor((diff / 1000) % 60);

            if (days > 0) {
                span.textContent = `${days}д ${hours}ч ${minutes}м`;
            } else {
                span.textContent = `${hours}ч ${minutes}м ${seconds}с`;
            }
        }

        updateTimer();
        setInterval(updateTimer, 1000);
    });
});