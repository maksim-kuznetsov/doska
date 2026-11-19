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
    readonly status: number

    constructor(status: number, message: string) {
      super(message)
      this.status = status
    }
  },
  api: {
    ads: { get: (id: string) => get(id) },
    catalog: { cities: () => cities(), sections: () => sections() },
  },
}))

import { ApiError } from '../../api/client'
import AdDetailView from '../AdDetailView.vue'

async function mountView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/ads/:id', name: 'ad-detail', component: AdDetailView },
    ],
  })

  await router.push('/ads/ad-1')
  await router.isReady()

  return mount(AdDetailView, { global: { plugins: [router] } })
}

describe('AdDetailView', () => {
  beforeEach(() => {
    get.mockReset()
    cities.mockReset()
    sections.mockReset()

    cities.mockResolvedValue([{ id: 'city-1', name: 'Москва' }])
    sections.mockResolvedValue([
      { id: 'section-1', name: 'Транспорт', subsections: [{ id: 'sub-1', name: 'Велосипеды' }] },
    ])
  })

  it('показывает данные активного объявления и галерею', async () => {
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
      photos: [
        {
          id: 'photo-1',
          url: '/api/media/ad-1/one.png',
          thumbnailUrl: '/api/media/ad-1/one_thumb.jpg',
          contentType: 'image/png',
          byteSize: 128,
          width: 8,
          height: 8,
          sortOrder: 1,
        },
      ],
    })

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Велосипед')
    expect(wrapper.text()).toContain('25000')
    expect(wrapper.text()).toContain('Велосипеды')
    expect(wrapper.findAll('.gallery__image')).toHaveLength(1)
    expect(wrapper.find('.breadcrumbs').text()).toContain('Транспорт')
  })

  it('показывает пустое состояние для недоступного объявления', async () => {
    get.mockRejectedValue(new ApiError(404, 'Объявление не найдено.'))

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.find('.empty-state').text()).toContain('Объявление недоступно')
  })
})
