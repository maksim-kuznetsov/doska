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

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    me.mockReset()
    providers.mockReset()
    updateProfile.mockReset()
  })

  it('загружает пользователя и доступность Google параллельно', async () => {
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
      isGoogleLinked: true,
    }

    me.mockResolvedValue(user)
    providers.mockResolvedValue({ google: true })

    await store.load()

    expect(store.user).toEqual(user)
    expect(store.googleEnabled).toBe(true)
    expect(store.loaded).toBe(true)
  })

  it('не ломает загрузку сессии при недоступном статусе провайдера', async () => {
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
      isGoogleLinked: false,
    }

    me.mockResolvedValue(user)
    providers.mockRejectedValue(new Error('network'))

    await store.load()

    expect(store.user).toEqual(user)
    expect(store.googleEnabled).toBe(false)
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
      isGoogleLinked: false,
    }

    const updated = { ...store.user, displayName: 'Иван', contacts: '+7 900 000-00-00' }
    updateProfile.mockResolvedValue(updated)

    await store.updateProfile('Иван', '+7 900 000-00-00')

    expect(updateProfile).toHaveBeenCalledWith('Иван', '+7 900 000-00-00')
    expect(store.user).toEqual(updated)
  })
})
