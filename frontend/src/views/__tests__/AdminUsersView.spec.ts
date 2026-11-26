import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'

const { list } = vi.hoisted(() => ({ list: vi.fn() }))

vi.mock('../../api/client', () =>
  createApiClientMock({
    adminUsers: {
      list: (page: number) => list(page),
    },
  }),
)

import router from '../../router'
import AdminUsersView from '../AdminUsersView.vue'

function mountView() {
  return mount(AdminUsersView, { global: { plugins: [router] } })
}

function pageResult(items: unknown[], totalCount: number, page = 1, pageSize = 20) {
  return { items, totalCount, page, pageSize }
}

function user(id: string, overrides: Record<string, unknown> = {}) {
  return {
    id,
    email: `${id}@example.com`,
    role: 'User',
    isTrusted: false,
    isBlocked: false,
    isEmailConfirmed: true,
    displayName: null,
    contacts: null,
    ...overrides,
  }
}

describe('AdminUsersView', () => {
  beforeEach(() => {
    list.mockReset()
  })

  it('показывает таблицу пользователей со ссылками на редактирование', async () => {
    list.mockResolvedValue(
      pageResult(
        [
          user('user-1', { email: 'user@example.com', displayName: 'Иван', contacts: '+7 999' }),
          user('user-2', {
            email: 'blocked@example.com',
            role: 'Moderator',
            isTrusted: true,
            isBlocked: true,
            isEmailConfirmed: false,
          }),
        ],
        2,
      ),
    )

    const wrapper = mountView()
    await flushPromises()

    expect(list).toHaveBeenCalledWith(1)
    expect(wrapper.find('[data-test="admin-users-table"]').exists()).toBe(true)

    const rows = wrapper.findAll('[data-test="admin-user"]')
    expect(rows).toHaveLength(2)
    expect(wrapper.text()).toContain('user@example.com')
    expect(wrapper.text()).toContain('Иван')
    expect(wrapper.text()).toContain('Модератор')
    expect(rows[0].findAll('.status-badge')).toHaveLength(1)

    const links = wrapper.findAll('a')
    expect(links.some((link) => link.attributes('href') === '/admin/users/user-1')).toBe(true)
    expect(links.some((link) => link.attributes('href') === '/admin/users/user-2')).toBe(true)
  })

  it('листает страницы постраничной навигацией', async () => {
    list.mockResolvedValueOnce(pageResult([user('user-1', { email: 'a@example.com' })], 21, 1, 20))
    list.mockResolvedValueOnce(pageResult([user('user-21', { email: 'z@example.com' })], 21, 2, 20))

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('.pagination').exists()).toBe(true)

    const next = wrapper.findAll('button').find((button) => button.text() === 'Вперёд')
    await next!.trigger('click')
    await flushPromises()

    expect(list).toHaveBeenLastCalledWith(2)
    expect(wrapper.text()).toContain('z@example.com')
  })

  it('показывает пустое состояние, когда пользователей нет', async () => {
    list.mockResolvedValue(pageResult([], 0))

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('[data-test="admin-users-table"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('Пользователей пока нет')
  })
})
