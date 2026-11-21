import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

const { confirmEmail } = vi.hoisted(() => ({ confirmEmail: vi.fn() }))

vi.mock('../../api/client', () => ({
  ApiError: class ApiError extends Error {
    readonly status: number

    constructor(status: number, message: string) {
      super(message)
      this.status = status
    }
  },
  api: {
    auth: {
      confirmEmail: (token: string) => confirmEmail(token),
    },
  },
}))

import { ApiError } from '../../api/client'
import ConfirmEmailView from '../ConfirmEmailView.vue'

async function mountView(token: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/ads/mine', name: 'ads-mine', component: { template: '<div />' } },
      {
        path: '/resend-confirmation',
        name: 'resend-confirmation',
        component: { template: '<div />' },
      },
      { path: '/confirm-email', name: 'confirm-email', component: ConfirmEmailView },
    ],
  })

  await router.push(`/confirm-email?token=${token}`)
  await router.isReady()

  return mount(ConfirmEmailView, { global: { plugins: [router] } })
}

describe('ConfirmEmailView', () => {
  beforeEach(() => {
    confirmEmail.mockReset()
    confirmEmail.mockResolvedValue(undefined)
  })

  it('предлагает запросить новое письмо при ошибке подтверждения', async () => {
    confirmEmail.mockRejectedValue(
      new ApiError(400, 'Ссылка подтверждения недействительна или устарела.'),
    )

    const wrapper = await mountView('broken-token')
    await flushPromises()

    const link = wrapper
      .findAll('a')
      .find(candidate => candidate.text().includes('Запросить новое письмо'))

    expect(link).toBeDefined()
    expect(link!.attributes('href')).toBe('/resend-confirmation')
  })

  it('не показывает повторную отправку после успешного подтверждения', async () => {
    const wrapper = await mountView('valid-token')
    await flushPromises()

    expect(wrapper.text()).toContain('Email подтверждён')
    expect(wrapper.text()).not.toContain('Запросить новое письмо')
  })
})
