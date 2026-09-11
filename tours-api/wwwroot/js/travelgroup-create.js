const stepPanels = Array.from(document.querySelectorAll('.step-panel'));
const stepButtons = Array.from(document.querySelectorAll('.step-pill'));
const nextButtons = Array.from(document.querySelectorAll('[data-next]'));
const prevButtons = Array.from(document.querySelectorAll('[data-prev]'));
const cancelButton = document.querySelector('[data-cancel]');
const saveDraftButton = document.querySelector('[data-save-draft]');
const createForm = document.getElementById('groupCreateForm');
const subtitle = document.getElementById('stepSubtitle');
const draftStorageKey = 'lazytravel.travelGroupCreateDraft';
const draftEndpoint = createForm?.dataset.draftUrl;
const saveDraftEndpoint = createForm?.dataset.saveDraftUrl;
const submitEndpoint = createForm?.dataset.submitUrl;
const queryParams = new URLSearchParams(window.location.search);
const editGroupId = Number(queryParams.get('editId') || 0);
const editViewerMemberId = queryParams.get('viewerMemberId');
const isEditMode = Number.isFinite(editGroupId) && editGroupId > 0;
const editDraftEndpoint = isEditMode ? `/TravelGroups/EditDraft/${editGroupId}${editViewerMemberId ? `?viewerMemberId=${encodeURIComponent(editViewerMemberId)}` : ''}` : '';
const updateEndpoint = isEditMode ? `/TravelGroups/Update/${editGroupId}${editViewerMemberId ? `?viewerMemberId=${encodeURIComponent(editViewerMemberId)}` : ''}` : '';
let currentStep = 1;
let dateRangePicker = null;

const subtitles = {
    1: '先把旅程輪廓整理好，下一步再設定旅伴條件。',
    2: '設定你期待的旅伴條件，讓同行的人更合拍。',
    3: '補上行程亮點與預算，讓旅伴更容易想這趟旅行。',
};

const countriesByRegion = {
    亞洲: ['台灣', '日本', '韓國', '泰國', '越南', '新加坡', '馬來西亞', '印尼', '菲律賓', '香港', '澳門'],
    歐洲: ['英國', '法國', '德國', '義大利', '西班牙', '葡萄牙', '荷蘭', '瑞士', '奧地利', '希臘', '冰島'],
    北美洲: ['美國', '加拿大', '墨西哥'],
    南美洲: ['巴西', '阿根廷', '智利', '秘魯'],
    非洲: ['埃及', '摩洛哥', '南非', '肯亞'],
    大洋洲: ['澳洲', '紐西蘭', '斐濟'],
};

const countryRegionDefaults = {
    台灣: ['台北', '台中', '台南', '花蓮', '澎湖'],
    日本: ['東京', '大阪', '京都', '奈良', '北海道', '沖繩'],
    韓國: ['首爾', '釜山', '濟州島'],
    泰國: ['曼谷', '清邁', '普吉島'],
    法國: ['巴黎', '尼斯', '里昂'],
    義大利: ['羅馬', '佛羅倫斯', '威尼斯'],
    美國: ['紐約', '洛杉磯', '舊金山', '夏威夷'],
};

const allCountries = Object.values(countriesByRegion).flat();
if (isEditMode) {
    document.title = document.title.replace('建立一趟新的旅行', '編輯揪團');
    document.querySelector('.create-header h1')?.replaceChildren(document.createTextNode('編輯揪團'));
    const cancel = document.querySelector('[data-cancel]');
    if (cancel) cancel.textContent = '返回詳情';
    subtitles[1] = '調整旅程輪廓後送出，就會更新原本的揪團房間。';
    subtitles[2] = '確認旅伴條件是否仍符合這趟行程。';
    subtitles[3] = '補上行程亮點與預算，送出後會更新詳細頁資料。';
}

function setStep(step) {
    currentStep = Math.min(3, Math.max(1, step));

    stepPanels.forEach((panel) => {
        panel.classList.toggle('active', Number(panel.dataset.step) === currentStep);
    });

    stepButtons.forEach((button) => {
        const stepNumber = Number(button.dataset.stepTarget);
        button.classList.toggle('active', stepNumber === currentStep);
        button.classList.toggle('done', stepNumber < currentStep);
        const number = button.querySelector('span');
        if (number) {
            number.textContent = stepNumber < currentStep ? '✓' : String(stepNumber);
        }
    });

    prevButtons.forEach((button) => {
        button.hidden = currentStep === 1;
    });

    if (cancelButton) {
        cancelButton.hidden = currentStep !== 1;
    }

    nextButtons.forEach((button) => {
        const isLast = currentStep === 3;
        button.innerHTML = isLast ? '送出 <span>→</span>' : '下一步 <span>→</span>';
    });

    if (subtitle) {
        subtitle.textContent = subtitles[currentStep];
    }
}

function validateCurrentStep() {
    const panel = stepPanels.find((item) => Number(item.dataset.step) === currentStep);
    if (!panel) return true;

    let valid = true;
    panel.querySelectorAll('[data-required]').forEach((input) => {
        const field = input.closest('.field');
        const message = input.dataset.required || '請填寫此欄位';
        const empty = !input.value.trim();
        field?.classList.toggle('has-error', empty);
        const error = field?.querySelector('.field-error');
        if (error) {
            error.textContent = empty ? message : '';
        }
        if (empty) valid = false;
    });

    if (currentStep === 1) {
        const chipEditor = document.querySelector('.chip-editor');
        const chipError = document.querySelector('[data-chip-error]');
        const emptyRegions = !chipEditor || chipEditor.querySelectorAll('.chip').length === 0;
        if (chipError) {
            chipError.textContent = emptyRegions ? '請至少新增一個城市或地區' : '';
        }
        if (emptyRegions) valid = false;
    }

    return valid;
}

stepButtons.forEach((button) => {
    button.addEventListener('click', () => {
        const target = Number(button.dataset.stepTarget);
        if (target <= currentStep || validateCurrentStep()) {
            setStep(target);
        }
    });
});

prevButtons.forEach((button) => {
    button.addEventListener('click', () => setStep(currentStep - 1));
});

nextButtons.forEach((button) => {
    button.addEventListener('click', async () => {
        if (!validateCurrentStep()) return;

        if (currentStep < 3) {
            setStep(currentStep + 1);
            return;
        }

        await submitTravelGroup(button);
    });
});

saveDraftButton?.addEventListener('click', async () => {
    try {
        const draft = collectDraft();
        try {
            localStorage.setItem(draftStorageKey, JSON.stringify(draft));
        } catch {
            // Database draft saving still works when browser storage is unavailable.
        }
        await saveDraftToDatabase(draft);
        showDraftSaved('已儲存');
    } catch {
        alert('草稿暫時無法儲存，請稍後再試。');
    }
});

document.querySelectorAll('[data-required]').forEach((input) => {
    input.addEventListener('input', () => {
        const field = input.closest('.field');
        field?.classList.remove('has-error');
        const error = field?.querySelector('.field-error');
        if (error) error.textContent = '';
    });
});

document.querySelectorAll('.option').forEach((button) => {
    button.addEventListener('click', () => {
        button.classList.toggle('active');
    });
});

const countryInput = document.getElementById('countryInput');
const countrySuggestions = document.getElementById('countrySuggestions');
if (countryInput && countrySuggestions) {
    renderCountrySuggestions('');

    countryInput.addEventListener('focus', () => renderCountrySuggestions(countryInput.value));
    countryInput.addEventListener('input', () => {
        renderCountrySuggestions(countryInput.value);
        const country = countryInput.value.trim();
        syncDefaultRegions(country, allCountries.includes(country));
    });

    document.addEventListener('click', (event) => {
        if (!event.target.closest('.country-field')) {
            countrySuggestions.classList.remove('open');
        }
    });
}

function renderCountrySuggestions(keyword) {
    const text = keyword.trim().toLowerCase();
    const matches = allCountries.filter((country) => country.toLowerCase().includes(text)).slice(0, 12);
    countrySuggestions.replaceChildren();

    matches.forEach((country) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = country;
        button.addEventListener('click', () => {
            countryInput.value = country;
            countrySuggestions.classList.remove('open');
            syncDefaultRegions(country, true);
            clearFieldError(countryInput);
        });
        countrySuggestions.appendChild(button);
    });

    countrySuggestions.classList.toggle('open', matches.length > 0);
}

function syncDefaultRegions(country, force = false) {
    const defaults = countryRegionDefaults[country];
    if (!defaults || (!force && document.querySelectorAll('.chip-editor .chip').length > 0)) return;
    document.querySelectorAll('.chip-editor .chip').forEach((chip) => chip.remove());
    defaults.slice(0, 3).forEach(addRegionChip);
    clearChipError();
}

function clearFieldError(input) {
    const field = input.closest('.field');
    field?.classList.remove('has-error');
    const error = field?.querySelector('.field-error');
    if (error) error.textContent = '';
}

const regionInput = document.getElementById('regionInput');
const addRegionButton = document.getElementById('addRegionBtn');
addRegionButton?.addEventListener('click', () => {
    const value = regionInput?.value.trim();
    if (!value) {
        const chipError = document.querySelector('[data-chip-error]');
        if (chipError) chipError.textContent = '請輸入要新增的地區';
        return;
    }
    addRegionChip(value);
    regionInput.value = '';
    clearChipError();
});

regionInput?.addEventListener('keydown', (event) => {
    if (event.key === 'Enter') {
        event.preventDefault();
        addRegionButton?.click();
    }
});

function addRegionChip(text) {
    const chipEditor = document.querySelector('.chip-editor');
    const chipInput = document.getElementById('regionInput');
    if (!chipEditor || !chipInput) return;
    const exists = Array.from(chipEditor.querySelectorAll('.chip')).some((chip) => chip.firstChild?.textContent?.trim() === text);
    if (exists) return;

    const chip = document.createElement('span');
    chip.className = 'chip';
    chip.append(document.createTextNode(text + ' '));
    const button = document.createElement('button');
    button.type = 'button';
    button.textContent = '×';
    button.addEventListener('click', () => chip.remove());
    chip.appendChild(button);
    chipEditor.insertBefore(chip, chipInput);
}

function clearChipError() {
    const chipError = document.querySelector('[data-chip-error]');
    if (chipError) chipError.textContent = '';
}

document.querySelectorAll('.chip button').forEach((button) => {
    button.addEventListener('click', () => {
        button.closest('.chip')?.remove();
    });
});

const coverUpload = document.getElementById('coverUpload');
const coverFileInput = document.getElementById('coverFileInput');
coverUpload?.addEventListener('click', () => coverFileInput?.click());
coverFileInput?.addEventListener('change', () => {
    const file = coverFileInput.files?.[0];
    if (!file) return;
    const url = URL.createObjectURL(file);
    coverUpload.style.backgroundImage = `linear-gradient(rgba(255, 255, 255, .08), rgba(31, 41, 55, .28)), url("${url}")`;
    const fileName = document.getElementById('coverFileName');
    if (fileName) fileName.textContent = file.name;
});

const supportImageList = document.getElementById('supportImageList');
function createSupportUploadButton(fileName = '') {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = fileName ? 'support-upload has-image' : 'support-upload support-upload-add';
    button.textContent = fileName || '＋';
    if (fileName) button.dataset.fileName = fileName;
    button.setAttribute('aria-label', fileName ? `補充圖片：${fileName}` : '新增補充圖片');
    return button;
}

function ensureSupportAddButton() {
    if (!supportImageList) return;
    supportImageList.querySelectorAll('.support-upload-add').forEach((button, index) => {
        if (index > 0) button.remove();
    });
    if (!supportImageList.querySelector('.support-upload-add')) {
        supportImageList.appendChild(createSupportUploadButton());
    }
}

supportImageList?.addEventListener('click', (event) => {
    const button = event.target.closest('.support-upload');
    if (!button) return;
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    input.addEventListener('change', () => {
        const file = input.files?.[0];
        if (!file) return;
        button.classList.remove('support-upload-add');
        button.classList.add('has-image');
        button.style.backgroundImage = `url("${URL.createObjectURL(file)}")`;
        button.style.backgroundSize = 'cover';
        button.style.backgroundPosition = 'center';
        button.textContent = '';
        button.dataset.fileName = file.name;
        button.setAttribute('aria-label', `補充圖片：${file.name}`);
        ensureSupportAddButton();
    });
    input.click();
});

document.querySelectorAll('.number-stepper').forEach((stepper) => {
    const input = stepper.querySelector('input');
    stepper.querySelectorAll('button').forEach((button) => {
        button.addEventListener('click', () => {
            const delta = Number(button.dataset.count || 0);
            const min = Number(input.min || 1);
            const max = Number(input.max || 30);
            const next = Math.min(max, Math.max(min, Number(input.value || min) + delta));
            input.value = String(next);
        });
    });
});

const ageMin = document.getElementById('ageMin');
const ageMax = document.getElementById('ageMax');
const ageRangePreview = document.getElementById('ageRangePreview');
const ageRange = document.getElementById('ageRange');
function updateAgeRange(changed) {
    if (!ageMin || !ageMax || !ageRangePreview) return;
    let min = Number(ageMin.value);
    let max = Number(ageMax.value);
    if (min > max - 1) {
        if (changed === ageMin) {
            min = max - 1;
            ageMin.value = String(min);
        } else {
            max = min + 1;
            ageMax.value = String(max);
        }
    }
    ageRangePreview.textContent = `${min} - ${max} 歲`;
    if (ageRange) {
        const minLimit = Number(ageMin.min || 18);
        const maxLimit = Number(ageMin.max || 65);
        const span = Math.max(1, maxLimit - minLimit);
        const left = ((min - minLimit) / span) * 100;
        const right = ((max - minLimit) / span) * 100;
        ageRange.style.setProperty('--range-left', `${left}%`);
        ageRange.style.setProperty('--range-right', `${right}%`);
    }
}
ageMin?.addEventListener('input', () => updateAgeRange(ageMin));
ageMax?.addEventListener('input', () => updateAgeRange(ageMax));
updateAgeRange();

const dayList = document.getElementById('dayList');
document.getElementById('addDayBtn')?.addEventListener('click', () => {
    const dayNumber = (dayList?.querySelectorAll('.day-card').length || 0) + 1;
    dayList?.appendChild(createDayCard(dayNumber));
});

dayList?.addEventListener('click', (event) => {
    const button = event.target.closest('.delete-day');
    if (!button) return;
    button.closest('.day-card')?.remove();
    renumberDays();
});

function createDayCard(dayNumber) {
    const article = document.createElement('article');
    article.className = 'day-card';
    article.innerHTML = `
        <div class="day-badge"><small>DAY</small><strong>${dayNumber}</strong></div>
        <div class="day-inputs">
            <label><span>☼ 早</span><input placeholder="想安排哪裡？例如：清水寺參拜、祇園散步" /></label>
            <label><span>☼ 中</span><input placeholder="午餐或午後行程，例如：二年坂、三年坂散步" /></label>
            <label><span>☾ 晚</span><input placeholder="晚餐或夜間活動，例如：拉麵小路、夜景散策" /></label>
        </div>
        <button class="delete-day" type="button" aria-label="刪除 DAY ${dayNumber}">×</button>`;
    return article;
}

function renumberDays() {
    dayList?.querySelectorAll('.day-card').forEach((card, index) => {
        const day = index + 1;
        card.querySelector('.day-badge strong').textContent = String(day);
        card.querySelector('.delete-day').setAttribute('aria-label', `刪除 DAY ${day}`);
    });
}

const budgetList = document.getElementById('budgetList');
document.getElementById('addBudgetBtn')?.addEventListener('click', () => {
    const name = prompt('請輸入新的預算種類，例如：網卡、保險、伴手禮');
    if (!name?.trim()) return;
    budgetList?.appendChild(createBudgetRow(name.trim(), true));
});

budgetList?.addEventListener('click', (event) => {
    const button = event.target.closest('.delete-budget');
    if (!button) return;
    button.closest('.budget-row')?.remove();
});

function createBudgetRow(name, removable) {
    const row = document.createElement('div');
    row.className = 'budget-row';
    row.innerHTML = `
        <span>${escapeText(name)}</span>
        <input inputmode="numeric" placeholder="金額" />
        <b>元</b>
        ${removable ? '<button class="delete-budget" type="button" aria-label="刪除預算種類">×</button>' : '<i></i>'}`;
    return row;
}

function escapeText(text) {
    return text.replace(/[&<>"']/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]));
}

function collectDraft() {
    return {
        currentStep,
        groupTitle: document.querySelector('[name="groupTitle"]')?.value || '',
        country: countryInput?.value || '',
        regions: Array.from(document.querySelectorAll('.chip-editor .chip')).map((chip) => chip.firstChild?.textContent?.trim()).filter(Boolean),
        dateRange: document.getElementById('createDateRange')?.value || '',
        description: document.querySelector('[name="description"]')?.value || '',
        coverFileName: document.getElementById('coverFileName')?.textContent || '',
        supportFileNames: Array.from(document.querySelectorAll('.support-upload.has-image')).map((button) => button.dataset.fileName || ''),
        activeOptions: Array.from(document.querySelectorAll('.option.active')).map((button) => button.textContent.trim()),
        people: Array.from(document.querySelectorAll('.number-stepper input')).map((input) => input.value),
        ageMin: ageMin?.value || '18',
        ageMax: ageMax?.value || '65',
        days: Array.from(document.querySelectorAll('#dayList .day-card')).map((card) =>
            Array.from(card.querySelectorAll('.day-inputs input')).map((input) => input.value)
        ),
        budgets: Array.from(document.querySelectorAll('#budgetList .budget-row')).map((row) => ({
            name: row.querySelector('span')?.textContent?.trim() || '',
            amount: row.querySelector('input')?.value || '',
            removable: Boolean(row.querySelector('.delete-budget')),
        })),
    };
}

function restoreDraft(sourceDraft) {
    let draft;
    if (sourceDraft) {
        draft = sourceDraft;
    } else {
        let raw = null;
        try {
            raw = localStorage.getItem(draftStorageKey);
        } catch {
            return;
        }
        if (!raw) return;

        try {
            draft = JSON.parse(raw);
        } catch {
            return;
        }
    }

    const title = document.querySelector('[name="groupTitle"]');
    if (title) title.value = draft.groupTitle || '';
    if (countryInput) countryInput.value = draft.country || '';
    const dateRange = document.getElementById('createDateRange');
    if (dateRange) dateRange.value = draft.dateRange || '';
    if (dateRangePicker && draft.dateRange) {
        const dates = draft.dateRange.includes(' to ') ? draft.dateRange.split(' to ') : draft.dateRange;
        dateRangePicker.setDate(dates, false, 'Y-m-d');
    }
    const description = document.querySelector('[name="description"]');
    if (description) description.value = draft.description || '';
    updateIntroCount();
    const coverFileName = document.getElementById('coverFileName');
    if (coverFileName) coverFileName.textContent = draft.coverFileName || '';

    if (Array.isArray(draft.regions)) {
        document.querySelectorAll('.chip-editor .chip').forEach((chip) => chip.remove());
        draft.regions.forEach(addRegionChip);
    }

    if (Array.isArray(draft.activeOptions)) {
        document.querySelectorAll('.option').forEach((button) => {
            button.classList.toggle('active', draft.activeOptions.includes(button.textContent.trim()));
        });
    }

    if (Array.isArray(draft.people)) {
        document.querySelectorAll('.number-stepper input').forEach((input, index) => {
            input.value = draft.people[index] || input.value;
        });
    }

    if (ageMin && draft.ageMin) ageMin.value = draft.ageMin;
    if (ageMax && draft.ageMax) ageMax.value = draft.ageMax;
    updateAgeRange();

    if (Array.isArray(draft.days) && draft.days.length > 0 && dayList) {
        dayList.replaceChildren();
        draft.days.forEach((values, index) => {
            const card = createDayCard(index + 1);
            card.querySelectorAll('.day-inputs input').forEach((input, valueIndex) => {
                input.value = values[valueIndex] || '';
            });
            dayList.appendChild(card);
        });
    }

    if (Array.isArray(draft.budgets) && draft.budgets.length > 0 && budgetList) {
        budgetList.replaceChildren();
        draft.budgets.forEach((item) => {
            const row = createBudgetRow(item.name || '其他', item.removable);
            row.querySelector('input').value = item.amount || '';
            budgetList.appendChild(row);
        });
    }

    if (Array.isArray(draft.supportFileNames) && supportImageList) {
        supportImageList.replaceChildren();
        draft.supportFileNames.filter(Boolean).forEach((fileName) => {
            supportImageList.appendChild(createSupportUploadButton(fileName));
        });
        ensureSupportAddButton();
    }

    setStep(Number(draft.currentStep || 1));
}

async function saveDraftToDatabase(draft) {
    if (!saveDraftEndpoint) return;

    const response = await fetch(saveDraftEndpoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken(),
        },
        body: JSON.stringify(draft),
    });

    if (!response.ok) {
        throw new Error('Save draft failed');
    }

    const result = await response.json();
    if (result?.draftId) {
        localStorage.setItem(`${draftStorageKey}.id`, String(result.draftId));
    }
}

async function submitTravelGroup(button) {
    const endpoint = isEditMode ? updateEndpoint : submitEndpoint;
    if (!endpoint) return;

    const original = button.innerHTML;
    button.disabled = true;
    button.innerHTML = '送出中...';

    try {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken(),
            },
            body: JSON.stringify(collectDraft()),
        });

        if (!response.ok) {
            const error = await response.json().catch(() => null);
            alert(error?.message || '送出失敗，請確認必填欄位後再試一次。');
            return;
        }

        const result = await response.json();
        try {
            if (!isEditMode) {
                localStorage.removeItem(draftStorageKey);
                localStorage.removeItem(`${draftStorageKey}.id`);
            }
        } catch {
            // The database record has already been published.
        }
        window.location.href = result?.redirectUrl || '/TravelGroups';
    } catch {
        alert('送出失敗，請稍後再試。');
    } finally {
        button.disabled = false;
        button.innerHTML = original;
    }
}

function getEditReturnUrl() {
    const url = new URL(`/TravelGroups/Details/${editGroupId}`, window.location.origin);
    if (editViewerMemberId) url.searchParams.set('viewerMemberId', editViewerMemberId);
    return url.toString();
}

function openCancelEditModal() {
    document.getElementById('tg-edit-cancel-modal')?.remove();
    document.body.insertAdjacentHTML('beforeend', `
        <div class="tg-edit-cancel-backdrop" id="tg-edit-cancel-modal" role="dialog" aria-modal="true" aria-label="取消編輯確認">
            <section class="tg-edit-cancel-card">
                <button class="tg-edit-cancel-x" type="button" aria-label="關閉">×</button>
                <span class="tg-edit-cancel-mark">?</span>
                <h2>要取消編輯嗎？</h2>
                <p>尚未送出的修改不會儲存，會返回揪團詳細頁。</p>
                <div class="tg-edit-cancel-actions">
                    <button class="glass-btn" type="button" data-edit-cancel-close>繼續編輯</button>
                    <button class="primary-btn" type="button" data-edit-cancel-confirm>取消編輯</button>
                </div>
            </section>
        </div>`);
    const modal = document.getElementById('tg-edit-cancel-modal');
    const close = () => modal?.remove();
    modal?.querySelector('.tg-edit-cancel-x')?.addEventListener('click', close);
    modal?.querySelector('[data-edit-cancel-close]')?.addEventListener('click', close);
    modal?.querySelector('[data-edit-cancel-confirm]')?.addEventListener('click', () => window.location.assign(getEditReturnUrl()));
    modal?.addEventListener('click', (event) => {
        if (event.target === modal) close();
    });
}

cancelButton?.addEventListener('click', (event) => {
    if (!isEditMode) return;
    event.preventDefault();
    openCancelEditModal();
});
async function loadDatabaseDraft() {
    const endpoint = isEditMode ? editDraftEndpoint : draftEndpoint;
    if (!endpoint) return;

    try {
        const response = await fetch(endpoint, { headers: { Accept: 'application/json' } });
        if (!response.ok) return;
        if (response.status === 204) return;
        const draft = await response.json();
        if (!draft) return;
        if (!isEditMode) {
            localStorage.setItem(draftStorageKey, JSON.stringify(draft));
            if (draft.draftId) {
                localStorage.setItem(`${draftStorageKey}.id`, String(draft.draftId));
            }
        }
        restoreDraft(draft);
    } catch {
        // Keep the page usable even when the draft endpoint is unavailable.
    }
}

function getAntiForgeryToken() {
    return createForm?.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

function showDraftSaved(message) {
    if (!saveDraftButton) return;
    const original = saveDraftButton.innerHTML;
    saveDraftButton.innerHTML = `<span class="btn-icon">✓</span> ${message}`;
    saveDraftButton.disabled = true;
    window.setTimeout(() => {
        saveDraftButton.innerHTML = original;
        saveDraftButton.disabled = false;
    }, 1300);
}

const intro = document.querySelector('textarea[maxlength="200"]');
const introCount = document.getElementById('introCount');
function updateIntroCount() {
    if (intro && introCount) {
        introCount.textContent = intro.value.length;
    }
}

if (intro && introCount) {
    intro.addEventListener('input', updateIntroCount);
    updateIntroCount();
}

if (window.flatpickr) {
    dateRangePicker = flatpickr('#createDateRange', {
        mode: 'range',
        dateFormat: 'Y-m-d',
        altInput: true,
        altFormat: 'Y/m/d',
        allowInput: false,
        onChange() {
            const input = document.getElementById('createDateRange');
            if (input) clearFieldError(input);
        },
    });
}

setStep(1);
if (!isEditMode) restoreDraft();
loadDatabaseDraft();
