document.querySelectorAll('.favorite-button').forEach((button) => {
    button.addEventListener('click', () => {
        const next = button.getAttribute('aria-pressed') !== 'true';
        button.setAttribute('aria-pressed', String(next));
        button.setAttribute('aria-label', next ? '取消收藏' : '收藏揪團');
    });
});

const regionSelect = document.getElementById('groupRegion');
const countrySelect = document.getElementById('groupCountry');

if (regionSelect && countrySelect) {
    const countriesByRegion = window.travelGroupCountryOptions || {};
    const allCountries = window.travelGroupAllCountries || [];
    const selectedCountry = countrySelect.dataset.selected || countrySelect.value;

    renderCountryOptions(regionSelect.value, selectedCountry);

    regionSelect.addEventListener('change', () => {
        renderCountryOptions(regionSelect.value, '');
    });

    function renderCountryOptions(region, selectedValue) {
        const countries = region && countriesByRegion[region]
            ? countriesByRegion[region]
            : allCountries;

        countrySelect.replaceChildren(createOption('', '所有國家'));

        countries.forEach((country) => {
            countrySelect.appendChild(createOption(country, country));
        });

        countrySelect.value = countries.includes(selectedValue) ? selectedValue : '';
    }

    function createOption(value, label) {
        const option = document.createElement('option');
        option.value = value;
        option.textContent = label;
        return option;
    }
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
