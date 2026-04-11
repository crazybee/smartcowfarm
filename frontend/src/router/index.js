import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'

const routes = [
  { path: '/', component: DashboardView },
  { path: '/cows', component: () => import('../views/CowListView.vue') },
  { path: '/vaccinations', component: () => import('../views/VaccinationView.vue') },
]

export default createRouter({
  history: createWebHistory(),
  routes,
})
