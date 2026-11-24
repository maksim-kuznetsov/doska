import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { resetPassword } = vi.hoisted(() => ({ resetPassword: vi.fn() }))

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: {
      resetPassword: (token: string, password: string) => resetPassword(token, password),
    },
  }),
)

import ResetPasswordView from '../ResetPasswordView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/login', name: 'login', component: { template: '<div />' } },
  { path: '/forgot-password', name: 'forgot-password', component: { template: '<div />' } },
  { path: '/reset-password', name: 'reset-password', component: ResetPasswordView },
]

async function mountView(token: string) {
  return mountWithRouter({
    component: ResetPasswordView,
    routes,
    path: `/reset-password?token=${token}`,
  })
}

describe('ResetPasswordView', () => {
  beforeEach(() => {
    resetPassword.mockReset()
    resetPassword.mockResolvedValue(undefined)
  })

  it('требует совпадения паролей до отправки', async () => {
    const wrapper = await mountView('abc')

    await wrapper.find('#reset-password').setValue('secret123')
    await wrapper.find('#reset-confirmation').setValue('secret124')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('[data-test="form-errors"]').exists()).toBe(true)
    expect(resetPassword).not.toHaveBeenCalled()
  })

  it('меняет пароль по действующей ссылке', async () => {
    const wrapper = await mountView('abc')

    await wrapper.find('#reset-password').setValue('secret123')
    await wrapper.find('#reset-confirmation').setValue('secret123')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(resetPassword).toHaveBeenCalledWith('abc', 'secret123')
    expect(wrapper.text()).toContain('Пароль изменён')
  })

  it('сообщает об отсутствии кода в ссылке', async () => {
    const wrapper = await mountView('')

    expect(wrapper.text()).toContain('В ссылке нет кода сброса')
    expect(wrapper.find('form').exists()).toBe(false)
  })
})