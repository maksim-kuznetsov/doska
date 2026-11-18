import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { api, type CurrentUser } from '../api/client'

/** Состояние текущей сессии: пользователь загружается с сервера, не хранится в localStorage. */
export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUser | null>(null)
  const loaded = ref(false)

  const isAuthenticated = computed(() => user.value !== null)
  const canModerate = computed(
    () => user.value?.role === 'Moderator' || user.value?.role === 'Admin',
  )
  const isAdmin = computed(() => user.value?.role === 'Admin')

  async function load(): Promise<void> {
    try {
      user.value = await api.auth.me()
    } catch {
      user.value = null
    } finally {
      loaded.value = true
    }
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

  return {
    user,
    loaded,
    isAuthenticated,
    canModerate,
    isAdmin,
    load,
    login,
    register,
    logout,
  }
})