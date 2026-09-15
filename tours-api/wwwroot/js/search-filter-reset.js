// Wait for both the classic filters and the Vue module to initialize Flatpickr.
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.groups-filter').forEach(form => {
        const date = form.querySelector('#groupDateRange');
        const start = form.querySelector('[name="startDate"]');
        const end = form.querySelector('[name="endDate"]');
        const clear = form.querySelector('.filter-date-clear');
        const reset = form.querySelector('.filter-reset');
        if (!date || !start || !end || !clear || !reset) return;

        const picker = date._flatpickr;
        const syncClearButton = () => {
            clear.hidden = !(start.value || end.value || picker?.selectedDates.length);
        };
        const clearDates = () => {
            picker?.clear();
            picker?.close();
            date.value = '';
            start.value = '';
            end.value = '';
            syncClearButton();
        };

        picker?.config.onChange.push(syncClearButton);
        picker?.config.onValueUpdate.push(syncClearButton);
        form.addEventListener('filters:updated', syncClearButton);
        syncClearButton();

        clear.addEventListener('click', () => {
            clearDates();
            // Keep keyboard focus visible without reopening the date picker.
            form.querySelector('.filter-submit')?.focus();
        });

        reset.addEventListener('click', () => {
            clearDates();
            const country = form.querySelector('[name="country"]');
            if (country) {
                country.value = '';
                country.dispatchEvent(new Event('input', { bubbles: true }));
                country.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
            }
            for (const name of ['maxDays', 'page', 'take']) {
                const input = form.elements.namedItem(name);
                if (input) input.value = '';
            }

            if (form.classList.contains('home-groups-filter')) {
                // Home shows recommendations, not filtered results; stay here.
                const url = new URL(window.location.href);
                for (const name of ['country', 'startDate', 'endDate', 'maxDays', 'page', 'take']) url.searchParams.delete(name);
                window.history.replaceState({}, '', url);
            } else if (document.getElementById('travel-groups-vue-app')) {
                form.requestSubmit();
            } else {
                const url = new URL(form.action, window.location.href);
                url.search = '';
                const scope = form.elements.namedItem('scope')?.value;
                if (scope) url.searchParams.set('scope', scope);
                window.location.assign(url);
            }
        });
    });
});
