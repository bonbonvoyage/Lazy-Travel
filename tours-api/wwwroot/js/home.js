// 首頁互動邏輯：搜尋列樣式、Hero 輪播、以及「熱門行程/熱門文章」兩排卡片。
// 側欄收合統一交給 sidebar.js，避免首頁重複綁定造成點一次切換兩次。
// 卡片資料一律來自 /Home/Data（HomeController.Data() 回傳的真實資料庫查詢結果，Ok(vm) JSON）。

document.querySelectorAll('.search-field select').forEach(sel => {
  const sync = () => sel.classList.toggle('is-placeholder', sel.selectedIndex === 0);
  sel.addEventListener('change', sync);
  sync();
});

function escapeHtml(s) {
  return (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

function scrollRow(id, dir) {
  const track = document.getElementById(id);
  if (track) track.scrollBy({ left: dir * 280, behavior: 'smooth' });
}

function updateRowNavState(track) {
  const scroller = track.closest('.row-scroller');
  const prevBtn = scroller.querySelector('.row-nav.prev');
  const nextBtn = scroller.querySelector('.row-nav.next');
  const maxScroll = track.scrollWidth - track.clientWidth;
  prevBtn.classList.toggle('is-disabled', track.scrollLeft <= 4 || maxScroll <= 4);
  nextBtn.classList.toggle('is-disabled', track.scrollLeft >= maxScroll - 4 || maxScroll <= 4);
}

document.querySelectorAll('.row-nav').forEach(btn => {
  btn.addEventListener('click', () => scrollRow(btn.dataset.row, Number(btn.dataset.dir)));
});

['trip-row', 'article-row'].forEach(id => {
  const track = document.getElementById(id);
  track.addEventListener('scroll', () => updateRowNavState(track));
  window.addEventListener('resize', () => updateRowNavState(track));
});

function peopleIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="9" cy="8" r="3"/><path d="M2 21c0-4 3-6.5 7-6.5s7 2.5 7 6.5"/></svg>';
}
function heartIconSvg() {
  return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 21s-7.5-4.6-10-9.3C.4 8.1 2.4 4.5 6 4a5 5 0 016 2.4A5 5 0 0118 4c3.6.5 5.6 4.1 4 7.7C19.5 16.4 12 21 12 21z"/></svg>';
}

function renderTrips(trips) {
  const track = document.getElementById('trip-row');
  if (!trips.length) {
    track.innerHTML = '<div class="row-empty">目前還沒有公開的揪團行程，敬請期待</div>';
    return;
  }
  track.innerHTML = trips.map((t, i) => `
    <a class="trip-card card-reveal" style="animation-delay:${.1 + i * .06}s" href="/TravelGroups/Details/${encodeURIComponent(t.groupId)}">
      <div class="art">${t.coverImageUrl ? `<img src="${escapeHtml(t.coverImageUrl)}" alt="${escapeHtml(t.groupTitle)}" onerror="this.remove()">` : ''}</div>
      <div class="body">
        <div class="title">${escapeHtml(t.groupTitle)}</div>
        <div class="trip-meta">
          <span class="tabular">${escapeHtml(t.daysText)}</span><span class="sep"></span>
          <span class="who">${peopleIconSvg()}<span class="tabular">${t.currentPeople}/${t.maxPeople} 人</span></span>
        </div>
      </div>
    </a>`).join('');
}

function renderArticles(articles) {
  const track = document.getElementById('article-row');
  if (!articles.length) {
    track.innerHTML = '<div class="row-empty">目前還沒有已發布的文章，敬請期待</div>';
    return;
  }
  track.innerHTML = articles.map((a, i) => `
    <a class="article-card card-reveal" style="animation-delay:${.16 + i * .06}s" href="/Explore/Details/${a.postId}">
      <div class="art">${a.mediaUrl ? `<img src="${escapeHtml(a.mediaUrl)}" alt="${escapeHtml(a.title)}" onerror="this.remove()">` : ''}</div>
      <div class="body">
        <div class="title">${escapeHtml(a.title)}</div>
        <div class="article-foot">
          <span class="trip-meta">${peopleIconSvg()}<span>${escapeHtml(a.authorName)}</span></span>
          <span class="tabular" style="color:var(--text-muted);font-size:.8rem">${escapeHtml(a.dateText)}</span>
        </div>
      </div>
    </a>`).join('');
}

async function loadHomeData() {
  try {
    const res = await fetch('/Home/Data');
    if (!res.ok) throw new Error('HTTP ' + res.status);
    const vm = await res.json();
    renderTrips(vm.popularTrips ?? []);
    renderArticles(vm.popularArticles ?? []);
  } catch (err) {
    console.error('首頁資料載入失敗', err);
    document.getElementById('trip-row').innerHTML = '<div class="row-empty">行程資料載入失敗，請稍後重新整理</div>';
    document.getElementById('article-row').innerHTML = '<div class="row-empty">文章資料載入失敗，請稍後重新整理</div>';
  } finally {
    ['trip-row', 'article-row'].forEach(id => updateRowNavState(document.getElementById(id)));
  }
}
loadHomeData();

/* ---------- Hero 輪播：純行銷文案，不是行程資料，所以維持在前端；
   背景照片是裝飾用的品牌意象圖，不是某一筆真實行程，所以用真實風景照而不是純色色塊。 */
const heroSlides = [
  { img: 'https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=1600&q=80', h: '遇見世界，遇見旅伴', p: '一起出發，讓旅行更有意義' },
  { img: 'https://images.unsplash.com/photo-1470770841072-f978cf4d019e?w=1600&q=80', h: '揪伴同行，行程更有溫度', p: '找到頻率相同的旅伴，一起規劃下一趟旅程' },
  { img: 'https://images.unsplash.com/photo-1512100356356-de1b84283e18?w=1600&q=80', h: '收藏喜歡的行程，隨時出發', p: '把心動的行程先收藏起來，準備好了就出發' },
];
const heroEl = document.getElementById('hero');
heroEl.innerHTML = heroSlides.map((s, i) => `
  <div class="slide ${i === 0 ? 'active' : ''}" data-i="${i}">
    <img src="${escapeHtml(s.img)}" alt="" onerror="this.remove()">
    <div class="slide-scrim"></div>
    <div class="slide-copy"><h2>${escapeHtml(s.h)}</h2><p>${escapeHtml(s.p)}</p></div>
  </div>`).join('') +
  `<button class="hero-arrow prev" id="heroPrev"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"><path d="M15 6l-6 6 6 6"/></svg></button>
   <button class="hero-arrow next" id="heroNext"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"><path d="M9 6l6 6-6 6"/></svg></button>`;
document.getElementById('hero-dots').innerHTML = heroSlides.map((_, i) => `<button class="${i === 0 ? 'active' : ''}" data-i="${i}"></button>`).join('');

let heroIdx = 0, heroTimer;
function showHero(i) {
  heroIdx = (i + heroSlides.length) % heroSlides.length;
  document.querySelectorAll('.slide').forEach((el, idx) => el.classList.toggle('active', idx === heroIdx));
  document.querySelectorAll('#hero-dots button').forEach((el, idx) => el.classList.toggle('active', idx === heroIdx));
}
function resetTimer() { clearInterval(heroTimer); heroTimer = setInterval(() => showHero(heroIdx + 1), 5000); }
function stepHero(dir) { showHero(heroIdx + dir); resetTimer(); }

document.getElementById('heroPrev').addEventListener('click', () => stepHero(-1));
document.getElementById('heroNext').addEventListener('click', () => stepHero(1));
document.querySelectorAll('#hero-dots button').forEach(btn => {
  btn.addEventListener('click', () => { showHero(Number(btn.dataset.i)); resetTimer(); });
});
resetTimer();
