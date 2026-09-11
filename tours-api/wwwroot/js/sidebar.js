const sidebar = document.getElementById('sidebar');
const sidebarToggle = document.getElementById('toggleBtn');
const sidebarScrim = document.getElementById('sidebarScrim') || document.querySelector('.sidebar-scrim');
const mobileSidebarQuery = window.matchMedia('(max-width: 760px)');
const sidebarStorageKey = 'lazytravel.sidebar.expanded';

function readSavedSidebarState() {
  const saved = localStorage.getItem(sidebarStorageKey);
  if (saved === 'true') return true;
  if (saved === 'false') return false;
  return !mobileSidebarQuery.matches;
}

function setSidebarExpanded(expanded, persist = true) {
  if (!sidebar || !sidebarToggle) return;

  sidebar.classList.toggle('expanded', expanded);
  sidebarToggle.setAttribute('aria-expanded', String(expanded));
  sidebarToggle.setAttribute('aria-label', expanded ? '收合側欄' : '展開側欄');
  sidebarScrim?.classList.toggle('is-visible', expanded && mobileSidebarQuery.matches);

  if (persist) localStorage.setItem(sidebarStorageKey, String(expanded));
}

sidebarToggle?.addEventListener('click', () => {
  setSidebarExpanded(!sidebar?.classList.contains('expanded'));
});

sidebarScrim?.addEventListener('click', () => setSidebarExpanded(false));

sidebar?.querySelectorAll('.nav-item').forEach(link => {
  link.addEventListener('click', () => {
    if (mobileSidebarQuery.matches) setSidebarExpanded(false);
  });
});

document.addEventListener('keydown', event => {
  if (event.key === 'Escape' && mobileSidebarQuery.matches) {
    setSidebarExpanded(false);
    sidebarToggle?.focus();
  }
});

mobileSidebarQuery.addEventListener?.('change', () => {
  const saved = localStorage.getItem(sidebarStorageKey);
  setSidebarExpanded(saved === null ? !mobileSidebarQuery.matches : saved === 'true', false);
});

setSidebarExpanded(readSavedSidebarState(), false);
