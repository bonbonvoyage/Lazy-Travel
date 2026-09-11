let pendingFavoriteUndo = null;
let groupsToastTimer = 0;

const showGroupsToast = (message, undo) => {
    const toast = document.querySelector('[data-groups-toast]');
    if (!toast) return;
    const messageNode = toast.querySelector('[data-toast-message]');
    const undoButton = toast.querySelector('[data-toast-undo]');
    if (messageNode) messageNode.textContent = message;
    undoButton.hidden = typeof undo !== 'function';
    pendingFavoriteUndo = undo;
    toast.hidden = false;
    window.clearTimeout(groupsToastTimer);
    groupsToastTimer = window.setTimeout(() => {
        toast.hidden = true;
        pendingFavoriteUndo = null;
    }, 3000);
};

const setFavoriteButtonState = (button, active, activeLabel = '取消收藏', inactiveLabel = '收藏') => {
    button.setAttribute('aria-pressed', String(active));
    button.setAttribute('aria-label', active ? activeLabel : inactiveLabel);
};

const setStatState = (node, active) => {
    if (!node) return;
    node.classList.toggle('is-active', active);
    if (node.matches('button')) node.setAttribute('aria-pressed', String(active));
};

const requestToggle = async (form) => {
    const response = await fetch(form.action, {
        method: 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        redirect: 'manual'
    });
    if (!response.ok && response.type !== 'opaqueredirect') throw new Error('toggle failed');
    return response.headers.get('content-type')?.includes('application/json') ? response.json() : null;
};

document.querySelector('[data-groups-toast] [data-toast-undo]')?.addEventListener('click', async () => {
    if (!pendingFavoriteUndo) return;
    window.clearTimeout(groupsToastTimer);
    const undo = pendingFavoriteUndo;
    pendingFavoriteUndo = null;
    await undo();
    const toast = document.querySelector('[data-groups-toast]');
    if (toast) toast.hidden = true;
});

document.querySelectorAll('.favorite-button[data-group-id]').forEach((button) => {
    const groupId = Number(button.dataset.groupId);
    const stat = document.querySelector('[data-group-favorite-stat][data-group-id="' + groupId + '"]');
    const countNode = stat?.querySelector('[data-group-favorite-count]');
    button.addEventListener('click', async () => {
        const previous = button.getAttribute('aria-pressed') === 'true';
        const previousCount = Number(countNode?.textContent ?? '0');
        button.disabled = true;
        try {
            const response = await fetch('/TravelGroups/ToggleFavorite/' + groupId, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                redirect: 'manual'
            });
            if (!response.ok && response.type !== 'opaqueredirect') throw new Error('favorite failed');
            const result = await response.json();
            const next = result.active === true;
            setFavoriteButtonState(button, next, '取消收藏', '收藏揪團');
            setStatState(stat, next);
            if (countNode) countNode.textContent = String(result.count ?? Math.max(0, previousCount + (next ? 1 : -1)));
            showGroupsToast(next ? '已加入收藏' : '已取消收藏', async () => {
                const undoResponse = await fetch('/TravelGroups/ToggleFavorite/' + groupId, {
                    method: 'POST',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    redirect: 'manual'
                });
                if (!undoResponse.ok && undoResponse.type !== 'opaqueredirect') throw new Error('favorite undo failed');
                const undoResult = await undoResponse.json();
                setFavoriteButtonState(button, undoResult.active === true, '取消收藏', '收藏揪團');
                setStatState(stat, undoResult.active === true);
                if (countNode) countNode.textContent = String(undoResult.count ?? previousCount);
            });
        } catch {
            setFavoriteButtonState(button, previous, '取消收藏', '收藏揪團');
            setStatState(stat, previous);
            if (countNode) countNode.textContent = String(previousCount);
            showGroupsToast('收藏更新失敗，請再試一次');
        } finally {
            button.disabled = false;
        }
    });
});

document.querySelectorAll('.favorite-article-form').forEach((form) => {
    const button = form.querySelector('[data-article-favorite]');
    if (!button) return;
    const card = form.closest('.group-card');
    const countNode = card?.querySelector('[data-favorite-count]');
    const statNode = card?.querySelector('[data-stat-favorite]');
    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const previous = button.getAttribute('aria-pressed') === 'true';
        const previousCount = Number(countNode?.textContent ?? '0');
        const next = !previous;
        button.disabled = true;
        try {
            await requestToggle(form);
            setFavoriteButtonState(button, next, '取消收藏', '收藏文章');
            setStatState(statNode, next);
            if (countNode) countNode.textContent = String(Math.max(0, previousCount + (next ? 1 : -1)));
            showGroupsToast(next ? '已加入收藏' : '已取消收藏', async () => {
                await requestToggle(form);
                setFavoriteButtonState(button, previous, '取消收藏', '收藏文章');
                setStatState(statNode, previous);
                if (countNode) countNode.textContent = String(previousCount);
            });
        } catch {
            showGroupsToast('收藏更新失敗，請再試一次');
        } finally {
            button.disabled = false;
        }
    });
});

document.querySelectorAll('.like-article-form').forEach((form) => {
    const button = form.querySelector('[data-article-like]');
    const countNode = form.querySelector('[data-like-count]');
    if (!button || !countNode) return;
    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const previous = button.getAttribute('aria-pressed') === 'true';
        const previousCount = Number(countNode.textContent || '0');
        const next = !previous;
        button.disabled = true;
        try {
            await requestToggle(form);
            setStatState(button, next);
            countNode.textContent = String(Math.max(0, previousCount + (next ? 1 : -1)));
            showGroupsToast(next ? '已按讚' : '已取消按讚', async () => {
                await requestToggle(form);
                setStatState(button, previous);
                countNode.textContent = String(previousCount);
            });
        } catch {
            showGroupsToast('按讚更新失敗，請再試一次');
        } finally {
            button.disabled = false;
        }
    });
});
const countryInput = document.getElementById('groupCountry');
const countryOptions = document.getElementById('groupCountryOptions');

if (countryInput && countryOptions) {
    const wrapper = countryInput.closest('.filter-country');
    const toggle = wrapper.querySelector('.country-toggle');
    const error = document.getElementById('countryError');
    const options = Array.from(countryOptions.querySelectorAll('[role="option"]'));
    const empty = countryOptions.querySelector('.country-empty');
    let active = -1;
    let visible = [];
    options.forEach((option, index) => { option.id = 'country-option-' + index; });

    function setActive(index) {
        active = index;
        options.forEach(option => option.classList.remove('is-active'));
        countryInput.removeAttribute('aria-activedescendant');
        if (visible[index]) {
            visible[index].classList.add('is-active');
            countryInput.setAttribute('aria-activedescendant', visible[index].id);
            visible[index].scrollIntoView({ block: 'nearest' });
        }
    }
    function close() {
        countryOptions.hidden = true;
        countryInput.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-label', '展開國家選單');
        setActive(-1);
    }
    function open(showAll = false) {
        const keyword = showAll ? '' : countryInput.value.trim().toLocaleLowerCase();
        options.forEach(option => {
            option.hidden = !!keyword && !option.dataset.value.toLocaleLowerCase().includes(keyword);
            option.setAttribute('aria-selected', String(option.dataset.value === countryInput.value.trim()));
        });
        visible = options.filter(option => !option.hidden);
        empty.hidden = visible.length > 0;
        countryOptions.hidden = false;
        countryInput.setAttribute('aria-expanded', 'true');
        toggle.setAttribute('aria-label', '收合國家選單');
        setActive(-1);
    }
    function clearError() {
        error.hidden = true;
        countryInput.removeAttribute('aria-invalid');
    }
    function choose(option) {
        countryInput.value = option.dataset.value;
        clearError();
        close();
    }
    countryInput.addEventListener('focus', () => open(true));
    countryInput.addEventListener('click', () => { if (countryOptions.hidden) open(true); });
    countryInput.addEventListener('input', () => { clearError(); open(); });
    toggle.addEventListener('mousedown', event => event.preventDefault());
    toggle.addEventListener('click', () => {
        const wasOpen = !countryOptions.hidden;
        countryInput.focus();
        if (wasOpen) close(); else open(true);
    });
    countryOptions.addEventListener('mousedown', event => event.preventDefault());
    countryOptions.addEventListener('click', event => {
        const option = event.target.closest('[role="option"]');
        if (option) choose(option);
    });
    countryInput.addEventListener('keydown', event => {
        if (event.isComposing) return;
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
            if (countryOptions.hidden) open(true);
            if (visible.length) setActive(event.key === 'ArrowDown'
                ? (active + 1) % visible.length
                : (active <= 0 ? visible.length - 1 : active - 1));
        } else if (event.key === 'Enter' && !countryOptions.hidden && active >= 0) {
            event.preventDefault();
            choose(visible[active]);
        } else if (event.key === 'Escape') {
            event.preventDefault();
            close();
        } else if (event.key === 'Tab') close();
    });
    wrapper.addEventListener('focusout', event => {
        if (!wrapper.contains(event.relatedTarget)) close();
    });
    document.addEventListener('click', event => {
        if (!wrapper.contains(event.target)) close();
    });
    countryInput.form.addEventListener('submit', event => {
        countryInput.value = countryInput.value.trim();
        if (!options.some(option => option.dataset.value === countryInput.value)) {
            event.preventDefault();
            countryInput.focus();
            open();
            error.hidden = false;
            countryInput.setAttribute('aria-invalid', 'true');
        }
    });
}

const dateRangeInput = document.getElementById('groupDateRange');
const startDateInput = document.getElementById('groupStartDate');
const endDateInput = document.getElementById('groupEndDate');

if (dateRangeInput && startDateInput && endDateInput && window.flatpickr) {
    const initialDates = [startDateInput.value, endDateInput.value].filter(Boolean);

    flatpickr(dateRangeInput, {
        mode: 'range',
        dateFormat: 'Y-m-d',
        altInput: true,
        altFormat: 'Y/m/d',
        defaultDate: initialDates,
        allowInput: false,
        onChange(selectedDates) {
            const [start, end] = selectedDates;
            startDateInput.value = start ? formatDate(start) : '';
            endDateInput.value = end ? formatDate(end) : '';
        },
        onClose(selectedDates) {
            if (selectedDates.length === 1) {
                const onlyDate = formatDate(selectedDates[0]);
                startDateInput.value = onlyDate;
                endDateInput.value = onlyDate;
            }
        }
    });
}

function formatDate(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}




