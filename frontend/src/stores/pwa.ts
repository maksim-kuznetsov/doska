import { defineStore } from 'pinia'
import { computed, ref, shallowRef } from 'vue'

import {
  isIosDevice,
  isSecureContextAvailable,
  isStandaloneMode,
} from '../pwa/environment'

export interface BeforeInstallPromptEvent extends Event {
  readonly platforms: string[]
  readonly userChoice: Promise<{
    outcome: 'accepted' | 'dismissed'
    platform: string
  }>
  prompt(): Promise<void>
}

export type InstallPromptResult = 'accepted' | 'dismissed' | 'unavailable'
export type UpdateServiceWorker = (reloadPage?: boolean) => Promise<void>

const INSTALL_DISMISSED_KEY = 'doska:pwa:install-dismissed'
const UPDATE_DISMISSED_KEY = 'doska:pwa:update-dismissed'

function readSessionFlag(key: string): boolean {
  try {
    return window.sessionStorage.getItem(key) === '1'
  } catch {
    return false
  }
}

function writeSessionFlag(key: string): void {
  try {
    window.sessionStorage.setItem(key, '1')
  } catch {
    // Приватный режим может запрещать sessionStorage — предложение просто не запоминается.
  }
}

/** Состояние установки, обновления и сетевого режима приложения. */
export const usePwaStore = defineStore('pwa', () => {
  const needRefresh = ref(false)
  const offlineReady = ref(false)
  const installEvent = shallowRef<BeforeInstallPromptEvent | null>(null)
  const updateHandler = shallowRef<UpdateServiceWorker | null>(null)
  const isInstalled = ref(isStandaloneMode())
  const isIos = ref(isIosDevice())
  const isOnline = ref(typeof navigator === 'undefined' ? true : navigator.onLine)
  const isSecure = ref(isSecureContextAvailable())
  const installDismissed = ref(readSessionFlag(INSTALL_DISMISSED_KEY))
  const updateDismissed = ref(readSessionFlag(UPDATE_DISMISSED_KEY))

  const installAvailable = computed(
    () => isSecure.value
      && installEvent.value !== null
      && !isInstalled.value
      && !installDismissed.value,
  )
  const iosInstallHintAvailable = computed(
    () => isSecure.value && isIos.value && !isInstalled.value && !installDismissed.value,
  )
  const updateAvailable = computed(() => needRefresh.value && !updateDismissed.value)

  let listening = false

  function setOnline(value: boolean): void {
    isOnline.value = value
  }

  function markInstalled(): void {
    isInstalled.value = true
    installEvent.value = null
  }

  function dismissInstall(): void {
    installDismissed.value = true
    writeSessionFlag(INSTALL_DISMISSED_KEY)
  }

  function captureInstallPrompt(event: BeforeInstallPromptEvent): void {
    installEvent.value = event
  }

  async function promptInstall(): Promise<InstallPromptResult> {
    const event = installEvent.value

    if (event === null) {
      return 'unavailable'
    }

    await event.prompt()
    const choice = await event.userChoice

    if (choice.outcome === 'accepted') {
      installEvent.value = null
    }

    return choice.outcome
  }

  function markNeedRefresh(): void {
    needRefresh.value = true
  }

  function markOfflineReady(): void {
    offlineReady.value = true
  }

  function dismissUpdate(): void {
    updateDismissed.value = true
    writeSessionFlag(UPDATE_DISMISSED_KEY)
  }

  function setUpdateHandler(handler: UpdateServiceWorker): void {
    updateHandler.value = handler
  }

  async function applyUpdate(): Promise<boolean> {
    if (updateHandler.value === null) {
      return false
    }

    await updateHandler.value(true)
    needRefresh.value = false

    return true
  }

  function handleBeforeInstallPrompt(event: Event): void {
    const promptEvent = event as BeforeInstallPromptEvent
    promptEvent.preventDefault()
    captureInstallPrompt(promptEvent)
  }

  function handleOnline(): void {
    setOnline(true)
  }

  function handleOffline(): void {
    setOnline(false)
  }

  function handleDisplayModeChange(event: MediaQueryListEvent): void {
    isInstalled.value = event.matches
  }

  /**
   * Подписывается на события браузера. Вызывается один раз при запуске
   * приложения; повторные вызовы ничего не делают.
   */
  function listen(): void {
    if (listening) {
      return
    }

    listening = true
    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    window.addEventListener('appinstalled', markInstalled)
    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    if (typeof window.matchMedia === 'function') {
      const media = window.matchMedia('(display-mode: standalone)')

      if (typeof media.addEventListener === 'function') {
        media.addEventListener('change', handleDisplayModeChange)
      }
    }
  }

  return {
    needRefresh,
    offlineReady,
    installEvent,
    isInstalled,
    isIos,
    isOnline,
    isSecure,
    installDismissed,
    updateDismissed,
    installAvailable,
    iosInstallHintAvailable,
    updateAvailable,
    listen,
    setOnline,
    markInstalled,
    dismissInstall,
    captureInstallPrompt,
    promptInstall,
    markNeedRefresh,
    markOfflineReady,
    dismissUpdate,
    setUpdateHandler,
    applyUpdate,
  }
})