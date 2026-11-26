import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { get, update } = vi.hoisted(() => ({ get: vi.fn(), update: vi.fn() }))

vi.mock('../../api/client', () =>
  createApiClientMock({
    adminUsers: {
      get: (id: string) => get(id),
      update: (id: string, payload: unknown) => update(id, payload),
    },
  }),
)

import AdminUserEditView from '../AdminUserEditView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/admin/users', name: 'admin-users', component: { template: '<div />' } },
  { path: '/admin/users/:id', name: 'admin-user-edit', component: AdminUserEditView },
]

function fullUser() {
  return {
    id: 'user-1',
    email: 'user@example.com',
    role: 'User',
    isTrusted: false,
    isBlocked: true,
    isEmailConfirmed: true,
    displayName: 'Иван',
    contacts: '+7 999 000-00-00',
  }
}

async function mountView() {
  return mountWithRouter({
    component: AdminUserEditView,
    routes,
    path: '/admin/users/user-1',
  })
}

describe('AdminUserEditView', () => {
  beforeEach(() => {
    get.mockReset()
    update.mockReset()
  })

  it('показывает текущие свойства пользователя', async () => {
    get.mockResolvedValue(fullUser())

    const wrapper = await mountView()
    await flushPromises()

    expect(get).toHaveBeenCalledWith('user-1')

    const email = wrapper.find('#admin-user-email').element as HTMLInputElement
    expect(email.value).toBe('user@example.com')
    expect(email.disabled).toBe(true)

    expect((wrapper.find('#admin-user-role').element as HTMLSelectElement).value).toBe('User')
    expect((wrapper.find('#admin-user-display-name').element as HTMLInputElement).value).toBe('Иван')
    expect((wrapper.find('#admin-user-contacts').element as HTMLTextAreaElement).value).toBe(
      '+7 999 000-00-00',
    )
    expect((wrapper.find('#admin-user-blocked').element as HTMLInputElement).checked).toBe(true)
    expect((wrapper.find('#admin-user-trusted').element as HTMLInputElement).checked).toBe(false)
    expect((wrapper.find('#admin-user-email-confirmed').element as HTMLInputElement).checked).toBe(
      true,
    )
  })

  it('сохраняет изменённые свойства', async () => {
    get.mockResolvedValue(fullUser())
    update.mockResolvedValue({
      ...fullUser(),
      role: 'Moderator',
      isTrusted: true,
      isBlocked: false,
      displayName: 'Пётр',
    })

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('#admin-user-role').setValue('Moderator')
    await wrapper.find('#admin-user-trusted').setValue(true)
    await wrapper.find('#admin-user-blocked').setValue(false)
    await wrapper.find('#admin-user-display-name').setValue('Пётр')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(update).toHaveBeenCalledWith('user-1', {
      role: 'Moderator',
      isTrusted: true,
      isBlocked: false,
      isEmailConfirmed: true,
      displayName: 'Пётр',
      contacts: '+7 999 000-00-00',
    })
    expect(wrapper.text()).toContain('Изменения сохранены.')
  })

  it('показывает ошибку, если пользователь не найден', async () => {
    get.mockRejectedValue(new Error('not found'))

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Не удалось загрузить пользователя.')
  })
})
