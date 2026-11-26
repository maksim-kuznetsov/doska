import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'

const { list } = vi.hoisted(() => ({ list: vi.fn() }))

vi.mock('../../api/client', () =>
  createApiClientMock({
    adminUsers: {
      list: () => list(),
    },
  }),
)

import router from '../../router'
import AdminUsersView from '../AdminUsersView.vue'

function mountView() {
  return mount(AdminUsersView, { global: { plugins: [router] } })
}

describe('AdminUsersView', () => {
  beforeEach(() => {
    list.mockReset()
  })

  it('показывает пользователей со свойствами и ссылкой на редактирование', async () => {
    list.mockResolvedValue([
      {
        id: 'user-1',
        email: 'user@example.com',
        role: 'User',
        isTrusted: false,
        isBlocked: false,
        isEmailConfirmed: true,
        displayName: 'Иван',
        contacts: '+7 999 000-00-00',
      },
      {
        id: 'user-2',
        email: 'blocked@example.com',
        role: 'Moderator',
        isTrusted: true,
        isBlocked: true,
        isEmailConfirmed: false,
        displayName: null,
        contacts: null,
      },
    ])

    const wrapper = mountView()
    await flushPromises()

    const cards = wrapper.findAll('[data-test="admin-user"]')
    expect(cards).toHaveLength(2)
    expect(wrapper.text()).toContain('user@example.com')
    expect(wrapper.text()).toContain('blocked@example.com')
    expect(wrapper.text()).toContain('Иван')
    expect(wrapper.text()).toContain('Модератор')
    expect(cards[0].findAll('.status-badge')).toHaveLength(1)

    const links = wrapper.findAll('a')
    expect(links.some((link) => link.attributes('href') === '/admin/users/user-1')).toBe(true)
    expect(links.some((link) => link.attributes('href') === '/admin/users/user-2')).toBe(true)
  })

  it('не даёт редактировать свойства прямо в списке', async () => {
    list.mockResolvedValue([
      {
        id: 'user-1',
        email: 'user@example.com',
        role: 'User',
        isTrusted: false,
        isBlocked: false,
        isEmailConfirmed: true,
        displayName: null,
        contacts: null,
      },
    ])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('select').exists()).toBe(false)
    expect(wrapper.findAll('button')).toHaveLength(0)
  })
})
