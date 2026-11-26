import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { get, cities, sections } = vi.hoisted(() => ({
  get: vi.fn(),
  cities: vi.fn(),
  sections: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    ads: { get: (id: string) => get(id), update: vi.fn() },
    catalog: { cities: () => cities(), sections: () => sections() },
  }),
)

import AdEditView from '../AdEditView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/ads/mine', name: 'ads-mine', component: { template: '<div />' } },
  { path: '/ads/:id/edit', name: 'ad-edit', component: AdEditView },
]

async function mountView() {
  return mountWithRouter({
    component: AdEditView,
    routes,
    path: '/ads/ad-1/edit',
  })
}

describe('AdEditView', () => {
  beforeEach(() => {
    get.mockReset()
    cities.mockReset()
    sections.mockReset()
  })

  it('показывает объявление и цепочку крошек', async () => {
    get.mockResolvedValue({
      id: 'ad-1',
      title: 'Велосипед',
      description: 'Аккуратный городской велосипед.',
      price: 25000,
      cityId: 'city-1',
      categoryId: 'sub-1',
      contacts: '+7 900 000-00-00',
      status: 'Active',
      moderationReason: null,
      createdAt: '2026-11-19T00:00:00Z',
      publishedAt: '2026-11-19T00:00:00Z',
      photos: [],
      isOwn: true,
    })
    cities.mockResolvedValue([{ id: 'city-1', name: 'Москва' }])
    sections.mockResolvedValue([
      { id: 'section-1', name: 'Транспорт', subsections: [{ id: 'sub-1', name: 'Велосипеды' }] },
    ])

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.find('.breadcrumbs').text()).toContain('Мои объявления')
    expect(wrapper.find('.breadcrumbs').text()).not.toContain('Редактировать объявление')
    expect((wrapper.find('#ad-title').element as HTMLInputElement).value).toBe('Велосипед')
  })
})
