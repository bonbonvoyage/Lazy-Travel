(() => {
  const countryInput = document.getElementById('groupCountry');
  const countryOptions = document.getElementById('groupCountryOptions');

  if (countryInput && countryOptions) {
    const wrapper = countryInput.closest('.filter-country');
    const toggle = wrapper?.querySelector('.country-toggle');
    const error = document.getElementById('countryError');
    const options = Array.from(countryOptions.querySelectorAll('[role="option"]'));
    const empty = countryOptions.querySelector('.country-empty');
    let active = -1;
    let visible = [];

    options.forEach((option, index) => { option.id = option.id || `country-option-${index}`; });

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
      toggle?.setAttribute('aria-label', '展開國家選單');
      setActive(-1);
    }

    function open(showAll = false) {
      const keyword = showAll ? '' : countryInput.value.trim().toLocaleLowerCase();
      options.forEach(option => {
        const value = option.dataset.value || '';
        option.hidden = !!keyword && !value.toLocaleLowerCase().includes(keyword);
        option.setAttribute('aria-selected', String(value === countryInput.value.trim()));
      });
      visible = options.filter(option => !option.hidden);
      if (empty) empty.hidden = visible.length > 0;
      countryOptions.hidden = false;
      countryInput.setAttribute('aria-expanded', 'true');
      toggle?.setAttribute('aria-label', '收合國家選單');
      setActive(-1);
    }

    function clearError() {
      if (error) error.hidden = true;
      countryInput.removeAttribute('aria-invalid');
    }

    function choose(option) {
      countryInput.value = option.dataset.value || '';
      clearError();
      close();
    }

    countryInput.addEventListener('focus', () => open(true));
    countryInput.addEventListener('click', () => { if (countryOptions.hidden) open(true); });
    countryInput.addEventListener('input', () => { clearError(); open(); });
    toggle?.addEventListener('mousedown', event => event.preventDefault());
    toggle?.addEventListener('click', () => {
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
        if (visible.length) {
          setActive(event.key === 'ArrowDown'
            ? (active + 1) % visible.length
            : (active <= 0 ? visible.length - 1 : active - 1));
        }
      } else if (event.key === 'Enter' && !countryOptions.hidden && active >= 0) {
        event.preventDefault();
        choose(visible[active]);
      } else if (event.key === 'Escape') {
        event.preventDefault();
        close();
      } else if (event.key === 'Tab') {
        close();
      }
    });
    wrapper?.addEventListener('focusout', event => {
      if (!wrapper.contains(event.relatedTarget)) close();
    });
    document.addEventListener('click', event => {
      if (!wrapper?.contains(event.target)) close();
    });
    countryInput.form?.addEventListener('submit', event => {
      countryInput.value = countryInput.value.trim();
      if (!options.some(option => option.dataset.value === countryInput.value)) {
        event.preventDefault();
        countryInput.focus();
        open();
        if (error) error.hidden = false;
        countryInput.setAttribute('aria-invalid', 'true');
      }
    });
  }

  const dateRangeInput = document.getElementById('groupDateRange');
  const startDateInput = document.getElementById('groupStartDate');
  const endDateInput = document.getElementById('groupEndDate');

  function formatDate(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  if (dateRangeInput && startDateInput && endDateInput && window.flatpickr) {
    const initialDates = [startDateInput.value, endDateInput.value].filter(Boolean);
    window.flatpickr(dateRangeInput, {
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
})();
