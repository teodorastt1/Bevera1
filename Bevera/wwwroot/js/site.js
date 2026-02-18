// Bevera UX helpers (no external deps)

// -----------------------------
// Mega menu (Categories)
// -----------------------------
(function () {
    const wrapper = document.getElementById('megaWrapper');
    if (!wrapper) return;

    const toggle = wrapper.querySelector('.mega-toggle');
    const menu = wrapper.querySelector('.mega-menu');
    const closeBtn = wrapper.querySelector('.mega-close');

    function setOpen(open) {
        wrapper.classList.toggle('mega-open', open);
        if (toggle) toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    toggle?.addEventListener('click', () => {
        const isOpen = wrapper.classList.contains('mega-open');
        setOpen(!isOpen);
    });

    closeBtn?.addEventListener('click', () => setOpen(false));

    // Click outside closes
    document.addEventListener('click', (e) => {
        if (!wrapper.classList.contains('mega-open')) return;
        if (wrapper.contains(e.target)) return;
        setOpen(false);
    });

    // ESC closes
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') setOpen(false);
    });

    // Keep menu open on inner clicks
    menu?.addEventListener('click', (e) => e.stopPropagation());
})();

// -----------------------------
// Lightweight JS messages
// Use: <div class="js-flash" data-message="..." data-type="success"></div>
// -----------------------------
(function () {
    const el = document.querySelector('.js-flash');
    if (!el) return;

    const msg = el.getAttribute('data-message');
    if (!msg) return;

    const type = el.getAttribute('data-type') || 'info';
    const cls = type === 'success' ? 'alert-success'
        : type === 'danger' ? 'alert-danger'
        : type === 'warning' ? 'alert-warning'
        : 'alert-info';

    const box = document.createElement('div');
    box.className = `alert ${cls} shadow-sm rounded-4 js-flash-box`;
    box.role = 'alert';
    box.innerHTML = `
        <div class="d-flex align-items-center justify-content-between gap-3">
          <div>${msg}</div>
          <button type="button" class="btn-close" aria-label="Close"></button>
        </div>`;

    document.body.appendChild(box);
    setTimeout(() => box.classList.add('show'), 10);

    const close = () => {
        box.classList.remove('show');
        setTimeout(() => box.remove(), 150);
    };

    box.querySelector('.btn-close')?.addEventListener('click', close);
    setTimeout(close, 3500);
})();
