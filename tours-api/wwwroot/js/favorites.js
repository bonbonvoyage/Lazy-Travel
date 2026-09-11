(() => {
    let pendingRemoval = null;
    let toastTimer = 0;

    const contentRoot = () => document.querySelector('[data-favorites-content]');

    const loadFavoritesContent = async (url, push = true) => {
        const root = contentRoot();
        if (!root) return;
        root.classList.add('is-loading');
        const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!response.ok) throw new Error('favorites load failed');
        const html = await response.text();
        const template = document.createElement('template');
        template.innerHTML = html.trim();
        const nextRoot = template.content.querySelector('[data-favorites-content]');
        root.replaceWith(nextRoot ?? template.content);
        bindFavoritesInteractions();
        if (push) window.history.pushState({}, '', url);
    };

    const buildUrlFromForm = (form) => {
        const data = new FormData(form);
        const params = new URLSearchParams();
        data.forEach((value, key) => {
            const text = String(value).trim();
            if (text) params.set(key, text);
        });
        return `${form.action || window.location.pathname}?${params.toString()}`;
    };

    const toggleArticleFavorite = async (form) => {
        const response = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            redirect: 'manual'
        });
        if (!response.ok && response.type !== 'opaqueredirect') throw new Error('favorite request failed');
    };

    const toggleTripFavorite = async (id) => {
        const response = await fetch('/TravelGroups/ToggleFavorite/' + id, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            redirect: 'manual'
        });
        if (!response.ok && response.type !== 'opaqueredirect') throw new Error('favorite request failed');
    };

    const activeCards = () => document.querySelectorAll('[data-favorite-card]');

    const maybeReloadEmpty = () => {
        if (activeCards().length === 0) loadFavoritesContent(window.location.href, false).catch(() => window.location.reload());
    };

    const restoreCard = (card, button, parent, next) => {
        if (next?.parentElement === parent) parent.insertBefore(card, next);
        else parent?.appendChild(card);
        card.classList.remove('is-removing');
        card.classList.add('is-restoring');
        window.setTimeout(() => card.classList.remove('is-restoring'), 240);
        button.disabled = false;
    };

    const removeCardWithReflow = (card) => {
        card.classList.add('is-removing');
        window.setTimeout(() => card.remove(), 180);
    };

    const showToast = (message, undo) => {
        const toast = document.querySelector('[data-favorites-toast]');
        if (!toast) return;
        const messageNode = toast.querySelector('[data-toast-message]');
        if (messageNode) messageNode.textContent = message;
        toast.hidden = false;
        pendingRemoval = undo;
        window.clearTimeout(toastTimer);
        toastTimer = window.setTimeout(() => {
            toast.hidden = true;
            pendingRemoval = null;
            maybeReloadEmpty();
        }, 3000);
    };

    const bindFavoritesInteractions = () => {
        const root = contentRoot();
        if (!root) return;

        root.querySelectorAll('.favorites-switch a, .sort-pills a').forEach(link => {
            link.addEventListener('click', (event) => {
                event.preventDefault();
                loadFavoritesContent(link.href).catch(() => { window.location.href = link.href; });
            });
        });

        root.querySelector('.favorites-search')?.addEventListener('submit', (event) => {
            event.preventDefault();
            const url = buildUrlFromForm(event.currentTarget);
            loadFavoritesContent(url).catch(() => { window.location.href = url; });
        });

        root.querySelectorAll('[data-favorite-remove]').forEach(button => {
            button.addEventListener('click', async (event) => {
                const card = button.closest('[data-favorite-card]');
                const id = Number(button.dataset.itemId);
                const kind = button.dataset.favoriteKind;
                if (!card || !id || !kind) return;

                event.preventDefault();
                button.disabled = true;
                const parent = card.parentElement;
                const next = card.nextElementSibling;

                if (kind === 'trip') {
                    try {
                        await toggleTripFavorite(id);
                        removeCardWithReflow(card);
                        showToast('已取消收藏', async () => {
                            await toggleTripFavorite(id);
                            restoreCard(card, button, parent, next);
                        });
                    } catch {
                        button.disabled = false;
                        showToast('取消收藏失敗，請再試一次', async () => {});
                    }
                    return;
                }

                const form = button.closest('form');
                try {
                    await toggleArticleFavorite(form);
                    removeCardWithReflow(card);
                    showToast('已取消收藏', async () => {
                        await toggleArticleFavorite(form);
                        restoreCard(card, button, parent, next);
                    });
                } catch {
                    button.disabled = false;
                    showToast('取消收藏失敗，請再試一次', async () => {});
                }
            });
        });
    };

    document.querySelector('[data-toast-undo]')?.addEventListener('click', async () => {
        if (!pendingRemoval) return;
        window.clearTimeout(toastTimer);
        const undo = pendingRemoval;
        pendingRemoval = null;
        await undo();
        const toast = document.querySelector('[data-favorites-toast]');
        if (toast) toast.hidden = true;
    });

    window.addEventListener('popstate', () => {
        loadFavoritesContent(window.location.href, false).catch(() => window.location.reload());
    });

    bindFavoritesInteractions();
})();
