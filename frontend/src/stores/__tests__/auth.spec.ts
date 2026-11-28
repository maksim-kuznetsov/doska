import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'

const { me, providers, updateProfile } = vi.hoisted(() => ({
  me: vi.fn(),
  providers: vi.fn(),
  updateProfile: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: {
      me: () => me(),
      providers: () => providers(),
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    },
    account: {
      updateProfile: (displayName: string | null, contacts: string | null) =>
        updateProfile(displayName, contacts),
    },
  }),
)

import { useAuthStore } from '../auth'

const googleProvider = { id: 'google', name: 'Google', enabled: true }

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    me.mockReset()
    providers.mockReset()
    updateProfile.mockReset()
  })

  it('загружает пользователя и список провайдеров параллельно', async () => {
    const store = useAuthStore()
    const user = {
      id: 'user-1',
      email: 'user@example.com',
      role: 'User' as const,
      isTrusted: false,
      isEmailConfirmed: true,
      displayName: null,
      contacts: null,
      hasPassword: false,
      linkedProviders: ['google'],
    }

    me.mockResolvedValue(user)
    providers.mockResolvedValue({ providers: [googleProvider] })

    await store.load()

    expect(store.user).toEqual(user)
    expect(store.providers).toEqual([googleProvider])
    expect(store.loaded).toBe(true)
  })

  it('не ломает загрузку сессии при недоступном списке провайдеров', async () => {
    const store = useAuthStore()
    const user = {
      id: 'user-1',
      email: 'user@example.com',
      role: 'User' as const,
      isTrusted: false,
      isEmailConfirmed: true,
      displayName: null,
      contacts: null,
      hasPassword: true,
      linkedProviders: [],
    }

    me.mockResolvedValue(user)
    providers.mockRejectedValue(new Error('network'))

    await store.load()

    expect(store.user).toEqual(user)
    expect(store.providers).toEqual([])
  })

  it('обновляет пользователя из ответа после сохранения профиля', async () => {
    const store = useAuthStore()
    store.user = {
      id: 'user-1',
      email: 'user@example.com',
      role: 'User',
      isTrusted: false,
      isEmailConfirmed: true,
      displayName: null,
      contacts: null,
      hasPassword: true,
      linkedProviders: [],
    }

    const updated = { ...store.user, displayName: 'Иван', contacts: '+7 900 000-00-00' }
    updateProfile.mockResolvedValue(updated)

    await store.updateProfile('Иван', '+7 900 000-00-00')

    expect(updateProfile).toHaveBeenCalledWith('Иван', '+7 900 000-00-00')
    expect(store.user).toEqual(updated)
  })
})
