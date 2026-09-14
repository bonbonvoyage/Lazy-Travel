<template>
  <div class="board-toolbar">
    <nav class="groups-tabs" aria-label="揪團分類">
      <button type="button" :class="{ active: filters.scope === 'all' }" id="board-title" @click="changeScope('all')">所有揪團</button>
      <button type="button" :class="{ active: filters.scope === 'mine' }" @click="changeScope('mine')">我的揪團</button>
    </nav>
    <a class="create-group" href="/TravelGroups/Create"><span>＋</span> 新增揪團</a>
  </div>

  <div v-if="isEmpty" :class="['groups-empty', filters.scope === 'mine' ? 'mine-empty' : '']">
    <template v-if="filters.scope === 'mine'">
      <img class="empty-illustration" :src="emptyImageUrl" alt="尚未建立揪團" />
      <h2>目前還沒有自己的揪團</h2>
      <p>建立一個揪團，邀請旅伴一起出發。</p>
      <a href="/TravelGroups/Create">新增揪團</a>
    </template>
    <template v-else>
      <div class="empty-icon">⌁</div>
      <h2>目前沒有符合條件的揪團</h2>
      <p>換個國家或日期看看，也可以清除條件瀏覽全部。</p>
      <a href="/TravelGroups">清除篩選</a>
    </template>
  </div>

  <div v-else class="groups-grid">
    <article class="group-card" v-for="group in groups" :key="group.groupId">
      <a class="card-cover" :href="detailsUrl(group.groupId)" :aria-label="`查看 ${group.title}`">
        <img v-if="group.coverImageUrl" :src="group.coverImageUrl" :alt="group.title" />
        <span v-else class="cover-fallback">LazyTravel</span>
      </a>
      <button class="favorite-button" type="button" :data-group-id="group.groupId" :aria-label="`${group.isFavorited ? '取消收藏' : '收藏'} ${group.title}`" :aria-pressed="String(group.isFavorited)" :disabled="group.isBusy" @click="toggleFavorite(group)">
        <svg viewBox="0 0 24 24"><path d="M12 21s-8-4.8-8-11a4.8 4.8 0 0 1 8-3.5A4.8 4.8 0 0 1 20 10c0 6.2-8 11-8 11Z"/></svg>
      </button>
      <div class="card-content">
        <a class="card-title" :href="detailsUrl(group.groupId)">{{ group.title }}</a>
        <p class="card-meta"><span>⌖</span> {{ locationText(group) }}</p>
        <p class="card-meta"><span>▣</span> {{ group.dateText }}</p>
        <div class="card-tags"><span v-for="tag in group.tags" :key="tag">{{ tag }}</span></div>
        <div class="card-progress"><span>♙</span><strong>{{ group.currentPeople }}</strong> / {{ group.maxPeople }} 人</div>
        <div class="group-card-stats" aria-label="揪團互動數據">
          <span :class="['stat-pill', 'stat-favorite', group.isFavorited ? 'is-active' : '']" title="收藏數" :data-group-id="group.groupId">
            <svg viewBox="0 0 24 24"><path d="M12 21s-8-4.8-8-11a4.8 4.8 0 0 1 8-3.5A4.8 4.8 0 0 1 20 10c0 6.2-8 11-8 11Z"/></svg><strong>{{ group.favoriteCount }}</strong>
          </span>
        </div>
        <footer class="card-footer">
          <div class="owner-avatar"><img v-if="group.ownerAvatarUrl" :src="group.ownerAvatarUrl" alt="" /><span v-else>{{ firstChar(group.ownerName) }}</span></div>
          <div class="owner-copy"><strong>{{ group.ownerName }}</strong><small>揪團發起人</small></div>
          <span :class="['status-badge', `status-${group.status}`]">{{ group.statusText }}</span>
        </footer>
      </div>
    </article>
  </div>

  <div class="groups-load-more" v-if="hasMore">
    <button type="button" :class="{ 'is-loading': isLoadingMore }" :disabled="isLoadingMore" @click="loadMore">{{ isLoadingMore ? '載入中…' : '載入更多' }}</button>
  </div>
</template>

<script setup>
import { computed, reactive, ref } from 'vue';

const props = defineProps({ initialData: { type: Object, required: true } });

const showToast = (message) => {
  const toast = document.querySelector('[data-groups-toast]');
  if (!toast) return;
  const messageNode = toast.querySelector('[data-toast-message]');
  const undoButton = toast.querySelector('[data-toast-undo]');
  if (messageNode) messageNode.textContent = message;
  if (undoButton) undoButton.hidden = true;
  toast.hidden = false;
  window.clearTimeout(showToast.timer);
  showToast.timer = window.setTimeout(() => { toast.hidden = true; }, 3000);
};

const readFiltersFromForm = () => {
  const form = document.querySelector('.groups-filter');
  return {
    country: form?.querySelector('[name="country"]')?.value?.trim() || '',
    startDate: form?.querySelector('[name="startDate"]')?.value || '',
    endDate: form?.querySelector('[name="endDate"]')?.value || '',
  };
};

const toQuery = (state, nextTake) => {
  const params = new URLSearchParams();
  if (state.country) params.set('country', state.country);
  if (state.startDate) params.set('startDate', state.startDate);
  if (state.endDate) params.set('endDate', state.endDate);
  params.set('scope', state.scope || 'all');
  params.set('take', String(nextTake));
  return params;
};

const groups = ref(props.initialData.groups || []);
const totalCount = ref(props.initialData.totalCount || 0);
const take = ref(props.initialData.take || 12);
const isLoadingMore = ref(false);
const isReloading = ref(false);
const filters = reactive({
  scope: props.initialData.scope || 'all',
  country: props.initialData.country || '',
  startDate: props.initialData.startDate || '',
  endDate: props.initialData.endDate || '',
});

const syncFilters = () => Object.assign(filters, readFiltersFromForm(), { scope: filters.scope });
const isEmpty = computed(() => !isReloading.value && groups.value.length === 0);
const hasMore = computed(() => groups.value.length < totalCount.value);

const applyData = (data) => {
  groups.value = data.groups || [];
  totalCount.value = data.totalCount || 0;
  take.value = data.take || groups.value.length || 12;
  filters.scope = data.scope || filters.scope;
  filters.country = data.country || '';
  filters.startDate = data.startDate || '';
  filters.endDate = data.endDate || '';
};

const fetchData = async (nextTake = 12, replaceUrl = true) => {
  syncFilters();
  const params = toQuery(filters, nextTake);
  const response = await fetch(`/TravelGroups/ListData?${params.toString()}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
  if (!response.ok) throw new Error('load groups failed');
  const data = await response.json();
  applyData(data);
  if (replaceUrl) window.history.replaceState({}, '', `/TravelGroups?${params.toString()}`);
};

const changeScope = async (scope) => {
  if (filters.scope === scope && !isReloading.value) return;
  filters.scope = scope;
  take.value = 12;
  isReloading.value = true;
  try { await fetchData(12); }
  catch { showToast('資料載入失敗，請再試一次'); }
  finally { isReloading.value = false; }
};

const loadMore = async () => {
  if (isLoadingMore.value) return;
  isLoadingMore.value = true;
  try { await fetchData(take.value + 12); }
  catch { showToast('載入失敗，請再試一次'); }
  finally { isLoadingMore.value = false; }
};

const toggleFavorite = async (group) => {
  if (group.isBusy) return;
  const previous = group.isFavorited;
  const previousCount = group.favoriteCount || 0;
  group.isBusy = true;
  try {
    const response = await fetch(`/TravelGroups/ToggleFavorite/${group.groupId}`, {
      method: 'POST',
      headers: { 'X-Requested-With': 'XMLHttpRequest' },
      redirect: 'manual',
    });
    if (!response.ok && response.type !== 'opaqueredirect') throw new Error('favorite failed');
    const result = await response.json();
    group.isFavorited = result.active === true;
    group.favoriteCount = result.count ?? Math.max(0, previousCount + (group.isFavorited ? 1 : -1));
    showToast(group.isFavorited ? '已加入收藏' : '已取消收藏');
  } catch {
    group.isFavorited = previous;
    group.favoriteCount = previousCount;
    showToast('收藏更新失敗，請再試一次');
  } finally {
    group.isBusy = false;
  }
};

document.querySelector('.groups-filter')?.addEventListener('submit', async (event) => {
  event.preventDefault();
  take.value = 12;
  isReloading.value = true;
  try { await fetchData(12); }
  catch { event.target.submit(); }
  finally { isReloading.value = false; }
});

const emptyImageUrl = '/images/empty-states/my-groups-empty.svg';
const detailsUrl = (id) => `/TravelGroups/Details/${id}`;
const locationText = (group) => `${group.country || ''}${group.region ? `・${group.region}` : ''}`;
const firstChar = (name) => (name || '旅人').slice(0, 1);
</script>

