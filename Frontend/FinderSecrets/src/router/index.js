import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import History from '../views/HistoryView.vue'
import Login from '../views/LoginView.vue'
import Registr from '../views/RegistrView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/history',
      name: 'history',
      component: History,
    },
    {
      path: '/login',
      name: 'login',
      component: Login,
    },
    {
      path: '/registr',
      name: 'registr',
      component: Registr,
    },
  ],
})

export default router
