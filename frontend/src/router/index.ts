import { getActivePinia } from 'pinia'
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

import DefaultLayout from '../layouts/DefaultLayout.vue'
import { useAuthStore } from '../stores/auth'
import AdDetailView from '../views/AdDetailView.vue'
import AdEditView from '../views/AdEditView.vue'
import AdFormView from '../views/AdFormView.vue'
import AdminCategoriesView from '../views/AdminCategoriesView.vue'
import AdminCitiesView from '../views/AdminCitiesView.vue'
import AdminOutboxView from '../views/AdminOutboxView.vue'
import AdminUsersView from '../views/AdminUsersView.vue'
import ComplaintsQueueView from '../views/ComplaintsQueueView.vue'
import ConfirmEmailView from '../views/ConfirmEmailView.vue'
import DesignSystemGuide from '../views/DesignSystemGuide.vue'
import ForgotPasswordView from '../views/ForgotPasswordView.vue'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import ModerationQueueView from '../views/ModerationQueueView.vue'
import MyAdsView from '../views/MyAdsView.vue'
import NotFoundView from '../views/NotFoundView.vue'
import RegisterView from '../views/RegisterView.vue'
import ResendConfirmationView from '../views/ResendConfirmationView.vue'
import ResetPasswordView from '../views/ResetPasswordView.vue'

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
        path: 'forgot-password',
        name: 'forgot-password',
        component: ForgotPasswordView,
      },
      {
        path: 'reset-password',
        name: 'reset-password',
        component: ResetPasswordView,
      },
      {
        path: 'resend-confirmation',
        name: 'resend-confirmation',
        component: ResendConfirmationView,
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
        path: 'ads/:id/edit',
        name: 'ad-edit',
        component: AdEditView,
        meta: { requiresAuth: true },
      },
      {
        path: 'ads/:id',
        name: 'item-detail',
        component: AdDetailView,
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
      {
        path: 'admin/categories',
        name: 'admin-categories',
        component: AdminCategoriesView,
        meta: { requiresAdmin: true },
      },
      {
        path: 'admin/cities',
        name: 'admin-cities',
        component: AdminCitiesView,
        meta: { requiresAdmin: true },
      },
      {
        path: 'admin/users',
        name: 'admin-users',
        component: AdminUsersView,
        meta: { requiresAdmin: true },
      },
      {
        path: 'admin/outbox',
        name: 'admin-outbox',
        component: AdminOutboxView,
        meta: { requiresAdmin: true },
      },
      {
        path: ':pathMatch(.*)*',
        name: 'not-found',
        component: NotFoundView,
      },
    ],
  },
]

// Страница-гайд дизайн-системы доступна только в окружении Development.
if (import.meta.env.DEV) {
  routes[0].children?.push({
    path: 'design-system',
    name: 'design-system',
    component: DesignSystemGuide,
  })
}

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

  if (to.meta.requiresAdmin === true) {
    if (!auth.isAuthenticated) {
      return { name: 'login', query: { redirect: to.fullPath } }
    }

    if (!auth.isAdmin) {
      return { name: 'home' }
    }
  }

  return true
})

export default router