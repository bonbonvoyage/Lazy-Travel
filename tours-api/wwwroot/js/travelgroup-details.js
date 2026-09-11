// 揪團找旅伴：行程詳情頁。所有資料來自 /TravelGroups/Data/{id}（真實資料庫查詢），
// 申請加入/退出揪團打真的 POST，用瀏覽器 Cookie 認訪客身分（沒有會員登入系統前的暫時做法）。

function escapeHtml(s) {
  return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

const root = document.getElementById('tg-root');
const groupId = Number(root.dataset.groupId);
let viewerMemberId = root.dataset.viewerMemberId ? Number(root.dataset.viewerMemberId) : null;
let applicationsRefreshTimer = null;
let currentGalleryImages = [];
document.querySelectorAll('[data-back-fallback]').forEach(button => {
  button.addEventListener('click', () => {
    window.location.assign(button.dataset.backFallback || '/TravelGroups');
  });
});

function openLoginPanel() {
  const trigger = document.createElement('button');
  trigger.type = 'button';
  trigger.hidden = true;
  trigger.dataset.authOpen = 'login';
  document.body.appendChild(trigger);
  trigger.click();
  trigger.remove();
}

function requiresLogin(vm) {
  return vm.viewerMode === 'guest' && !viewerMemberId;
}

function authAttrs(vm) {
  return requiresLogin(vm) ? ' data-auth-required="true"' : '';
}

function getAntiForgeryToken() {
  return document.querySelector('#tg-antiforgery input[name="__RequestVerificationToken"]').value;
}

async function postAction(action) {
  const url = new URL(`/TravelGroups/${action}/${groupId}`, window.location.origin);
  if (viewerMemberId) url.searchParams.set('viewerMemberId', viewerMemberId);
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'RequestVerificationToken': getAntiForgeryToken(), 'X-Requested-With': 'XMLHttpRequest' },
  });
  if (res.status === 401) { openLoginPanel(); return false; }
  if (!res.ok) {
    const text = await res.text();
    console.warn(text || '操作失敗，請稍後再試');
    return false;
  }
  return true;
}


async function postOwnerApplicationAction(action, targetId) {
  const actionMap = {
    'remove-member': 'RemoveGroupMember',
    'reject-request': 'RejectJoinRequest',
    'release-rejected': 'ReleaseRejectedJoinRequest',
  };
  const endpoint = actionMap[action];
  if (!endpoint || !targetId) return false;
  const url = new URL(`/TravelGroups/${endpoint}/${groupId}`, window.location.origin);
  if (action === 'remove-member') url.searchParams.set('memberId', targetId);
  else url.searchParams.set('requestId', targetId);
  if (viewerMemberId) url.searchParams.set('viewerMemberId', viewerMemberId);

  const res = await fetch(url, {
    method: 'POST',
    headers: { 'RequestVerificationToken': getAntiForgeryToken(), 'X-Requested-With': 'XMLHttpRequest' },
  });
  if (res.status === 401) { openLoginPanel(); return false; }
  if (!res.ok) {
    const text = await res.text();
    console.warn(text || '管理申請失敗，請稍後再試');
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
function heartIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 21s-8-4.8-8-11a4.8 4.8 0 0 1 8-3.5A4.8 4.8 0 0 1 20 10c0 6.2-8 11-8 11Z"/></svg>';
}
function flagIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 22V4"/><path d="M5 4h12l-1.5 4L17 12H5"/></svg>';
}

function renderGallery(urls) {
  const safeUrls = Array.isArray(urls) ? urls.filter(Boolean) : [];
  if (!safeUrls.length) {
    return '<div class="tg-gallery-empty">目前還沒有照片</div>';
  }
  const shown = safeUrls.slice(0, 5);
  const extraCount = Math.max(0, safeUrls.length - shown.length);
  const cells = shown.map((url, i) => {
    const cls = i === 0 ? 'tg-g-main' : '';
    const overlay = (i === shown.length - 1 && extraCount > 0)
      ? `<span class="tg-g-more-overlay">${photoIconSvg()}<span>更多圖片 +${extraCount}</span></span>`
      : '';
    return `<button class="tg-gallery-tile ${cls}" type="button" data-gallery-open data-gallery-index="${i}"><img src="${escapeHtml(url)}" alt="揪團照片 ${i + 1}" onerror="this.closest('.tg-gallery-tile').remove()">${overlay}</button>`;
  }).join('');
  return cells;
}

function openGalleryModal(images, startIndex = 0) {
  const urls = (Array.isArray(images) ? images : []).filter(Boolean);
  if (!urls.length) return;

  let activeIndex = Math.min(Math.max(Number(startIndex) || 0, 0), urls.length - 1);
  document.getElementById('tg-gallery-modal')?.remove();

  const thumbs = urls.map((url, index) => `
    <button class="tg-gallery-modal-thumb" type="button" data-modal-gallery-index="${index}" aria-label="查看第 ${index + 1} 張照片">
      <img src="${escapeHtml(url)}" alt="">
    </button>`).join('');

  document.body.insertAdjacentHTML('beforeend', `
    <div class="tg-gallery-modal-backdrop" id="tg-gallery-modal" role="dialog" aria-modal="true" aria-label="揪團所有圖片">
      <div class="tg-gallery-modal-panel">
        <button class="tg-gallery-modal-close" type="button" aria-label="關閉圖片瀏覽">×</button>
        <div class="tg-gallery-modal-main">
          <button class="tg-gallery-modal-nav tg-gallery-modal-prev" type="button" aria-label="上一張">＜</button>
          <img class="tg-gallery-modal-image" src="" alt="揪團照片">
          <button class="tg-gallery-modal-nav tg-gallery-modal-next" type="button" aria-label="下一張">＞</button>
        </div>
        <div class="tg-gallery-modal-meta"><span data-gallery-counter></span></div>
        <div class="tg-gallery-modal-thumbs">${thumbs}</div>
      </div>
    </div>`);

  const modal = document.getElementById('tg-gallery-modal');
  const image = modal.querySelector('.tg-gallery-modal-image');
  const counter = modal.querySelector('[data-gallery-counter]');
  const update = index => {
    activeIndex = (index + urls.length) % urls.length;
    image.src = urls[activeIndex];
    image.alt = `揪團照片 ${activeIndex + 1}`;
    counter.textContent = `${activeIndex + 1} / ${urls.length}`;
    modal.querySelectorAll('[data-modal-gallery-index]').forEach(button => {
      button.classList.toggle('active', Number(button.dataset.modalGalleryIndex) === activeIndex);
    });
  };
  const close = () => modal.remove();

  modal.querySelector('.tg-gallery-modal-close')?.addEventListener('click', close);
  modal.querySelector('.tg-gallery-modal-prev')?.addEventListener('click', () => update(activeIndex - 1));
  modal.querySelector('.tg-gallery-modal-next')?.addEventListener('click', () => update(activeIndex + 1));
  modal.querySelectorAll('[data-modal-gallery-index]').forEach(button => {
    button.addEventListener('click', () => update(Number(button.dataset.modalGalleryIndex)));
  });
  modal.addEventListener('click', event => { if (event.target === modal) close(); });
  const keyHandler = event => {
    if (!document.getElementById('tg-gallery-modal')) {
      document.removeEventListener('keydown', keyHandler);
      return;
    }
    if (event.key === 'Escape') close();
    if (event.key === 'ArrowLeft') update(activeIndex - 1);
    if (event.key === 'ArrowRight') update(activeIndex + 1);
  };
  document.addEventListener('keydown', keyHandler);
  update(activeIndex);
}

function getStatusOptions(vm) {
  const status = Number(vm.groupStatus ?? -1);
  if (status === 0 || status === 1) {
    const options = [];
    if (vm.canStartTrip) options.push({ action: 'start', label: '開始行程', message: '開始行程後，將不再接受新的入團申請。' });
    options.push({ action: 'disband', label: '解散揪團', danger: true, message: '解散後此揪團將不再顯示於前台。' });
    return options;
  }
  if (status === 2) return [{ action: 'complete', label: '完成行程', message: '確認此趟行程已結束。' }];
  return [];
}

function openStatusModal(vm) {
  document.getElementById('tg-status-modal')?.remove();
  const options = getStatusOptions(vm);
  const status = Number(vm.groupStatus ?? -1);
  const insufficient = (status === 0 || status === 1) && !vm.canStartTrip;
  const terminal = status === 3 || status === 4;
  const message = terminal ? '此揪團狀態已結束，無法再變更。' : insufficient ? `目前為 ${vm.currentPeople} / ${vm.minPeople} 人，尚未達到最小成行人數，暫時不能開始行程。` : status === 2 && vm.isPastEndDate ? '行程結束日已過，請確認是否完成此趟行程。' : '請選擇下一步狀態。';
  const buttons = options.map(option => `<button class="${option.danger ? 'tg-status-danger' : 'tg-status-primary'}" type="button" data-status-action="${option.action}" data-status-message="${escapeHtml(option.message)}">${option.label}</button>`).join('');
  document.body.insertAdjacentHTML('beforeend', `
    <div class="tg-status-backdrop" id="tg-status-modal" role="dialog" aria-modal="true" aria-label="變更揪團狀態">
      <section class="tg-status-card">
        <button class="tg-status-close" type="button" aria-label="關閉">×</button>
        <span class="tg-status-eyebrow">STATUS</span>
        <h2>變更揪團狀態</h2>
        <p>目前狀態：<strong>${escapeHtml(vm.groupStatusText)}</strong></p>
        <p>${escapeHtml(message)}</p>
        <div class="tg-status-actions">${buttons}<button class="tg-status-secondary" type="button" data-status-close>取消</button></div>
      </section>
    </div>`);
  const modal = document.getElementById('tg-status-modal');
  const close = () => modal?.remove();
  modal?.querySelector('.tg-status-close')?.addEventListener('click', close);
  modal?.querySelector('[data-status-close]')?.addEventListener('click', close);
  modal?.addEventListener('click', event => { if (event.target === modal) close(); });
  modal?.querySelectorAll('[data-status-action]').forEach(button => {
    button.addEventListener('click', async () => {
      button.disabled = true;
      const ok = await postStatusAction(button.dataset.statusAction);
      if (ok) { close(); await load(); }
      else button.disabled = false;
    });
  });
}

async function postStatusAction(action) {
  const url = new URL(`/TravelGroups/ChangeStatus/${groupId}`, window.location.origin);
  if (viewerMemberId) url.searchParams.set('viewerMemberId', viewerMemberId);
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken(), 'X-Requested-With': 'XMLHttpRequest' },
    body: JSON.stringify({ action })
  });
  if (res.status === 401) { openLoginPanel(); return false; }
  if (!res.ok) {
    const text = await res.text();
    window.alert(text || '狀態更新失敗，請稍後再試。');
    return false;
  }
  return true;
}

function renderMembers(members) {
  if (!members.length) {
    return '<div class="tg-empty">目前還沒有團員</div>';
  }
  return members.map(m => {
    const profileUrl = `/Members/Profile?id=${encodeURIComponent(m.memberId)}`;
    return `
    <div class="tg-member">
      <a class="tg-member-link" href="${profileUrl}" aria-label="查看 ${escapeHtml(m.name)} 的個人中心">
        <span class="tg-member-avatar">
          ${m.avatarUrl ? `<img src="${escapeHtml(m.avatarUrl)}" alt="" onerror="this.remove()">` : personIconSvg()}
          ${m.isOwner ? `<span class="tg-member-crown">${crownIconSvg()}</span>` : ''}
        </span>
        <span class="tg-member-name">${escapeHtml(m.name)}</span>
      </a>
      ${m.isOwner ? '<span class="tg-member-role">團主</span>' : ''}
    </div>`;
  }).join('');
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
  const favoriteActive = vm.viewerHasFavorited === true;
  const auth = authAttrs(vm);
  const favoriteBtn = `<button class="tg-btn tg-btn-ghost tg-interaction-btn${favoriteActive ? ' active' : ''}" id="tg-favorite-btn" type="button" aria-pressed="${favoriteActive}"${auth}>${heartIconSvg()}<span>${favoriteActive ? '已收藏' : '收藏房間'}</span></button>`;
  const reportBtn = `<button class="tg-btn tg-btn-ghost" type="button" data-report-open data-report-type="TravelGroup" data-report-target-id="${groupId}" data-report-title="${escapeHtml(vm.groupTitle)}" data-report-viewer-member-id="${viewerMemberId || ''}"${auth}>${flagIconSvg()}<span>檢舉房間</span></button>`;
  const statusBtn = `<button class="tg-btn tg-btn-status" id="tg-status-btn" type="button">變更狀態</button>`;
  const disbandBtn = Number(vm.groupStatus) === 3 || Number(vm.groupStatus) === 4
    ? ''
    : `<button class="tg-btn tg-btn-ghost tg-btn-danger" id="tg-disband-btn" type="button">解散揪團</button>`;

  if (vm.viewerMode === 'owner') {
    return `${disbandBtn}<button class="tg-btn tg-btn-ghost" id="tg-edit-group-btn" type="button">編輯揪團</button><button class="tg-btn tg-btn-ghost" id="tg-applications-btn" type="button">查看揪團申請</button>${favoriteBtn}${statusBtn}`;
  }
  if (vm.viewerMode === 'member') {
    return `<button class="tg-btn tg-btn-ghost" id="tg-leave-btn" type="button">${personIconSvg()}<span>退出揪團</span></button>${favoriteBtn}${reportBtn}`;
  }
  if (vm.viewerHasPendingRequest) {
    return `<button class="tg-btn tg-btn-pending" id="tg-cancel-join-btn" type="button"${auth}>取消申請</button>${favoriteBtn}${reportBtn}`;
  }
  return `<button class="tg-btn tg-btn-ghost" id="tg-join-btn" type="button"${auth}>${personIconSvg()}<span>申請加入</span></button>${favoriteBtn}${reportBtn}`;
}

function formatDateText(value) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return date.toLocaleDateString('zh-TW');
}

function openTicketPreview(url) {
  if (!url) return;
  document.getElementById('tg-ticket-preview-modal')?.remove();
  const safeUrl = String(url).startsWith('/Members/Profile') ? url : '/Members/Profile';
  document.body.insertAdjacentHTML('beforeend', '<div class="tg-ticket-preview-backdrop" id="tg-ticket-preview-modal" role="dialog" aria-modal="true" aria-label="申請者機票"><div class="tg-ticket-preview-frame"><button class="tg-ticket-preview-close" type="button" aria-label="關閉機票">×</button><div class="tg-ticket-preview-shell"><iframe src="' + safeUrl + '" title="申請者機票" loading="lazy"></iframe></div></div></div>');
  const modal = document.getElementById('tg-ticket-preview-modal');
  const close = () => modal?.remove();
  modal?.querySelector('.tg-ticket-preview-close')?.addEventListener('click', close);
  modal?.addEventListener('click', event => { if (event.target === modal) close(); });
}

function renderApplicationPerson(item, status) {
  const appliedAt = formatDateText(item.appliedAt);
  const action = status === 'joined' ? 'remove-member' : status === 'pending' ? 'reject-request' : 'release-rejected';
  const targetId = status === 'joined' ? item.memberId : item.requestId;
  const title = status === 'joined' ? '移除團員並加入已拒絕名單' : status === 'pending' ? '拒絕此申請' : '放出已拒絕名單';
  return `<div class="tg-application-person">
    <div class="tg-application-avatar">${item.avatarUrl ? `<img src="${escapeHtml(item.avatarUrl)}" alt="" onerror="this.remove()">` : personIconSvg()}</div>
    <div class="tg-application-copy">
      <button class="tg-application-name" type="button" data-ticket-url="${escapeHtml(item.profileUrl || `/Members/Profile?id=${item.memberId}&openTicket=1&ticketOnly=1`)}">${escapeHtml(item.name)}</button>
      ${appliedAt ? `<small>${appliedAt}</small>` : ''}
    </div>
    <button class="tg-application-remove" type="button" data-application-action="${action}" data-target-id="${targetId}" title="${title}">×</button>
  </div>`;
}

function renderApplicationColumn(title, description, items, status) {
  return `<section class="tg-application-column">
    <div class="tg-application-column-head"><h3>${escapeHtml(title)}</h3><span>${items.length}</span></div>
    <p>${escapeHtml(description)}</p>
    <div class="tg-application-list">
      ${items.length ? items.map(item => renderApplicationPerson(item, status)).join('') : '<div class="tg-application-empty">目前沒有名單</div>'}
    </div>
  </section>`;
}

function renderApplicationColumns(applications) {
  const data = applications || { joined: [], pending: [], rejected: [] };
  return `${renderApplicationColumn('目前揪團成員', '可檢視目前團員，日後可由 X 移除團員。', data.joined || [], 'joined')}
        ${renderApplicationColumn('申請中', '等待團長審核的入團申請，X 會移至已拒絕。', data.pending || [], 'pending')}
        ${renderApplicationColumn('已拒絕', '仍在拒絕名單中時，會員不能再次送出申請。', data.rejected || [], 'rejected')}`;
}

function updateApplicationsModal(applications) {
  const columns = document.querySelector('#tg-applications-modal [data-application-columns]');
  if (!columns) return;
  columns.innerHTML = renderApplicationColumns(applications);
}

async function refreshApplicationsModal() {
  const vm = await fetchDetailsVm();
  updateApplicationsModal(vm.applications);
  return vm;
}

function stopApplicationsAutoRefresh() {
  if (applicationsRefreshTimer) {
    window.clearInterval(applicationsRefreshTimer);
    applicationsRefreshTimer = null;
  }
}

function startApplicationsAutoRefresh() {
  stopApplicationsAutoRefresh();
  applicationsRefreshTimer = window.setInterval(async () => {
    if (!document.getElementById('tg-applications-modal')) {
      stopApplicationsAutoRefresh();
      return;
    }
    try { await refreshApplicationsModal(); }
    catch (error) { console.warn('入團申請同步失敗', error); }
  }, 5000);
}

function renderApplicationsModal(applications) {
  return `<div class="tg-modal-backdrop" id="tg-applications-modal" role="dialog" aria-modal="true" aria-labelledby="tg-applications-title">
    <div class="tg-applications-modal">
      <button class="tg-modal-close" type="button" aria-label="關閉">×</button>
      <div class="tg-boarding-pass-head">
        <span>✈ BOARDING PASS ・ 入團申請</span>
      </div>
      <div class="tg-applications-title-row">
        <div>
          <p id="tg-applications-title">已拒絕名單中的會員會被保留，放出前不能再次申請此揪團。</p>
        </div>
      </div>
      <div class="tg-application-columns" data-application-columns>${renderApplicationColumns(applications)}</div>
    </div>
  </div>`;
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
  currentGalleryImages = Array.isArray(vm.galleryImageUrls) ? vm.galleryImageUrls.filter(Boolean) : [];
  root.innerHTML = `
    <div class="tg-gallery">${renderGallery(vm.galleryImageUrls)}</div>

    <div class="tg-grid">
        <section class="tg-heading-block">
          <div class="tg-title-row">
            <h1>${escapeHtml(vm.groupTitle)}</h1>
            <span class="tg-status-pill">${escapeHtml(vm.groupStatusText)}</span>
          </div>
          <div class="tg-desc">${escapeHtml(vm.description)}</div>
        </section>
        <div class="tg-actions" id="tg-actions">${renderActions(vm)}</div>

        <div class="tg-card tg-room-details">
          <h3>房間詳細</h3>
          <div class="tg-info-row"><span class="tg-label">國家</span><span class="tg-value">${escapeHtml(vm.country)}</span></div>
          <div class="tg-info-row"><span class="tg-label">地區</span><span class="tg-value">${escapeHtml(vm.region)}</span></div>
          <div class="tg-info-row"><span class="tg-label">行程區間</span><span class="tg-value">${escapeHtml(vm.dateRangeText)}　${escapeHtml(vm.daysNightsText)}</span></div>
          <div class="tg-info-row"><span class="tg-label">人數上限</span><span class="tg-value">${vm.maxPeople} 人</span></div>
          <div class="tg-info-row"><span class="tg-label">最少成行</span><span class="tg-value">${vm.minPeople} 人</span></div>
          <div class="tg-info-row"><span class="tg-label">目前參加</span><span class="tg-value">${vm.currentPeople}/${vm.maxPeople} 人</span></div>
        </div>
      <div class="tg-room-sidebar">
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

  root.querySelectorAll('[data-auth-required="true"]').forEach(button => {
    button.addEventListener('click', event => {
      event.preventDefault();
      event.stopImmediatePropagation();
      openLoginPanel();
    }, { capture: true });
  });

  root.querySelectorAll('[data-gallery-open]').forEach(button => {
    button.addEventListener('click', () => openGalleryModal(currentGalleryImages, Number(button.dataset.galleryIndex || 0)));
  });

  root.querySelectorAll('.tg-tab').forEach(tab => {
    tab.addEventListener('click', () => {
      root.querySelectorAll('.tg-tab').forEach(t => t.classList.toggle('active', t === tab));
      root.querySelectorAll('.tg-tab-panel').forEach(p => { p.hidden = p.dataset.panel !== tab.dataset.tab; });
    });
  });

  document.getElementById('tg-edit-group-btn')?.addEventListener('click', () => {
    const url = new URL('/TravelGroups/Create', window.location.origin);
    url.searchParams.set('editId', groupId);
    if (viewerMemberId) url.searchParams.set('viewerMemberId', viewerMemberId);
    window.location.assign(url.toString());
  });

  document.getElementById('tg-status-btn')?.addEventListener('click', () => openStatusModal(vm));
  document.getElementById('tg-disband-btn')?.addEventListener('click', async event => {
    const button = event.currentTarget;
    button.disabled = true;
    if (await postStatusAction('disband')) await load();
    else button.disabled = false;
  });

  document.getElementById('tg-applications-btn')?.addEventListener('click', () => {
    stopApplicationsAutoRefresh();
    document.getElementById('tg-applications-modal')?.remove();
    document.body.insertAdjacentHTML('beforeend', renderApplicationsModal(vm.applications));
    const modal = document.getElementById('tg-applications-modal');
    const close = () => { stopApplicationsAutoRefresh(); modal?.remove(); };
    modal?.querySelector('.tg-modal-close')?.addEventListener('click', close);
    modal?.addEventListener('click', async event => {
      if (event.target === modal) { close(); return; }
      const ticketButton = event.target.closest('[data-ticket-url]');
      if (ticketButton) { openTicketPreview(ticketButton.dataset.ticketUrl); return; }
      const actionButton = event.target.closest('[data-application-action]');
      if (!actionButton) return;
      const action = actionButton.dataset.applicationAction;
      const targetId = actionButton.dataset.targetId;
      const confirmMessage = action === 'remove-member'
        ? '確定要移除此團員並加入已拒絕名單嗎？'
        : action === 'reject-request'
          ? '確定要拒絕此申請嗎？'
          : '確定要將此會員移出已拒絕名單嗎？';
      if (!confirm(confirmMessage)) return;
      actionButton.disabled = true;
      const ok = await postOwnerApplicationAction(action, targetId);
      if (ok) {
        try { await refreshApplicationsModal(); }
        catch (error) { console.warn('入團申請刷新失敗', error); }
      } else {
        actionButton.disabled = false;
      }
    });
    refreshApplicationsModal().catch(error => console.warn('入團申請刷新失敗', error));
    startApplicationsAutoRefresh();
  });

  document.getElementById('tg-favorite-btn')?.addEventListener('click', async event => {
    const button = event.currentTarget;
    button.disabled = true;
    try {
      const response = await fetch('/TravelGroups/ToggleFavorite/' + groupId, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        redirect: 'manual'
      });
      if (response.status === 401) { openLoginPanel(); return; }
      if (!response.ok && response.type !== 'opaqueredirect') throw new Error(await response.text() || '收藏更新失敗');
      const result = await response.json();
      const next = result.active === true;
      button.classList.toggle('active', next);
      button.setAttribute('aria-pressed', String(next));
      button.querySelector('span').textContent = next ? '已收藏' : '收藏房間';
    } catch (error) {
      console.warn(error.message || '收藏更新失敗，請稍後再試');
    } finally {
      button.disabled = false;
    }
  });

  document.getElementById('tg-join-btn')?.addEventListener('click', async event => {
    const button = event.currentTarget;
    button.disabled = true;
    if (await postAction('Join')) await load();
    else button.disabled = false;
  });

  document.getElementById('tg-cancel-join-btn')?.addEventListener('click', async event => {
    const button = event.currentTarget;
    button.disabled = true;
    if (await postAction('CancelJoin')) await load();
    else button.disabled = false;
  });

  document.getElementById('tg-leave-btn')?.addEventListener('click', async event => {
    if (!confirm('確定要退出這個揪團嗎？')) return;
    const button = event.currentTarget;
    button.disabled = true;
    if (await postAction('Leave')) await load();
    else button.disabled = false;
  });
}

async function fetchDetailsVm() {
  const query = viewerMemberId ? `?viewerMemberId=${viewerMemberId}` : "";
  const res = await fetch(`/TravelGroups/Data/${groupId}${query}`, { headers: { Accept: 'application/json' } });
  if (!res.ok) throw new Error('HTTP ' + res.status);
  return await res.json();
}

async function load() {
  try {
    stopApplicationsAutoRefresh();
    document.getElementById('tg-applications-modal')?.remove();
    const vm = await fetchDetailsVm();
    render(vm);
  } catch (err) {
    console.error('行程詳情載入失敗', err);
    root.innerHTML = '<div class="tg-empty">行程資料載入失敗，請稍後重新整理</div>';
  }
}

const viewerSwitch = document.getElementById('tgViewerSwitch');
viewerSwitch?.addEventListener('change', event => {
  viewerMemberId = event.currentTarget.value ? Number(event.currentTarget.value) : null;
  const url = new URL(window.location.href);
  if (viewerMemberId) url.searchParams.set('viewerMemberId', viewerMemberId);
  else url.searchParams.delete('viewerMemberId');
  window.history.replaceState({}, '', url);
  root.dataset.viewerMemberId = viewerMemberId ? String(viewerMemberId) : '';
  load();
});

window.addEventListener('message', event => {
  if (event.origin !== window.location.origin) return;
  if (event.data?.type !== 'lazytravel:close-ticket-preview') return;
  document.getElementById('tg-ticket-preview-modal')?.remove();
});

load();
