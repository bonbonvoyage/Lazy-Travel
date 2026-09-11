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
const toggleDetailInteraction = async (form, apply) => {
    const response = await fetch(form.action, {
        method: 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        redirect: 'manual'
    });

    if (!response.ok && response.type !== 'opaqueredirect') throw new Error('interaction failed');
    const result = response.headers.get('content-type')?.includes('application/json') ? await response.json() : null;
    if (result) apply(result);
};

const setArticleButtonState = (button, active) => {
    button.classList.toggle('active', active);
    button.setAttribute('aria-pressed', String(active));
};

document.querySelector('[data-detail-favorite-form]')?.addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget;
    const button = form.querySelector('[data-detail-favorite-button]');
    const text = form.querySelector('[data-detail-favorite-text]');
    if (!button) return;

    button.disabled = true;
    try {
        await toggleDetailInteraction(form, result => {
            setArticleButtonState(button, result.active === true);
            if (text) text.textContent = result.active ? '已收藏' : '收藏';
        });
    } finally {
        button.disabled = false;
    }
});

document.querySelector('[data-detail-like-form]')?.addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget;
    const button = form.querySelector('[data-detail-like-button]');
    const count = form.querySelector('[data-detail-like-count]');
    if (!button) return;

    button.disabled = true;
    try {
        await toggleDetailInteraction(form, result => {
            setArticleButtonState(button, result.active === true);
            if (count && Number.isInteger(result.count)) count.textContent = result.count;
        });
    } finally {
        button.disabled = false;
    }
});



const openArticleDeleteModal = form => {
    document.getElementById('article-delete-modal')?.remove();
    document.body.insertAdjacentHTML('beforeend', `
        <div class="article-delete-backdrop" id="article-delete-modal" role="dialog" aria-modal="true" aria-label="刪除文章確認">
            <div class="article-delete-card">
                <h2>確定要刪除文章嗎？</h2>
                <div class="article-delete-message" data-delete-message></div>
                <div class="article-delete-actions">
                    <button class="article-delete-cancel" type="button">取消</button>
                    <button class="article-delete-confirm" type="button">確認刪除</button>
                </div>
            </div>
        </div>`);
    const modal = document.getElementById('article-delete-modal');
    const message = modal.querySelector('[data-delete-message]');
    const close = () => modal.remove();
    modal.querySelector('.article-delete-cancel').addEventListener('click', close);
    modal.addEventListener('click', event => { if (event.target === modal) close(); });
    modal.querySelector('.article-delete-confirm').addEventListener('click', async event => {
        const button = event.currentTarget;
        button.disabled = true;
        button.textContent = '刪除中...';
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const data = response.headers.get('content-type')?.includes('application/json') ? await response.json() : null;
            if (!response.ok) throw new Error(data?.message || '刪除失敗，請確認目前身分是否為文章作者。');
            window.location.assign(data?.redirectUrl || '/Explore');
        } catch (error) {
            message.textContent = error.message || '刪除失敗，請稍後再試。';
            button.disabled = false;
            button.textContent = '確認刪除';
        }
    });
};

document.querySelector('[data-detail-delete-form]')?.addEventListener('submit', event => {
    event.preventDefault();
    openArticleDeleteModal(event.currentTarget);
});
