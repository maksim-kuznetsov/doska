import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const { get, cities, sections } = vi.hoisted(() => ({
  get: vi.fn(),
  cities: vi.fn(),
  sections: vi.fn(),
}))

vi.mock('../../api/client', () => ({
  ApiError: class ApiError extends Error {
    readonly status = 0
  },
  api: {
    ads: { get: (id: string) => get(id), update: vi.fn() },
    catalog: { cities: () => cities(), sections: () => sections() },
  },
}))

import AdEditView from '../AdEditView.vue'

async function mountView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/ads/mine', name: 'ads-mine', component: { template: '<div />' } },
      { path: '/ads/:id/edit', name: 'ad-edit', component: AdEditView },
    ],
  })

  await router.push('/ads/ad-1/edit')
  await router.isReady()

  return mount(AdEditView, { global: { plugins: [router] } })
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
    })
    cities.mockResolvedValue([{ id: 'city-1', name: 'Москва' }])
    sections.mockResolvedValue([
      { id: 'section-1', name: 'Транспорт', subsections: [{ id: 'sub-1', name: 'Велосипеды' }] },
    ])

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.find('.breadcrumbs').text()).toContain('Мои объявления')
    expect(wrapper.find('.breadcrumbs').text()).toContain('Редактировать объявление')
    expect((wrapper.find('#ad-title').element as HTMLInputElement).value).toBe('Велосипед')
  })
})
