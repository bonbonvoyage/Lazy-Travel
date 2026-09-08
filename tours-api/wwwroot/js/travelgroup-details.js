// 揪團找旅伴：行程詳情頁。所有資料來自 /TravelGroups/Data/{id}（真實資料庫查詢），
// 申請加入/退出揪團打真的 POST，用瀏覽器 Cookie 認訪客身分（沒有會員登入系統前的暫時做法）。

function escapeHtml(s) {
  return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

const root = document.getElementById('tg-root');
const groupId = root.dataset.groupId;

function getAntiForgeryToken() {
  return document.querySelector('#tg-antiforgery input[name="__RequestVerificationToken"]').value;
}

async function postAction(action) {
  const res = await fetch(`/TravelGroups/${action}/${groupId}`, {
    method: 'POST',
    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
  });
  if (!res.ok) {
    const text = await res.text();
    alert(text || '操作失敗，請稍後再試');
    return false;
  }
  return true;
}

function personIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="8" r="4"/><path d="M4 21c0-4.4 3.6-7 8-7s8 2.6 8 7"/></svg>';
}
function crownIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="#F0B429" stroke="none"><path d="M3 8l4 3 5-6 5 6 4-3-2 10H5L3 8z"/></svg>';
}
function photoIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="5" width="18" height="14" rx="2"/><circle cx="8.5" cy="10.5" r="1.5"/><path d="M21 15l-5-5L5 19"/></svg>';
}
function chatIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15a4 4 0 0 1-4 4H8l-5 3V7a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4z"/><path d="M8 10h.01M12 10h.01M16 10h.01"/></svg>';
}
function heartIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 21s-8-4.8-8-11a4.8 4.8 0 0 1 8-3.5A4.8 4.8 0 0 1 20 10c0 6.2-8 11-8 11Z"/></svg>';
}
function flagIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 22V4"/><path d="M5 4h12l-1.5 4L17 12H5"/></svg>';
}

function renderGallery(urls) {
  if (!urls.length) {
    return '<div class="tg-gallery-empty">目前還沒有照片</div>';
  }
  const shown = urls.slice(0, 5);
  const extraCount = Math.max(0, urls.length - shown.length);
  const cells = shown.map((url, i) => {
    const cls = i === 0 ? 'tg-g-main' : '';
    const overlay = (i === shown.length - 1 && extraCount > 0)
      ? `<button class="tg-g-more-overlay" type="button">${photoIconSvg()}<span>更多圖片 +${extraCount}</span></button>`
      : '';
    return `<div class="${cls}"><img src="${escapeHtml(url)}" alt="" onerror="this.closest('div').remove()">${overlay}</div>`;
  }).join('');
  return cells;
}

function renderMembers(members) {
  if (!members.length) {
    return '<div class="tg-empty">目前還沒有團員</div>';
  }
  return members.map(m => `
    <div class="tg-member">
      <div class="tg-member-avatar">
        ${m.avatarUrl ? `<img src="${escapeHtml(m.avatarUrl)}" alt="" onerror="this.remove()">` : personIconSvg()}
        ${m.isOwner ? `<span class="tg-member-crown">${crownIconSvg()}</span>` : ''}
      </div>
      <span class="tg-member-name">${escapeHtml(m.name)}</span>
      ${m.isOwner ? '<span class="tg-member-role">團主</span>' : ''}
    </div>`).join('');
}

function renderItinerary(days) {
  if (!days.length) {
    return '<div class="tg-empty">還沒有安排行程</div>';
  }
  return days.map(d => `
    <div class="tg-day">
      <div class="tg-day-head"><span><span class="tg-day-badge">DAY ${d.dayNumber}</span></span></div>
      ${d.stops.map(s => `
        <div class="tg-stop">
          ${s.timeRangeText ? `<div class="tg-stop-time">${escapeHtml(s.timeRangeText)}</div>` : ''}
          <div class="tg-stop-title">${escapeHtml(s.title)}${s.locationName ? '　' + escapeHtml(s.locationName) : ''}</div>
          ${s.description ? `<div class="tg-stop-desc">${escapeHtml(s.description)}</div>` : ''}
        </div>`).join('')}
    </div>`).join('');
}

function renderBudget(items, total) {
  if (!items.length) {
    return '<div class="tg-empty">還沒有預算資料</div>';
  }
  const rows = items.map(b => `
    <div class="tg-budget-row">
      <span>${escapeHtml(b.budgetName)}${b.isRequired ? '<span class="tg-required">必要</span>' : ''}</span>
      <span class="tabular">${b.amount != null ? `${escapeHtml(b.currencyCode)} $${b.amount.toLocaleString()}` : '—'}</span>
    </div>`).join('');
  return rows + `<div class="tg-budget-total"><span>預估每人</span><span class="tabular">TWD $${total.toLocaleString()}</span></div>`;
}

function renderActions(vm) {
  const messageBtn = `<button class="tg-btn tg-btn-ghost" disabled title="還沒有站內訊息功能">${chatIconSvg()}<span>私訊團主</span></button>`;
  const favoriteBtn = `<button class="tg-btn tg-btn-ghost" disabled title="還沒有收藏功能">${heartIconSvg()}<span>收藏房間</span></button>`;
  const reportBtn = `<button class="tg-btn tg-btn-ghost" disabled title="檢舉表單串接中">${flagIconSvg()}<span>檢舉房間</span></button>`;
  const exportBtn = `<button class="tg-btn tg-btn-ghost" disabled title="匯出文章功能開發中">${photoIconSvg()}<span>匯出文章</span></button>`;

  if (vm.viewerIsOwner) {
    return `<button class="tg-btn tg-btn-ghost" disabled>解散揪團</button><button class="tg-btn tg-btn-ghost" disabled>編輯揪團</button>${favoriteBtn}${reportBtn}${exportBtn}`;
  }
  if (vm.viewerIsMember) {
    return `<button class="tg-btn tg-btn-ghost" id="tg-leave-btn">${personIconSvg()}<span>退出揪團</span></button>${messageBtn}${favoriteBtn}${reportBtn}`;
  }
  if (vm.viewerHasPendingRequest) {
    return `<button class="tg-btn tg-btn-ghost" disabled>申請審核中</button>${messageBtn}${favoriteBtn}${reportBtn}`;
  }
  return `<button class="tg-btn tg-btn-ghost" id="tg-join-btn">${personIconSvg()}<span>申請加入</span></button>${messageBtn}${favoriteBtn}${reportBtn}`;
}

function renderActivityLog(log) {
  if (!log.length) {
    return '<div class="tg-empty">還沒有任何動態</div>';
  }
  return log.map(a => `
    <div class="tg-activity-row">
      <span class="tg-activity-dot"></span>
      <span class="tg-activity-date">${new Date(a.when).toLocaleDateString('zh-TW')}</span>
      <span class="tg-activity-text">${escapeHtml(a.text)}</span>
    </div>`).join('');
}

function render(vm) {
  root.innerHTML = `
    <div class="tg-gallery">${renderGallery(vm.galleryImageUrls)}</div>

    <div class="tg-grid">
      <div>
        <section class="tg-heading-block">
          <div class="tg-title-row">
            <h1>${escapeHtml(vm.groupTitle)}</h1>
            <span class="tg-status-pill">${escapeHtml(vm.groupStatusText)}</span>
          </div>
          <div class="tg-desc">${escapeHtml(vm.description)}</div>
        </section>

        <div class="tg-card">
          <h3>房間詳細</h3>
          <div class="tg-info-row"><span class="tg-label">國家</span><span class="tg-value">${escapeHtml(vm.country)}</span></div>
          <div class="tg-info-row"><span class="tg-label">地區</span><span class="tg-value">${escapeHtml(vm.region)}</span></div>
          <div class="tg-info-row"><span class="tg-label">行程區間</span><span class="tg-value">${escapeHtml(vm.dateRangeText)}　${escapeHtml(vm.daysNightsText)}</span></div>
          <div class="tg-info-row"><span class="tg-label">人數上限</span><span class="tg-value">${vm.maxPeople} 人</span></div>
          <div class="tg-info-row"><span class="tg-label">最少成行</span><span class="tg-value">${vm.minPeople} 人</span></div>
          <div class="tg-info-row"><span class="tg-label">目前參加</span><span class="tg-value">${vm.currentPeople}/${vm.maxPeople} 人</span></div>
        </div>
      </div>
      <div>
        <div class="tg-actions" id="tg-actions">${renderActions(vm)}</div>
        <div class="tg-card">
          <h3>已加入的旅伴（${vm.members.length}/${vm.maxPeople}）</h3>
          <div class="tg-members">${renderMembers(vm.members)}</div>
        </div>
        <div class="tg-card">
          <div class="tg-tabs">
            <button class="tg-tab active" data-tab="activity">動態歷程</button>
            <button class="tg-tab" data-tab="budget">預算試算</button>
          </div>
          <div class="tg-tab-panel" data-panel="activity">${renderActivityLog(vm.activityLog)}</div>
          <div class="tg-tab-panel" data-panel="budget" hidden>${renderBudget(vm.budgetItems, vm.budgetTotalPerPerson)}</div>
        </div>
      </div>
    </div>

    <div class="tg-card tg-itinerary-card">
      <h3>行程表</h3>
      ${renderItinerary(vm.itineraryDays)}
    </div>
  `;

  root.querySelectorAll('.tg-tab').forEach(tab => {
    tab.addEventListener('click', () => {
      root.querySelectorAll('.tg-tab').forEach(t => t.classList.toggle('active', t === tab));
      root.querySelectorAll('.tg-tab-panel').forEach(p => { p.hidden = p.dataset.panel !== tab.dataset.tab; });
    });
  });

  document.getElementById('tg-join-btn')?.addEventListener('click', async (e) => {
    e.target.disabled = true;
    if (await postAction('Join')) {
      await load();
    } else {
      e.target.disabled = false;
    }
  });
  document.getElementById('tg-leave-btn')?.addEventListener('click', async (e) => {
    if (!confirm('確定要退出這個揪團嗎？')) return;
    e.target.disabled = true;
    if (await postAction('Leave')) {
      await load();
    } else {
      e.target.disabled = false;
    }
  });
}

async function load() {
  try {
    const res = await fetch(`/TravelGroups/Data/${groupId}`);
    if (!res.ok) throw new Error('HTTP ' + res.status);
    const vm = await res.json();
    render(vm);
  } catch (err) {
    console.error('行程詳情載入失敗', err);
    root.innerHTML = '<div class="tg-empty">行程資料載入失敗，請稍後重新整理</div>';
  }
}
load();
