import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

const { resetPassword } = vi.hoisted(() => ({ resetPassword: vi.fn() }))

vi.mock('../../api/client', () => ({
  ApiError: class ApiError extends Error {
    readonly status = 0
  },
  api: {
    auth: {
      resetPassword: (token: string, password: string) => resetPassword(token, password),
    },
  },
}))

import ResetPasswordView from '../ResetPasswordView.vue'

async function mountView(token: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/login', name: 'login', component: { template: '<div />' } },
      { path: '/forgot-password', name: 'forgot-password', component: { template: '<div />' } },
      { path: '/reset-password', name: 'reset-password', component: ResetPasswordView },
    ],
  })

  await router.push(`/reset-password?token=${token}`)
  await router.isReady()

  return mount(ResetPasswordView, { global: { plugins: [router] } })
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