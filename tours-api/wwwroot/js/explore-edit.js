(() => {
    const shell = document.querySelector('.editor-shell');
    if (!shell) return;

    const postId = shell.dataset.postId;
    const token = document.querySelector('#article-antiforgery input[name="__RequestVerificationToken"]')?.value ?? '';
    const dayList = document.querySelector('#dayList');
    const titleInput = document.querySelector('#articleTitle');
    const introInput = document.querySelector('#articleIntro');
    const highlightInput = document.querySelector('#articleHighlight');
    const saveButton = document.querySelector('[data-action="save"]');
    const addDayButton = document.querySelector('.add-day');
    const statusToggle = document.querySelector('[data-status-toggle]');

    const showToast = (message, type = 'ok') => {
        let toast = document.querySelector('.editor-toast');
        if (!toast) {
            toast = document.createElement('div');
            toast.className = 'editor-toast';
            document.body.appendChild(toast);
        }
        toast.textContent = message;
        toast.dataset.type = type;
        toast.classList.add('show');
        window.clearTimeout(showToast.timer);
        showToast.timer = window.setTimeout(() => toast.classList.remove('show'), 2200);
    };

    const renumberDays = () => {
        dayList?.querySelectorAll('[data-day]').forEach((card, index) => {
            const label = card.querySelector('strong');
            if (label) label.textContent = `DAY ${index + 1}`;
        });
    };

    const createDayCard = () => {
        const article = document.createElement('article');
        article.className = 'day-edit-card';
        article.dataset.day = '';
        article.innerHTML = `
            <strong>DAY</strong>
            <div class="day-fields">
                <input class="day-title" type="text" placeholder="輸入每日標題（例如：奈良公園、回程準備）" />
                <textarea class="day-route" rows="2" placeholder="描述當天的行程安排、美食、住宿或心得..."></textarea>
                <label class="note-row"><span>心得筆記</span><textarea class="day-note-input" rows="1" placeholder="寫下今天的旅行心得..."></textarea></label>
            </div>
            <button class="remove-day" type="button" aria-label="移除這一天">×</button>`;
        return article;
    };

    const collectPayload = () => ({
        title: titleInput?.value?.trim() ?? '',
        intro: introInput?.value?.trim() ?? '',
        highlight: highlightInput?.value?.trim() ?? '',
        days: [...(dayList?.querySelectorAll('[data-day]') ?? [])].map(card => ({
            title: card.querySelector('.day-title')?.value?.trim() ?? '',
            route: card.querySelector('.day-route')?.value?.trim() ?? '',
            note: card.querySelector('.day-note-input')?.value?.trim() ?? ''
        }))
    });

    const setStatus = (status) => {
        statusToggle?.querySelectorAll('[data-status-value]').forEach(button => {
            button.classList.toggle('active', button.dataset.statusValue === status);
        });
        if (saveButton) {
            saveButton.dataset.currentStatus = status;
            saveButton.textContent = status === 'submit' ? '✈ 送出' : '▣ 儲存';
        }
    };

    const setBusy = (button, busy) => {
        if (!button) return;
        button.disabled = busy;
        button.dataset.originalText ??= button.textContent;
        button.textContent = busy ? '處理中...' : button.dataset.originalText;
    };

    const send = async (action) => {
        const isSubmit = action === 'submit';
        const payload = collectPayload();
        if (isSubmit && !payload.title) {
            titleInput?.focus();
            showToast('請先輸入行程主題。', 'error');
            return;
        }

        setBusy(saveButton, true);
        try {
            const response = await fetch(`/Explore/${isSubmit ? 'Submit' : 'SaveDraft'}/${postId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            });
            const data = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(data.message || '儲存失敗，請稍後再試。');
            showToast(isSubmit ? '文章已送出審核。' : '草稿已儲存。');
            if (isSubmit && data.redirectUrl) window.setTimeout(() => { window.location.href = data.redirectUrl; }, 650);
        } catch (error) {
            showToast(error.message || '儲存失敗，請稍後再試。', 'error');
        } finally {
            setBusy(saveButton, false);
        }
    };

    addDayButton?.addEventListener('click', () => {
        dayList?.appendChild(createDayCard());
        renumberDays();
        dayList?.lastElementChild?.querySelector('input')?.focus();
    });

    dayList?.addEventListener('click', event => {
        const remove = event.target.closest('.remove-day');
        if (!remove) return;
        const cards = dayList.querySelectorAll('[data-day]');
        if (cards.length <= 1) {
            showToast('至少保留一天行程。', 'error');
            return;
        }
        remove.closest('[data-day]')?.remove();
        renumberDays();
    });

    saveButton?.addEventListener('click', () => send(saveButton.dataset.currentStatus || 'draft'));

    statusToggle?.addEventListener('click', event => {
        const button = event.target.closest('[data-status-value]');
        if (!button) return;
        setStatus(button.dataset.statusValue);
        saveButton?.focus();
    });

    setStatus(statusToggle?.querySelector('[data-status-value].active')?.dataset.statusValue || 'draft');

    document.querySelectorAll('[data-file-target]').forEach(button => {
        button.addEventListener('click', () => {
            const input = document.getElementById(button.dataset.fileTarget);
            input?.click();
        });
    });

    const coverInput = document.querySelector('#coverImageInput');
    const coverPicker = document.querySelector('.cover-preview');
    const removeCover = document.querySelector('.remove-cover');
    coverInput?.addEventListener('change', () => {
        const file = coverInput.files?.[0];
        if (!file) return;
        const url = URL.createObjectURL(file);
        coverPicker.innerHTML = `<img src="${url}" alt="封面圖片預覽" data-cover-preview /><span class="upload-hint">點擊更換封面</span>`;
        removeCover?.classList.remove('hidden');
        showToast('封面圖片已加入預覽。');
    });

    removeCover?.addEventListener('click', () => {
        if (coverInput) coverInput.value = '';
        coverPicker.innerHTML = '<span class="empty-upload" data-cover-empty>＋<small>選擇封面圖片</small></span>';
        removeCover.classList.add('hidden');
    });

    const supportInput = document.querySelector('#supportImagesInput');
    const supportLarge = document.querySelector('[data-support-large]');
    const supportStrip = document.querySelector('[data-support-strip]');
    const setLargeSupport = (src) => {
        if (supportLarge) supportLarge.innerHTML = `<img src="${src}" alt="補充照片預覽" />`;
    };
    supportInput?.addEventListener('change', () => {
        const files = [...(supportInput.files ?? [])].filter(file => file.type.startsWith('image/')).slice(0, 8);
        supportStrip.innerHTML = '';
        if (files.length === 0) {
            supportLarge.innerHTML = '<span>尚未選擇補充照片</span>';
            return;
        }
        files.forEach((file, index) => {
            const src = URL.createObjectURL(file);
            const thumb = document.createElement('button');
            thumb.type = 'button';
            thumb.className = `support-thumb${index === 0 ? ' active' : ''}`;
            thumb.innerHTML = `<img src="${src}" alt="補充照片 ${index + 1}" />`;
            thumb.addEventListener('click', () => {
                supportStrip.querySelectorAll('.support-thumb').forEach(item => item.classList.remove('active'));
                thumb.classList.add('active');
                setLargeSupport(src);
            });
            supportStrip.appendChild(thumb);
            if (index === 0) setLargeSupport(src);
        });
        showToast(`已加入 ${files.length} 張補充照片預覽。`);
    });

    supportStrip?.addEventListener('click', event => {
        const thumb = event.target.closest('.support-thumb');
        const img = thumb?.querySelector('img');
        if (!thumb || !img) return;
        supportStrip.querySelectorAll('.support-thumb').forEach(item => item.classList.remove('active'));
        thumb.classList.add('active');
        setLargeSupport(img.src);
    });

    document.querySelector('.editor-link-back')?.addEventListener('click', event => {
        const fallback = event.currentTarget.dataset.backFallback || '/Explore';
        if (window.history.length > 1) window.history.back();
        else window.location.href = fallback;
    });
})();
