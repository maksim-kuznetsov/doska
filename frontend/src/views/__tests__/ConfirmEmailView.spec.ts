import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { confirmEmail } = vi.hoisted(() => ({ confirmEmail: vi.fn() }))

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: {
      confirmEmail: (token: string) => confirmEmail(token),
    },
  }),
)

import { ApiError } from '../../api/client'
import ConfirmEmailView from '../ConfirmEmailView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/ads/mine', name: 'ads-mine', component: { template: '<div />' } },
  {
    path: '/resend-confirmation',
    name: 'resend-confirmation',
    component: { template: '<div />' },
  },
  { path: '/confirm-email', name: 'confirm-email', component: ConfirmEmailView },
]

async function mountView(token: string) {
  return mountWithRouter({
    component: ConfirmEmailView,
    routes,
    path: `/confirm-email?token=${token}`,
  })
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
