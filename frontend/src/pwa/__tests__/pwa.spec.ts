import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { createInstallPromptEvent } from '../../__tests__/helpers/pwa'
import DefaultLayout from '../../layouts/DefaultLayout.vue'
import OfflineApp from '../OfflineApp.vue'
import {
  isIosDevice,
  isSecureContextAvailable,
  isStandaloneMode,
  shouldRegisterServiceWorker,
} from '../environment'
import { usePwaStore } from '../../stores/pwa'

vi.mock('../../components/SiteHeader.vue', () => ({
  default: { name: 'SiteHeader', template: '<header />' },
}))

vi.mock('../../components/SiteFooter.vue', () => ({
  default: { name: 'SiteFooter', template: '<footer />' },
}))

describe('Окружение PWA', () => {
  it('определяет iOS и iPadOS', () => {
    expect(isIosDevice('Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X)', 5)).toBe(true)
    expect(isIosDevice('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)', 5)).toBe(true)
    expect(isIosDevice('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)', 0)).toBe(false)
    expect(isIosDevice('Mozilla/5.0 (Windows NT 10.0; Win64; x64)', 0)).toBe(false)
  })

  it('определяет режим standalone', () => {
    expect(isStandaloneMode(true, false)).toBe(true)
    expect(isStandaloneMode(false, true)).toBe(true)
    expect(isStandaloneMode(false, false)).toBe(false)
  })

  it('определяет защищённый контекст', () => {
    expect(isSecureContextAvailable(true)).toBe(true)
    expect(isSecureContextAvailable(false, 'example.com')).toBe(false)
    expect(isSecureContextAvailable(false, 'localhost')).toBe(true)
    expect(isSecureContextAvailable(false, '127.0.0.1')).toBe(true)
  })

  it('регистрирует service worker только в production и защищённом контексте', () => {
    expect(
      shouldRegisterServiceWorker({ production: true, secure: true, supported: true }),
    ).toBe(true)
    expect(
      shouldRegisterServiceWorker({ production: false, secure: true, supported: true }),
    ).toBe(false)
    expect(
      shouldRegisterServiceWorker({ production: true, secure: false, supported: true }),
    ).toBe(false)
    expect(
      shouldRegisterServiceWorker({ production: true, secure: true, supported: false }),
    ).toBe(false)
  })
})

describe('Состояние PWA', () => {
  beforeEach(() => {
    window.sessionStorage.clear()
    setActivePinia(createPinia())
  })

  it('отражает состояние сети', () => {
    const pwa = usePwaStore()

    pwa.setOnline(false)
    expect(pwa.isOnline).toBe(false)

    pwa.setOnline(true)
    expect(pwa.isOnline).toBe(true)
  })

  it('показывает установку после beforeinstallprompt и скрывает после согласия', async () => {
    const pwa = usePwaStore()
    const event = createInstallPromptEvent('accepted')

    pwa.captureInstallPrompt(event)
    expect(pwa.installAvailable).toBe(true)

    await expect(pwa.promptInstall()).resolves.toBe('accepted')
    expect(event.prompt).toHaveBeenCalledOnce()
    expect(pwa.installAvailable).toBe(false)
  })

  it('оставляет установку доступной при отказе в системном окне', async () => {
    const pwa = usePwaStore()

    pwa.captureInstallPrompt(createInstallPromptEvent('dismissed'))

    await expect(pwa.promptInstall()).resolves.toBe('dismissed')
    expect(pwa.installAvailable).toBe(true)
  })

  it('скрывает установку до конца сеанса после закрытия предложения', () => {
    const pwa = usePwaStore()

    pwa.captureInstallPrompt(createInstallPromptEvent())
    pwa.dismissInstall()

    expect(pwa.installAvailable).toBe(false)
    expect(window.sessionStorage.getItem('doska:pwa:install-dismissed')).toBe('1')
  })

  it('скрывает установку в уже установленном приложении', () => {
    const pwa = usePwaStore()

    pwa.captureInstallPrompt(createInstallPromptEvent())
    pwa.markInstalled()

    expect(pwa.installAvailable).toBe(false)
  })

  it('предлагает обновление и применяет его через обработчик service worker', async () => {
    const pwa = usePwaStore()
    const update = vi.fn().mockResolvedValue(undefined)

    pwa.setUpdateHandler(update)
    pwa.markNeedRefresh()

    expect(pwa.updateAvailable).toBe(true)
    await expect(pwa.applyUpdate()).resolves.toBe(true)
    expect(update).toHaveBeenCalledWith(true)
    expect(pwa.updateAvailable).toBe(false)
  })

  it('скрывает обновление до конца сеанса', () => {
    const pwa = usePwaStore()

    pwa.markNeedRefresh()
    pwa.dismissUpdate()

    expect(pwa.updateAvailable).toBe(false)
    expect(window.sessionStorage.getItem('doska:pwa:update-dismissed')).toBe('1')
  })
})

describe('Офлайн-интерфейс', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('показывает офлайн-страницу с действием «Повторить» и не обращается к API', () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
    const wrapper = mount(OfflineApp)

    expect(wrapper.text()).toContain('Нет подключения к интернету')
    expect(wrapper.get('button').text()).toBe('Повторить')
    expect(fetchSpy).not.toHaveBeenCalled()
  })

  it('показывает и скрывает баннер отсутствия сети в макете', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const pwa = usePwaStore()
    pwa.setOnline(false)

    const wrapper = mount(DefaultLayout, {
      global: {
        plugins: [pinia],
        stubs: { RouterView: true },
      },
    })

    await nextTick()
    expect(wrapper.text()).toContain('Нет подключения к интернету')

    pwa.setOnline(true)
    await nextTick()
    expect(wrapper.text()).not.toContain('Нет подключения к интернету')
  })
})