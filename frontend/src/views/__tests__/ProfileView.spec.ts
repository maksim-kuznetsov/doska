import { flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { updateProfile, changePassword } = vi.hoisted(() => ({
  updateProfile: vi.fn(),
  changePassword: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: { me: () => Promise.resolve(null) },
    account: {
      updateProfile: (displayName: string | null, contacts: string | null) =>
        updateProfile(displayName, contacts),
      changePassword: (currentPassword: string, newPassword: string) =>
        changePassword(currentPassword, newPassword),
    },
  }),
)

import { ApiError } from '../../api/client'
import { useAuthStore } from '../../stores/auth'
import ProfileView from '../ProfileView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/profile', name: 'profile', component: ProfileView },
]

async function mountView() {
  const pinia = createPinia()
  setActivePinia(pinia)

  const auth = useAuthStore()
  auth.user = {
    id: 'user-1',
    email: 'user@example.com',
    role: 'User',
    isTrusted: false,
    isEmailConfirmed: true,
    displayName: 'Иван',
    contacts: '+7 900 000-00-00',
  }

  const wrapper = await mountWithRouter({
    component: ProfileView,
    routes,
    path: '/profile',
    plugins: [pinia],
  })

  return { wrapper, auth }
}

describe('ProfileView', () => {
  beforeEach(() => {
    updateProfile.mockReset()
    changePassword.mockReset()
  })

  it('предзаполняет профиль из данных пользователя', async () => {
    const { wrapper } = await mountView()

    expect((wrapper.find('#profile-display-name').element as HTMLInputElement).value).toBe('Иван')
    expect((wrapper.find('#profile-contacts').element as HTMLTextAreaElement).value).toBe(
      '+7 900 000-00-00',
    )
  })

  it('сохраняет профиль и показывает успех', async () => {
    updateProfile.mockResolvedValue({
      id: 'user-1',
      email: 'user@example.com',
      role: 'User',
      isTrusted: false,
      isEmailConfirmed: true,
      displayName: 'Иван',
      contacts: '+7 900 000-00-00',
    })

    const { wrapper } = await mountView()
    await wrapper.findAll('form')[0].trigger('submit')
    await flushPromises()

    expect(updateProfile).toHaveBeenCalledWith('Иван', '+7 900 000-00-00')
    expect(wrapper.text()).toContain('Профиль сохранён.')
  })

  it('меняет пароль и показывает успех', async () => {
    changePassword.mockResolvedValue(undefined)

    const { wrapper } = await mountView()
    await wrapper.find('#profile-current-password').setValue('oldpass1')
    await wrapper.find('#profile-new-password').setValue('newpass1')
    await wrapper.find('#profile-confirm-password').setValue('newpass1')
    await wrapper.findAll('form')[1].trigger('submit')
    await flushPromises()

    expect(changePassword).toHaveBeenCalledWith('oldpass1', 'newpass1')
    expect(wrapper.text()).toContain('Пароль изменён.')
  })

  it('показывает ошибку смены пароля', async () => {
    changePassword.mockRejectedValue(new ApiError(401, 'Неверный текущий пароль.'))

    const { wrapper } = await mountView()
    await wrapper.find('#profile-current-password').setValue('wrongpass')
    await wrapper.find('#profile-new-password').setValue('newpass1')
    await wrapper.find('#profile-confirm-password').setValue('newpass1')
    await wrapper.findAll('form')[1].trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('Неверный текущий пароль.')
  })
})
