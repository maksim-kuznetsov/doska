import { getActivePinia } from 'pinia'
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

import DefaultLayout from '../layouts/DefaultLayout.vue'
import { useAuthStore } from '../stores/auth'
import AdFormView from '../views/AdFormView.vue'
import ComplaintsQueueView from '../views/ComplaintsQueueView.vue'
import ConfirmEmailView from '../views/ConfirmEmailView.vue'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import ModerationQueueView from '../views/ModerationQueueView.vue'
import MyAdsView from '../views/MyAdsView.vue'
import RegisterView from '../views/RegisterView.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: DefaultLayout,
    children: [
      {
        path: '',
        name: 'home',
        component: HomeView,
      },
      {
        path: 'login',
        name: 'login',
        component: LoginView,
      },
      {
        path: 'register',
        name: 'register',
        component: RegisterView,
      },
      {
        path: 'confirm-email',
        name: 'confirm-email',
        component: ConfirmEmailView,
      },
      {
        path: 'ads/new',
        name: 'ad-new',
        component: AdFormView,
        meta: { requiresAuth: true },
      },
      {
        path: 'ads/mine',
        name: 'ads-mine',
        component: MyAdsView,
        meta: { requiresAuth: true },
      },
      {
        path: 'moderation',
        name: 'moderation',
        component: ModerationQueueView,
        meta: { requiresModerator: true },
      },
      {
        path: 'complaints',
        name: 'complaints',
        component: ComplaintsQueueView,
        meta: { requiresModerator: true },
      },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

/**
 * Доступность проверяется и на клиенте (чтобы не показывать недоступные страницы),
 * и на сервере — сервер остаётся источником правил.
 */
router.beforeEach(async (to) => {
  // Маршрутизатор может проверяться в изоляции (например, в тестах) до установки Pinia.
  if (getActivePinia() === undefined) {
    return true
  }

  const auth = useAuthStore()

  if (!auth.loaded) {
    await auth.load()
  }

  if (to.meta.requiresAuth === true && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresModerator === true && !auth.canModerate) {
    return { name: 'home' }
  }

  return true
})

export default router