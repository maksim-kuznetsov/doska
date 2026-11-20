import { registerSW } from 'virtual:pwa-register'

import type { usePwaStore } from '../stores/pwa'
import { shouldRegisterServiceWorker } from './environment'

type PwaStore = ReturnType<typeof usePwaStore>

const UPDATE_INTERVAL_MS = 60 * 60 * 1000

/**
 * Регистрирует service worker и связывает его события с состоянием PWA.
 * В разработке и вне защищённого контекста регистрация не выполняется.
 */
export function registerPwaServiceWorker(pwa: PwaStore): void {
  if (
    !shouldRegisterServiceWorker({
      production: import.meta.env.PROD,
      secure: window.isSecureContext,
      supported: 'serviceWorker' in navigator,
    })
  ) {
    return
  }

  const updateServiceWorker = registerSW({
    immediate: true,
    onNeedRefresh() {
      pwa.markNeedRefresh()
    },
    onOfflineReady() {
      pwa.markOfflineReady()
    },
    onRegisteredSW(_serviceWorkerUrl, registration) {
      if (registration === undefined) {
        return
      }

      // Проверяем обновления после деплоя, не дожидаясь перезапуска браузера.
      window.setInterval(() => {
        void registration.update()
      }, UPDATE_INTERVAL_MS)

      window.addEventListener('focus', () => {
        void registration.update()
      })
    },
    onRegisterError(error) {
      console.warn('Не удалось зарегистрировать service worker', error)
    },
  })

  pwa.setUpdateHandler(updateServiceWorker)
}