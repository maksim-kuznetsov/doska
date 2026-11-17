import { defineStore } from 'pinia'
import { ref } from 'vue'

/**
 * Состояние оболочки приложения. Пока содержит только идентификацию
 * приложения и базовый путь к API — точку роста для последующих изменений.
 */
export const useAppStore = defineStore('app', () => {
  const appName = ref('Doska')
  const apiBaseUrl = ref('/api')

  return { appName, apiBaseUrl }
})
