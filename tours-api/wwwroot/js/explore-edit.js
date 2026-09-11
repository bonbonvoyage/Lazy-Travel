(() => {
    const shell = document.querySelector('.editor-shell');
    if (!shell) return;

    const postId = shell.dataset.postId;
    const viewerMemberId = shell.dataset.viewerMemberId || '';
    const token = document.querySelector('#article-antiforgery input[name="__RequestVerificationToken"]')?.value ?? '';
    const dayList = document.querySelector('#dayList');
    const titleInput = document.querySelector('#articleTitle');
    const introInput = document.querySelector('#articleIntro');
    const highlightInput = document.querySelector('#articleHighlight');
    const countryInput = document.querySelector('#articleCountry');
    const regionInput = document.querySelector('#articleRegion');
    const startDateInput = document.querySelector('#articleStartDate');
    const endDateInput = document.querySelector('#articleEndDate');
    const dateRangeInput = document.querySelector('#articleDateRange');
    const peopleInput = document.querySelector('#articlePeople');
    const saveButton = document.querySelector('[data-action="submit"]');
    const addDayButton = document.querySelector('.add-day');
    const selectedSupportFiles = [];
    let hasUnsavedArticleChanges = false;
    let allowArticleEditorLeave = false;
    let articleHistoryGuardReady = false;

    const markArticleDirty = () => { hasUnsavedArticleChanges = true; };
    const canSkipArticleLeaveGuard = () => allowArticleEditorLeave || !hasUnsavedArticleChanges;
    const navigateArticleEditor = destination => {
        allowArticleEditorLeave = true;
        if (typeof destination === 'function') destination();
        else window.location.href = destination;
    };

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
                <textarea class="day-morning" rows="2" placeholder="早上行程，例如：清水寺參拜、二年坂散步"></textarea>
                <textarea class="day-afternoon" rows="2" placeholder="下午行程，例如：嵐山竹林、渡月橋"></textarea>
                <textarea class="day-evening" rows="2" placeholder="晚上行程，例如：祇園晚餐、鴨川散步"></textarea>
                <label class="note-row"><span>心得筆記</span><textarea class="day-note-input" rows="1" placeholder="寫下今天的旅行心得..."></textarea></label>
            </div>
            <button class="remove-day" type="button" aria-label="移除這一天">×</button>`;
        return article;
    };

    const collectPayload = () => ({
        title: titleInput?.value?.trim() ?? '',
        intro: introInput?.value?.trim() ?? '',
        highlight: highlightInput?.value?.trim() ?? '',
        country: countryInput?.value?.trim() ?? '',
        region: regionInput?.value?.trim() ?? '',
        startDate: startDateInput?.value ?? '',
        endDate: endDateInput?.value ?? '',
        people: peopleInput?.value ?? '',
        days: [...(dayList?.querySelectorAll('[data-day]') ?? [])].map(card => ({
            title: card.querySelector('.day-title')?.value?.trim() ?? '',
            morning: card.querySelector('.day-morning')?.value?.trim() ?? '',
            afternoon: card.querySelector('.day-afternoon')?.value?.trim() ?? '',
            evening: card.querySelector('.day-evening')?.value?.trim() ?? '',
            note: card.querySelector('.day-note-input')?.value?.trim() ?? ''
        }))
    });

    const setBusy = (button, busy) => {
        if (!button) return;
        button.disabled = busy;
        button.dataset.originalText ??= button.textContent;
        button.textContent = busy ? '處理中...' : button.dataset.originalText;
    };

    const formatDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };

    const updateDateRangeText = () => {
        if (!dateRangeInput) return;
        const start = startDateInput?.value || '';
        const end = endDateInput?.value || '';
        dateRangeInput.value = start ? `${start} 至 ${end || start}` : '';
    };

    if (dateRangeInput && startDateInput && endDateInput && window.flatpickr) {
        const initialDates = [startDateInput.value, endDateInput.value].filter(Boolean);
        flatpickr(dateRangeInput, {
            mode: 'range',
            dateFormat: 'Y-m-d',
            defaultDate: initialDates,
            disableMobile: true,
            onChange(selectedDates) {
                const [start, end] = selectedDates;
                startDateInput.value = start ? formatDate(start) : '';
                endDateInput.value = end ? formatDate(end) : '';
                updateDateRangeText();
                markArticleDirty();
            },
            onClose(selectedDates) {
                if (selectedDates.length === 1) {
                    const onlyDate = formatDate(selectedDates[0]);
                    startDateInput.value = onlyDate;
                    endDateInput.value = onlyDate;
                    updateDateRangeText();
                }
            }
        });
        updateDateRangeText();
    }

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
            const query = viewerMemberId ? `?viewerMemberId=${encodeURIComponent(viewerMemberId)}` : '';
            const formData = new FormData();
            formData.append('payload', JSON.stringify(payload));
            const coverFile = document.querySelector('#coverImageInput')?.files?.[0];
            if (coverFile) formData.append('coverImage', coverFile);
            selectedSupportFiles.forEach(file => formData.append('supportImages', file));

            const response = await fetch(`/Explore/Submit/${postId}${query}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            });
            const data = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(data.message || '送出失敗，請稍後再試。');
            showToast(data.message || '文章已送出。');
            allowArticleEditorLeave = true;
            hasUnsavedArticleChanges = false;
            if (data.redirectUrl) window.setTimeout(() => { window.location.href = data.redirectUrl; }, 650);
        } catch (error) {
            showToast(error.message || '送出失敗，請稍後再試。', 'error');
        } finally {
            setBusy(saveButton, false);
        }
    };

    addDayButton?.addEventListener('click', () => {
        dayList?.appendChild(createDayCard());
        markArticleDirty();
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
        markArticleDirty();
        renumberDays();
    });

    saveButton?.addEventListener('click', () => send('submit'));

    shell.addEventListener('input', markArticleDirty, true);
    shell.addEventListener('change', markArticleDirty, true);

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
    const renderSupportFiles = () => {
        if (!supportStrip || !supportLarge) return;
        supportStrip.innerHTML = '';
        if (selectedSupportFiles.length === 0) {
            supportLarge.innerHTML = '<span>尚未選擇補充照片</span>';
            return;
        }

        selectedSupportFiles.forEach((file, index) => {
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
    };

    supportInput?.addEventListener('change', () => {
        const files = [...(supportInput.files ?? [])].filter(file => file.type.startsWith('image/'));
        if (files.length === 0) return;
        selectedSupportFiles.push(...files);
        supportInput.value = '';
        renderSupportFiles();
        showToast(`已累積 ${selectedSupportFiles.length} 張補充照片。`);
    });

    supportStrip?.addEventListener('click', event => {
        const thumb = event.target.closest('.support-thumb');
        const img = thumb?.querySelector('img');
        if (!thumb || !img) return;
        supportStrip.querySelectorAll('.support-thumb').forEach(item => item.classList.remove('active'));
        thumb.classList.add('active');
        setLargeSupport(img.src);
    });

    const leaveEditor = fallback => {
        navigateArticleEditor(() => {
            if (window.history.length > 1) window.history.back();
            else window.location.href = fallback;
        });
    };

    const openCancelEditModal = destination => {
        document.getElementById('article-edit-cancel-modal')?.remove();
        document.body.insertAdjacentHTML('beforeend', `
            <div class="article-edit-cancel-backdrop" id="article-edit-cancel-modal" role="dialog" aria-modal="true" aria-label="取消編輯確認">
                <div class="article-edit-cancel-card">
                    <h2>要取消編輯嗎？</h2>
                    <div class="article-edit-cancel-actions">
                        <button class="article-edit-cancel-secondary" type="button">取消</button>
                        <button class="article-edit-cancel-primary" type="button">確定</button>
                    </div>
                </div>
            </div>`);
        const modal = document.getElementById('article-edit-cancel-modal');
        const close = () => modal.remove();
        modal.querySelector('.article-edit-cancel-secondary').addEventListener('click', close);
        modal.querySelector('.article-edit-cancel-primary').addEventListener('click', () => {
            if (typeof destination === 'function') navigateArticleEditor(destination);
            else navigateArticleEditor(destination);
        });
        modal.addEventListener('click', event => { if (event.target === modal) close(); });
    };

    const confirmArticleLeave = destination => {
        if (canSkipArticleLeaveGuard()) {
            navigateArticleEditor(destination);
            return;
        }
        openCancelEditModal(destination);
    };

    document.querySelector('.editor-link-back')?.addEventListener('click', event => {
        event.preventDefault();
        const fallback = event.currentTarget.dataset.backFallback || '/Explore';
        confirmArticleLeave(() => {
            if (window.history.length > 1) window.history.back();
            else window.location.href = fallback;
        });
    });

    document.addEventListener('click', event => {
        const link = event.target.closest('a[href]');
        if (!link || link.closest('#article-edit-cancel-modal')) return;
        if (link.target && link.target !== '_self') return;
        if (link.hasAttribute('download')) return;
        const href = link.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
        const url = new URL(href, window.location.href);
        if (url.href === window.location.href) return;
        if (canSkipArticleLeaveGuard()) return;
        event.preventDefault();
        confirmArticleLeave(url.href);
    }, true);

    window.addEventListener('beforeunload', event => {
        if (canSkipArticleLeaveGuard()) return;
        event.preventDefault();
        event.returnValue = '';
    });

    if (!articleHistoryGuardReady && history.pushState) {
        articleHistoryGuardReady = true;
        history.pushState({ articleEditorGuard: true }, '', window.location.href);
        window.addEventListener('popstate', () => {
            if (canSkipArticleLeaveGuard()) {
                allowArticleEditorLeave = true;
                history.back();
                return;
            }
            history.pushState({ articleEditorGuard: true }, '', window.location.href);
            confirmArticleLeave(() => history.back());
        });
    }
})();



