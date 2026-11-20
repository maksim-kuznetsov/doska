import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import PwaInstallPrompt from '../PwaInstallPrompt.vue'
import PwaUpdatePrompt from '../PwaUpdatePrompt.vue'
import { usePwaStore, type BeforeInstallPromptEvent } from '../../stores/pwa'

function createInstallPromptEvent(): BeforeInstallPromptEvent {
  const prompt = vi.fn().mockResolvedValue(undefined)
  const event = new Event('beforeinstallprompt')

  Object.defineProperties(event, {
    prompt: { value: prompt },
    userChoice: {
      value: Promise.resolve({ outcome: 'accepted', platform: 'web' }),
    },
    platforms: { value: ['web'] },
  })

  return event as unknown as BeforeInstallPromptEvent
}

function buttonByText(wrapper: ReturnType<typeof mount>, text: string) {
  return wrapper.findAll('button').find((button) => button.text() === text)
}

describe('PwaInstallPrompt', () => {
  beforeEach(() => {
    window.sessionStorage.clear()
    setActivePinia(createPinia())
  })

  it('не показывается без события установки', () => {
    const wrapper = mount(PwaInstallPrompt, {
      global: { plugins: [createPinia()] },
    })

    expect(wrapper.find('.message').exists()).toBe(false)
  })

  it('показывает действие установки и вызывает системное предложение', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const pwa = usePwaStore()
    const event = createInstallPromptEvent()
    pwa.captureInstallPrompt(event)

    const wrapper = mount(PwaInstallPrompt, {
      global: { plugins: [pinia] },
    })

    expect(wrapper.text()).toContain('Установить приложение')

    await buttonByText(wrapper, 'Установить приложение')?.trigger('click')
    await flushPromises()

    expect(event.prompt).toHaveBeenCalledOnce()
  })

  it('скрывается до конца сеанса после закрытия', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const pwa = usePwaStore()
    pwa.captureInstallPrompt(createInstallPromptEvent())

    const wrapper = mount(PwaInstallPrompt, {
      global: { plugins: [pinia] },
    })

    await buttonByText(wrapper, 'Не сейчас')?.trigger('click')

    expect(pwa.installAvailable).toBe(false)
    expect(wrapper.find('.message').exists()).toBe(false)
  })

  it('показывает подсказку для iOS без события установки', () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const pwa = usePwaStore()
    pwa.isIos = true

    const wrapper = mount(PwaInstallPrompt, {
      global: { plugins: [pinia] },
    })

    expect(wrapper.text()).toContain('Поделиться')
    expect(wrapper.text()).toContain('На экран „Домой“')
  })

  it('не показывается в установленном приложении', () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const pwa = usePwaStore()
    pwa.captureInstallPrompt(createInstallPromptEvent())
    pwa.markInstalled()

    const wrapper = mount(PwaInstallPrompt, {
      global: { plugins: [pinia] },
    })

    expect(wrapper.find('.message').exists()).toBe(false)
  })
})

describe('PwaUpdatePrompt', () => {
  beforeEach(() => {
    window.sessionStorage.clear()
    setActivePinia(createPinia())
  })

  it('не показывается без новой версии', () => {
    const wrapper = mount(PwaUpdatePrompt, {
      global: { plugins: [createPinia()] },
    })

    expect(wrapper.find('.message').exists()).toBe(false)
  })

  it('применяет обновление по действию «Обновить»', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const pwa = usePwaStore()
    const update = vi.fn().mockResolvedValue(undefined)
    pwa.setUpdateHandler(update)
    pwa.markNeedRefresh()

    const wrapper = mount(PwaUpdatePrompt, {
      global: { plugins: [pinia] },
    })

    expect(wrapper.text()).toContain('Доступна новая версия приложения')

    await buttonByText(wrapper, 'Обновить')?.trigger('click')
    await flushPromises()

    expect(update).toHaveBeenCalledWith(true)
  })

  it('скрывается по действию «Позже»', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    const pwa = usePwaStore()
    pwa.markNeedRefresh()

    const wrapper = mount(PwaUpdatePrompt, {
      global: { plugins: [pinia] },
    })

    await buttonByText(wrapper, 'Позже')?.trigger('click')

    expect(pwa.updateAvailable).toBe(false)
    expect(wrapper.find('.message').exists()).toBe(false)
  })
})