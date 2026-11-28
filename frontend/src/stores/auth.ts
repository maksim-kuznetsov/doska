import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { api, type AuthProviderInfo, type CurrentUser } from '../api/client'

/** Состояние текущей сессии: пользователь загружается с сервера, не хранится в localStorage. */
export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUser | null>(null)
  const loaded = ref(false)
  const providers = ref<AuthProviderInfo[]>([])

  const isAuthenticated = computed(() => user.value !== null)
  const canModerate = computed(
    () => user.value?.role === 'Moderator' || user.value?.role === 'Admin',
  )
  const isAdmin = computed(() => user.value?.role === 'Admin')

  async function load(): Promise<void> {
    const [userResult, providersResult] = await Promise.allSettled([
      api.auth.me(),
      api.auth.providers(),
    ])

    user.value = userResult.status === 'fulfilled' ? userResult.value : null
    providers.value =
      providersResult.status === 'fulfilled' ? providersResult.value.providers : []
    loaded.value = true
  }

  async function login(email: string, password: string): Promise<void> {
    user.value = await api.auth.login(email, password)
    loaded.value = true
  }

  async function register(email: string, password: string): Promise<void> {
    user.value = await api.auth.register(email, password)
    loaded.value = true
  }

  async function logout(): Promise<void> {
    await api.auth.logout()
    user.value = null
  }

  async function updateProfile(displayName: string | null, contacts: string | null): Promise<void> {
    user.value = await api.account.updateProfile(displayName, contacts)
  }

  return {
    user,
    loaded,
    providers,
    isAuthenticated,
    canModerate,
    isAdmin,
    load,
    login,
    register,
    logout,
    updateProfile,
  }
})