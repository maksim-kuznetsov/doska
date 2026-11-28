import { flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { cities, sections, create } = vi.hoisted(() => ({
  cities: vi.fn(),
  sections: vi.fn(),
  create: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    catalog: { cities: () => cities(), sections: () => sections() },
    ads: { create: (formData: FormData) => create(formData) },
  }),
)

import type { CurrentUser } from '../../api/client'
import { useAuthStore } from '../../stores/auth'
import AdFormView from '../AdFormView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/ads/new', name: 'ad-new', component: AdFormView },
  { path: '/ads/mine', name: 'ads-mine', component: { template: '<div />' } },
]

function userWith(displayName: string | null, contacts: string | null): CurrentUser {
  return {
    id: 'user-1',
    email: 'user@example.com',
    role: 'User',
    isTrusted: false,
    isEmailConfirmed: true,
    displayName,
    contacts,
    hasPassword: true,
    linkedProviders: [],
  }
}

async function mountView(user: CurrentUser | null) {
  const pinia = createPinia()
  setActivePinia(pinia)

  const auth = useAuthStore()
  if (user) {
    auth.user = user
  }

  const wrapper = await mountWithRouter({
    component: AdFormView,
    routes,
    path: '/ads/new',
    plugins: [pinia],
  })
  await flushPromises()

  return wrapper
}

function contactsValue(wrapper: Awaited<ReturnType<typeof mountView>>): string {
  return (wrapper.find('#ad-contacts').element as HTMLInputElement).value
}

describe('AdFormView', () => {
  beforeEach(() => {
    cities.mockReset()
    sections.mockReset()
    create.mockReset()
    cities.mockResolvedValue([])
    sections.mockResolvedValue([])
  })

  it('подставляет имя и контакты профиля в формате «имя контакты»', async () => {
    const wrapper = await mountView(userWith('Иван', '+7 900 000-00-00'))

    expect(contactsValue(wrapper)).toBe('Иван +7 900 000-00-00')
  })

  it('подставляет только контакты, если имя не заполнено', async () => {
    const wrapper = await mountView(userWith(null, '+7 900 000-00-00'))

    expect(contactsValue(wrapper)).toBe('+7 900 000-00-00')
  })

  it('оставляет поле пустым без данных профиля', async () => {
    const wrapper = await mountView(userWith(null, null))

    expect(contactsValue(wrapper)).toBe('')
  })

  it('позволяет изменить предзаполненные контакты', async () => {
    const wrapper = await mountView(userWith('Иван', '+7 900 000-00-00'))

    await wrapper.find('#ad-contacts').setValue('Пётр +7 111 111-11-11')

    expect(contactsValue(wrapper)).toBe('Пётр +7 111 111-11-11')
  })
})
