(() => {
    const dialog = document.querySelector('[data-ai-match-dialog]');
    const openButton = document.querySelector('[data-ai-match-open]');
    if (!dialog || !openButton) return;

    const form = dialog.querySelector('[data-ai-match-form]');
    const prompt = dialog.querySelector('#aiTravelPrompt');
    const count = dialog.querySelector('[data-ai-match-count]');
    const error = dialog.querySelector('[data-ai-match-error]');
    const submit = dialog.querySelector('[data-ai-match-submit]');
    const criteriaRoot = dialog.querySelector('[data-ai-match-criteria]');
    const resultsRoot = dialog.querySelector('[data-ai-match-results]');
    const resultTitle = dialog.querySelector('[data-ai-match-result-title]');
    let responseData = null;

    const setStep = (step) => {
        dialog.querySelectorAll('[data-ai-match-step]').forEach((panel) => {
            const active = Number(panel.dataset.aiMatchStep) === step;
            panel.hidden = !active;
            panel.classList.toggle('is-active', active);
        });
        dialog.querySelectorAll('[data-ai-step-indicator]').forEach((indicator) => {
            indicator.classList.toggle('is-active', Number(indicator.dataset.aiStepIndicator) <= step);
        });
    };

    const updateCount = () => { count.textContent = String(prompt.value.length); };
    const showError = (message) => {
        error.textContent = message;
        error.hidden = !message;
    };

    openButton.addEventListener('click', () => {
        setStep(1);
        showError('');
        dialog.showModal();
        window.setTimeout(() => prompt.focus(), 50);
    });
    dialog.querySelector('[data-ai-match-close]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('click', (event) => {
        if (event.target === dialog) dialog.close();
    });

    prompt.addEventListener('input', updateCount);
    updateCount();
    dialog.querySelectorAll('[data-ai-suggestion]').forEach((button) => {
        button.addEventListener('click', () => {
            const value = button.dataset.aiSuggestion;
            if (!prompt.value.includes(value)) {
                prompt.value = `${prompt.value.trim()}${prompt.value.trim() ? '，' : ''}${value}`;
                updateCount();
            }
            prompt.focus();
        });
    });

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        showError('');
        submit.disabled = true;
        submit.querySelector('span').textContent = '正在理解需求…';
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || '暫時無法分析需求，請再試一次。');
            responseData = data;
            renderCriteria(data.criteria || []);
            setStep(2);
        } catch (requestError) {
            showError(requestError.message || '暫時無法分析需求，請再試一次。');
        } finally {
            submit.disabled = false;
            submit.querySelector('span').textContent = '下一步';
        }
    });

    dialog.querySelector('[data-ai-match-edit]').addEventListener('click', () => setStep(1));
    dialog.querySelector('[data-ai-match-restart]').addEventListener('click', () => setStep(1));
    dialog.querySelector('[data-ai-match-confirm]').addEventListener('click', () => {
        if (!responseData) return;
        resultTitle.textContent = responseData.message;
        renderResults(responseData.matches || []);
        setStep(3);
    });

    function renderCriteria(criteria) {
        criteriaRoot.replaceChildren();
        criteria.forEach((item) => {
            const row = document.createElement('div');
            row.className = 'ai-match-criterion';
            const label = document.createElement('span');
            label.textContent = item.label;
            const value = document.createElement('strong');
            value.textContent = item.value;
            row.append(label, value);
            criteriaRoot.append(row);
        });
    }

    function renderResults(matches) {
        resultsRoot.replaceChildren();
        if (!matches.length) {
            const empty = document.createElement('div');
            empty.className = 'ai-match-empty';
            empty.textContent = '目前沒有符合需求的公開旅團，請修改目的地、天數或旅伴偏好後再試一次。';
            resultsRoot.append(empty);
            return;
        }

        matches.forEach((match, index) => {
            const card = document.createElement('article');
            card.className = `ai-match-result${index === 0 ? ' is-best' : ''}`;

            const cover = document.createElement('div');
            cover.className = 'ai-match-result__cover';
            if (match.coverImageUrl) {
                const image = document.createElement('img');
                image.src = match.coverImageUrl;
                image.alt = match.title;
                image.loading = 'lazy';
                cover.append(image);
            }

            const body = document.createElement('div');
            body.className = 'ai-match-result__body';
            const titleRow = document.createElement('div');
            titleRow.className = 'ai-match-result__title-row';
            const title = document.createElement('h4');
            title.textContent = match.title;
            const badge = document.createElement('span');
            badge.className = 'ai-match-result__badge';
            badge.textContent = match.matchLabel;
            titleRow.append(title, badge);

            const meta = document.createElement('p');
            meta.className = 'ai-match-result__meta';
            meta.textContent = `${match.location}　｜　${match.days ?? '日期未定'}${match.days ? ' 天' : ''}　｜　目前 ${match.currentPeople}／${match.maxPeople} 人`;

            const reasons = document.createElement('ul');
            reasons.className = 'ai-match-result__reasons';
            match.reasons.forEach((reason) => {
                const item = document.createElement('li');
                item.textContent = reason;
                reasons.append(item);
            });

            const footer = document.createElement('div');
            footer.className = 'ai-match-result__footer';
            const companion = document.createElement('p');
            companion.className = 'ai-match-result__companion';
            companion.textContent = match.companionSummary;
            const links = document.createElement('div');
            links.className = 'ai-match-result__links';
            const details = document.createElement('a');
            details.href = match.detailsUrl;
            details.textContent = '查看旅團詳情';
            const join = document.createElement('a');
            join.href = match.detailsUrl;
            join.textContent = '申請加入';
            links.append(details, join);
            footer.append(companion, links);

            body.append(titleRow, meta, reasons, footer);
            card.append(cover, body);
            resultsRoot.append(card);
        });
    }
})();
