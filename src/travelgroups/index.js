import { createApp } from 'vue';
import TravelGroupsIndex from './TravelGroupsIndex.vue';

const initialNode = document.getElementById('travel-groups-initial-data');
const mountNode = document.getElementById('travel-groups-vue-app');
let initialData = null;

try {
  initialData = initialNode?.textContent ? JSON.parse(initialNode.textContent) : null;
} catch {
  initialData = null;
}

if (mountNode && initialData) {
  createApp(TravelGroupsIndex, { initialData }).mount(mountNode);
}
