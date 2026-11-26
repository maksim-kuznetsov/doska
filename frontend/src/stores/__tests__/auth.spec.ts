import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'

const { me, updateProfile } = vi.hoisted(() => ({
  me: vi.fn(),
  updateProfile: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: {
      me: () => me(),
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
    updateProfile.mockReset()
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
    }

    const updated = { ...store.user, displayName: 'Иван', contacts: '+7 900 000-00-00' }
    updateProfile.mockResolvedValue(updated)

    await store.updateProfile('Иван', '+7 900 000-00-00')

    expect(updateProfile).toHaveBeenCalledWith('Иван', '+7 900 000-00-00')
    expect(store.user).toEqual(updated)
  })
})
