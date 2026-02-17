document.addEventListener("click", function (e) {
    const mega = e.target.closest(".bevera-mega");
    if (mega) {
        e.stopPropagation(); // keep dropdown open on inner clicks
    }
});
