const photoData = document.getElementById('articlePhotos');
document.querySelectorAll('[data-back-fallback]').forEach(button => {
    button.addEventListener('click', () => {
        if (window.history.length > 1) {
            window.history.back();
            return;
        }
        window.location.assign(button.dataset.backFallback || '/Explore');
    });
});

const dialog = document.querySelector('.photo-dialog');
if (photoData && dialog) {
    const photos = JSON.parse(photoData.textContent);
    const full = document.getElementById('photoFull');
    const counter = document.getElementById('photoCounter');
    let current = 0;
    function show(index) {
        if (!photos.length) return;
        current = (index + photos.length) % photos.length;
        full.src = photos[current];
        full.alt = '旅程照片 ' + (current + 1);
        counter.textContent = (current + 1) + ' / ' + photos.length;
    }
    document.querySelectorAll('[data-photo-index]').forEach(button => {
        button.addEventListener('click', () => {
            show(Number(button.dataset.photoIndex));
            dialog.showModal();
        });
    });
    dialog.querySelector('.photo-close').addEventListener('click', () => dialog.close());
    dialog.querySelector('.photo-prev').addEventListener('click', () => show(current - 1));
    dialog.querySelector('.photo-next').addEventListener('click', () => show(current + 1));
    dialog.addEventListener('keydown', event => {
        if (event.key === 'ArrowLeft') { event.preventDefault(); show(current - 1); }
        if (event.key === 'ArrowRight') { event.preventDefault(); show(current + 1); }
    });
    dialog.addEventListener('click', event => {
        if (event.target === dialog) {
            const rect = dialog.getBoundingClientRect();
            if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close();
        }
    });
}
