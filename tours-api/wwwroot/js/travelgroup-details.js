// 揪團找旅伴：行程詳情頁。所有資料來自 /TravelGroups/Data/{id}（真實資料庫查詢），
// 申請加入/退出揪團打真的 POST，用瀏覽器 Cookie 認訪客身分（沒有會員登入系統前的暫時做法）。

function escapeHtml(s) {
  return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

const root = document.getElementById('tg-root');
const groupId = Number(root.dataset.groupId);
let viewerMemberId = root.dataset.viewerMemberId ? Number(root.dataset.viewerMemberId) : null;
let applicationsRefreshTimer = null;
document.querySelectorAll('[data-back-fallback]').forEach(button => {
  button.addEventListener('click', () => {
    window.location.assign(button.dataset.backFallback || '/TravelGroups');
  });
});

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
  const favoriteActive = vm.viewerHasFavorited === true;
  const favoriteBtn = `<button class="tg-btn tg-btn-ghost tg-interaction-btn${favoriteActive ? ' active' : ''}" id="tg-favorite-btn" type="button" aria-pressed="${favoriteActive}">${heartIconSvg()}<span>${favoriteActive ? '已收藏' : '收藏房間'}</span></button>`;
  const reportBtn = `<button class="tg-btn tg-btn-ghost" type="button" data-report-open data-report-type="TravelGroup" data-report-target-id="${groupId}" data-report-title="${escapeHtml(vm.groupTitle)}" data-report-viewer-member-id="${viewerMemberId || ''}">${flagIconSvg()}<span>檢舉房間</span></button>`;

  if (vm.viewerMode === 'owner') {
    return `<button class="tg-btn tg-btn-ghost tg-btn-danger" type="button" disabled>解散揪團</button><button class="tg-btn tg-btn-ghost" id="tg-edit-group-btn" type="button">編輯揪團</button><button class="tg-btn tg-btn-ghost" id="tg-applications-btn" type="button">查看揪團申請</button>${favoriteBtn}`;
  }
  if (vm.viewerMode === 'member') {
    return `<button class="tg-btn tg-btn-ghost" id="tg-leave-btn">${personIconSvg()}<span>退出揪團</span></button>${favoriteBtn}${reportBtn}`;
  }
  if (vm.viewerHasPendingRequest) {
    return `<button class="tg-btn tg-btn-pending" id="tg-cancel-join-btn" type="button">取消申請</button>${favoriteBtn}${reportBtn}`;
  }
  return `<button class="tg-btn tg-btn-ghost" id="tg-join-btn">${personIconSvg()}<span>申請加入</span></button>${favoriteBtn}${reportBtn}`;
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
  document.getElementById('tg-join-btn')?.addEventListener('click', async (event) => {
    const button = event.currentTarget;
    button.disabled = true;
    if (await postAction('Join')) {
      await 
load();
    } else {
      button.disabled = false;
    }
  });
  document.getElementById('tg-cancel-join-btn')?.addEventListener('click', async (event) => {
    const button = event.currentTarget;
    button.disabled = true;
    if (await postAction('CancelJoin')) {
      await 
load();
    } else {
      button.disabled = false;
    }
  });
  document.getElementById('tg-leave-btn')?.addEventListener('click', async (event) => {
    if (!confirm('確定要退出這個揪團嗎？')) return;
    const button = event.currentTarget;
    button.disabled = true;
    if (await postAction('Leave')) {
      await 
load();
    } else {
      button.disabled = false;
    }
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



















