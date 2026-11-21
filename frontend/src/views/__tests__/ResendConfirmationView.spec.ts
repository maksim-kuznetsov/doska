import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

const { resendConfirmation } = vi.hoisted(() => ({ resendConfirmation: vi.fn() }))

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
      me: () => Promise.resolve(null),
      resendConfirmation: (email: string) => resendConfirmation(email),
    },
  },
}))

import { ApiError } from '../../api/client'
import { useAuthStore } from '../../stores/auth'
import ResendConfirmationView from '../ResendConfirmationView.vue'

async function mountView(userEmail?: string) {
  const pinia = createPinia()
  setActivePinia(pinia)

  const auth = useAuthStore()
  if (userEmail !== undefined) {
    auth.user = {
      id: 'user-1',
      email: userEmail,
      role: 'User',
      isTrusted: false,
      isEmailConfirmed: false,
    }
  }

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/login', name: 'login', component: { template: '<div />' } },
      {
        path: '/resend-confirmation',
        name: 'resend-confirmation',
        component: ResendConfirmationView,
      },
    ],
  })

  await router.push('/resend-confirmation')
  await router.isReady()

  const wrapper = mount(ResendConfirmationView, { global: { plugins: [pinia, router] } })

  return { wrapper, auth }
}

describe('ResendConfirmationView', () => {
  beforeEach(() => {
    resendConfirmation.mockReset()
    resendConfirmation.mockResolvedValue(undefined)
  })

  it('подставляет email вошедшего пользователя и отправляет письмо', async () => {
    const { wrapper } = await mountView('user@example.com')

    const input = wrapper.find('#resend-confirmation-email').element as HTMLInputElement
    expect(input.value).toBe('user@example.com')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(resendConfirmation).toHaveBeenCalledWith('user@example.com')
    expect(wrapper.text()).toContain('Если адрес зарегистрирован и ещё не подтверждён, письмо отправлено.')
  })

  it('показывает ошибку ApiError', async () => {
    resendConfirmation.mockRejectedValue(
      new ApiError(429, 'Слишком много запросов. Попробуйте позже.'),
    )

    const { wrapper } = await mountView()

    await wrapper.find('#resend-confirmation-email').setValue('user@example.com')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('Слишком много запросов. Попробуйте позже.')
  })
})
