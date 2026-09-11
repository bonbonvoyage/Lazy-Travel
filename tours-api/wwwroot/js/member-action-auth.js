(() => {
    const protectedActionSelector = [
        '#tg-join-btn',
        '#tg-cancel-join-btn',
        '#tg-leave-btn',
        '#tg-favorite-btn',
        '.favorite-button[data-group-id]',
        '[data-report-open]'
    ].join(',');

    const actionMessages = [
        ['[data-report-open]', '請先登入，登入後即可檢舉。'],
        ['#tg-join-btn', '請先登入，登入後即可申請加入。'],
        ['#tg-cancel-join-btn, #tg-leave-btn', '請先登入後再管理揪團。'],
        ['#tg-favorite-btn, .favorite-button[data-group-id]', '請先登入，登入後即可收藏。']
    ];

    function loginTrigger() {
        return document.querySelector('[data-auth-open="login"]');
    }

    function messageFor(action) {
        return actionMessages.find(([selector]) => action.matches(selector))?.[1]
            || '請先登入後再進行此操作。';
    }

    document.addEventListener('click', event => {
        const action = event.target.closest(protectedActionSelector);
        const trigger = loginTrigger();
        if (!action || !trigger) return;

        event.preventDefault();
        event.stopImmediatePropagation();
        trigger.click();

        requestAnimationFrame(() => {
            const error = document.getElementById('ltLoginError');
            if (error) error.textContent = messageFor(action);
        });
    }, true);
})();
