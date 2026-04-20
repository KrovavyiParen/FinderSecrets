import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import History from '../views/HistoryView.vue'
import Login from '../views/LoginView.vue'
import Registr from '../views/RegistrView.vue'
import Graph from '../views/NetworkGraph.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { requiresAuth: true }
    },
    {
      path: '/history',
      name: 'history',
      component: History,
      meta: { requiresAuth: true }
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
    {
      path: '/graph',
      name: 'graph',
      component: Graph,
    },
  ],
})

// Функция проверки аутентификации
const isAuthenticated = () => {
  return !!localStorage.getItem('authToken') || !!sessionStorage.getItem('authToken')
}

router.beforeEach((to, from, next) => {
  if (to.meta.requiresAuth && !isAuthenticated()) {
    next('/login')
  } else {
    next()
  }
})

export default router