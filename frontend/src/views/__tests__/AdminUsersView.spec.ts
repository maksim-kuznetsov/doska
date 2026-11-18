import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const { list, setRole } = vi.hoisted(() => ({ list: vi.fn(), setRole: vi.fn() }))

vi.mock('../../api/client', () => ({
  ApiError: class ApiError extends Error {
    readonly status = 0
  },
  api: {
    adminUsers: {
      list: () => list(),
      setRole: (id: string, role: string) => setRole(id, role),
    },
    users: {
      block: vi.fn(),
      unblock: vi.fn(),
    },
  },
}))

import AdminUsersView from '../AdminUsersView.vue'

describe('AdminUsersView', () => {
  beforeEach(() => {
    list.mockReset()
    setRole.mockReset()
    setRole.mockResolvedValue(undefined)
  })

  it('показывает пользователей, роли и статусы', async () => {
    list.mockResolvedValue([
      {
        id: 'user-1',
        email: 'user@example.com',
        role: 'User',
        isTrusted: false,
        isBlocked: false,
        isEmailConfirmed: true,
      },
      {
        id: 'user-2',
        email: 'blocked@example.com',
        role: 'Moderator',
        isTrusted: true,
        isBlocked: true,
        isEmailConfirmed: false,
      },
    ])

    const wrapper = mount(AdminUsersView)
    await flushPromises()

    expect(wrapper.findAll('[data-test="admin-user"]')).toHaveLength(2)
    expect(wrapper.text()).toContain('user@example.com')
    expect(wrapper.text()).toContain('blocked@example.com')
    expect(wrapper.text()).toContain('Разблокировать')
  })

  it('сохраняет изменённую роль', async () => {
    list.mockResolvedValue([
      {
        id: 'user-1',
        email: 'user@example.com',
        role: 'User',
        isTrusted: false,
        isBlocked: false,
        isEmailConfirmed: true,
      },
    ])

    const wrapper = mount(AdminUsersView)
    await flushPromises()

    await wrapper.find('select').setValue('Moderator')
    const save = wrapper.findAll('button').find((button) => button.text() === 'Сохранить роль')
    await save!.trigger('click')
    await flushPromises()

    expect(setRole).toHaveBeenCalledWith('user-1', 'Moderator')
  })
})