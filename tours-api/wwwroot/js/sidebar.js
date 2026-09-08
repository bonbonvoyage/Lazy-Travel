const sidebar = document.getElementById('sidebar');
const sidebarToggle = document.getElementById('toggleBtn');
const sidebarScrim = document.getElementById('sidebarScrim');
const mobileSidebarQuery = window.matchMedia('(max-width: 760px)');

function setSidebarExpanded(expanded) {
  if (!sidebar || !sidebarToggle) return;

  sidebar.classList.toggle('expanded', expanded);
  sidebarToggle.setAttribute('aria-expanded', String(expanded));
  sidebarToggle.setAttribute('aria-label', expanded ? '收合側欄' : '展開側欄');
  sidebarScrim?.classList.toggle('is-visible', expanded && mobileSidebarQuery.matches);
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

function syncSidebarForViewport(event) {
  setSidebarExpanded(!event.matches);
}

mobileSidebarQuery.addEventListener?.('change', syncSidebarForViewport);
setSidebarExpanded(!mobileSidebarQuery.matches);
