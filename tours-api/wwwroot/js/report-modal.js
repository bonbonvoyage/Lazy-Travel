(function () {
  const reasons = [
    { value: '0', label: '廣告垃圾訊息' },
    { value: '1', label: '詐騙／安全疑慮' },
    { value: '2', label: '騷擾／不當言論' },
    { value: '3', label: '版權／抄襲爭議' },
    { value: '4', label: '服務／行程糾紛' },
    { value: '5', label: '其他' },
  ];

  function token() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  function label(type) {
    return type === 'TravelGroup' ? '揪團房間' : type === 'VlogPost' ? '行程文章' : '內容';
  }

  function close() {
    document.getElementById('lt-report-modal')?.remove();
  }

  function open(button) {
    close();
    const type = button.dataset.reportType;
    const targetId = button.dataset.reportTargetId;
    const title = button.dataset.reportTitle || label(type);
    const viewerMemberId = button.dataset.reportViewerMemberId || '';
    document.body.insertAdjacentHTML('beforeend', `
      <div class="lt-report-backdrop" id="lt-report-modal" role="dialog" aria-modal="true" aria-label="檢舉${label(type)}">
        <form class="lt-report-card" enctype="multipart/form-data">
          <button class="lt-report-close" type="button" aria-label="關閉">×</button>
          <div class="lt-report-head">
            <span class="lt-report-kicker">REPORT</span>
            <h2>檢舉${label(type)}</h2>
            <p>你正在檢舉「${escapeHtml(title)}」。請選擇原因並上傳截圖，協助後台判斷。</p>
          </div>
          <div class="lt-report-reasons" role="radiogroup" aria-label="檢舉原因">
            ${reasons.map((r, i) => `<label><input type="radio" name="ReasonCategory" value="${r.value}" ${i === 0 ? 'checked' : ''}><span>${r.label}</span></label>`).join('')}
          </div>
          <label class="lt-report-field"><span>補充說明（選填）</span><textarea name="Description" maxlength="500" placeholder="可以補充你看到的狀況，最多 500 字"></textarea></label>
          <label class="lt-report-upload"><input type="file" name="Evidence" accept="image/jpeg,image/png,image/webp" required hidden><span class="lt-report-upload-icon">＋</span><strong>上傳截圖證據</strong><small>必填，支援 JPG / PNG / WEBP，5MB 內</small><img class="lt-report-preview" data-report-preview alt="截圖預覽" hidden><em data-report-file>尚未選擇檔案</em></label>
          <div class="lt-report-message" data-report-message></div>
          <div class="lt-report-actions"><button class="lt-report-secondary" type="button">取消</button><button class="lt-report-primary" type="submit">送出檢舉</button></div>
          <input type="hidden" name="TargetType" value="${type}"><input type="hidden" name="TargetId" value="${targetId}"><input type="hidden" name="ViewerMemberId" value="${viewerMemberId}">
        </form>
      </div>`);
    const modal = document.getElementById('lt-report-modal');
    const form = modal.querySelector('form');
    const file = form.querySelector('input[type="file"]');
    const fileText = form.querySelector('[data-report-file]');
    const preview = form.querySelector('[data-report-preview]');
    const message = form.querySelector('[data-report-message]');
    const submit = form.querySelector('.lt-report-primary');
    modal.querySelector('.lt-report-close').addEventListener('click', close);
    modal.querySelector('.lt-report-secondary').addEventListener('click', close);
    modal.addEventListener('click', e => { if (e.target === modal) close(); });
    file.addEventListener('change', () => {
      const selected = file.files?.[0];
      fileText.textContent = selected?.name || '尚未選擇檔案';
      if (selected && preview) {
        preview.src = URL.createObjectURL(selected);
        preview.hidden = false;
      } else if (preview) {
        preview.removeAttribute('src');
        preview.hidden = true;
      }
    });
    form.addEventListener('submit', async e => {
      e.preventDefault();
      message.textContent = '';
      if (!file.files?.[0]) { message.textContent = '請上傳檢舉截圖。'; return; }
      submit.disabled = true; submit.textContent = '送出中...';
      try {
        const res = await fetch('/Report/SubmitTargetReport', { method: 'POST', headers: { RequestVerificationToken: token(), 'X-Requested-With': 'XMLHttpRequest' }, body: new FormData(form) });
        const data = await res.json().catch(() => null);
        if (!res.ok) throw new Error(data?.message || '檢舉送出失敗，請稍後再試。');
        message.classList.add('is-success');
        message.textContent = data?.message || '檢舉已送出。';
        setTimeout(close, 1100);
      } catch (err) {
        message.classList.remove('is-success');
        message.textContent = err.message || '檢舉送出失敗，請稍後再試。';
        submit.disabled = false; submit.textContent = '送出檢舉';
      }
    });
  }

  function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  }

  document.addEventListener('click', event => {
    const button = event.target.closest('[data-report-open]');
    if (!button) return;
    event.preventDefault();
    open(button);
  });
})();
