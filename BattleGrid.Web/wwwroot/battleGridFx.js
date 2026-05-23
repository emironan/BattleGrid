// Preload shot effect sprites so the first frame after a salvo is not skipped while images load.
window.battleGridFx = {
    preload(urls) {
        if (!urls || !urls.length) return;
        for (const url of urls) {
            const img = new Image();
            img.src = url;
        }
    }
};
