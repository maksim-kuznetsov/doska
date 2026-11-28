import { flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { me, providers, updateProfile, changePassword } = vi.hoisted(() => ({
  me: vi.fn(),
  providers: vi.fn(),
  updateProfile: vi.fn(),
  changePassword: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: {
      me: () => me(),
      providers: () => providers(),
    },
    account: {
      updateProfile: (displayName: string | null, contacts: string | null) =>
        updateProfile(displayName, contacts),
      changePassword: (currentPassword: string, newPassword: string) =>
        changePassword(currentPassword, newPassword),
    },
  }),
)

import type { AuthProviderInfo } from '../../api/client'
import { ApiError } from '../../api/client'
import { useAuthStore } from '../../stores/auth'
import ProfileView from '../ProfileView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/profile', name: 'profile', component: ProfileView },
]

const googleProvider: AuthProviderInfo = { id: 'google', name: 'Google', enabled: true }
const googleLinked: AuthProviderInfo = { id: 'google', name: 'Google', enabled: true }

const currentUser = {
  id: 'user-1',
  email: 'user@example.com',
  role: 'User' as const,
  isTrusted: false,
  isEmailConfirmed: true,
  displayName: 'Иван',
  contacts: '+7 900 000-00-00',
  hasPassword: true,
  linkedProviders: [] as string[],
}

async function mountView(
  options: {
    path?: string
    providers?: AuthProviderInfo[]
    linkedProviders?: string[]
    hasPassword?: boolean
  } = {},
) {
  const pinia = createPinia()
  setActivePinia(pinia)

  const auth = useAuthStore()
  auth.user = {
    ...currentUser,
    hasPassword: options.hasPassword ?? true,
    linkedProviders: options.linkedProviders ?? [],
  }
  auth.providers = options.providers ?? []
  me.mockResolvedValue({ ...currentUser, hasPassword: true })
  providers.mockResolvedValue({ providers: options.providers ?? [] })

  const wrapper = await mountWithRouter({
    component: ProfileView,
    routes,
    path: options.path ?? '/profile',
    plugins: [pinia],
  })

  return { wrapper, auth }
}

describe('ProfileView', () => {
  beforeEach(() => {
    me.mockReset()
    providers.mockReset()
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
      hasPassword: true,
      linkedProviders: [],
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

  it('предлагает привязать Google при доступном провайдере', async () => {
    const { wrapper } = await mountView({ providers: [googleProvider] })

    const link = wrapper.find('a[href="/api/auth/google/link"]')

    expect(link.exists()).toBe(true)
    expect(link.text()).toContain('Привязать Google')
  })

  it('показывает привязанный Google', async () => {
    const { wrapper } = await mountView({
      providers: [googleLinked],
      linkedProviders: ['google'],
    })

    expect(wrapper.text()).toContain('Google привязан к аккаунту')
    expect(wrapper.find('a[href="/api/auth/google/link"]').exists()).toBe(false)
  })

  it('показывает успех привязки из query', async () => {
    const { wrapper } = await mountView({
      path: '/profile?google=linked',
      providers: [googleProvider],
      linkedProviders: ['google'],
    })
    await flushPromises()

    expect(wrapper.text()).toContain('Google привязан к аккаунту.')
  })

  it('показывает ошибку привязки из query', async () => {
    const { wrapper } = await mountView({
      path: '/profile?google=error&reason=conflict',
      providers: [googleProvider],
    })
    await flushPromises()

    expect(wrapper.text()).toContain('уже привязан к другому аккаунту')
  })

  it('задаёт первый пароль без текущего пароля', async () => {
    changePassword.mockResolvedValue(undefined)

    const { wrapper } = await mountView({ hasPassword: false })
    expect(wrapper.find('#profile-current-password').exists()).toBe(false)

    await wrapper.find('#profile-new-password').setValue('newpass1')
    await wrapper.find('#profile-confirm-password').setValue('newpass1')
    await wrapper.findAll('form')[1].trigger('submit')
    await flushPromises()

    expect(changePassword).toHaveBeenCalledWith('', 'newpass1')
    expect(wrapper.text()).toContain('Пароль задан.')
    expect(wrapper.find('#profile-current-password').exists()).toBe(true)
  })
})
