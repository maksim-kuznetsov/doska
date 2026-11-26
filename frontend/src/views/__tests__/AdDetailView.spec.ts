import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter, type RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { stubDialogElement } from '../../__tests__/helpers/dialog'

const { get, cities, sections, fileComplaint } = vi.hoisted(() => ({
  get: vi.fn(),
  cities: vi.fn(),
  sections: vi.fn(),
  fileComplaint: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    ads: { get: (id: string) => get(id) },
    catalog: { cities: () => cities(), sections: () => sections() },
    complaints: { file: (adId: string, reason: string) => fileComplaint(adId, reason) },
  }),
)

import { ApiError } from '../../api/client'
import { useAuthStore } from '../../stores/auth'
import AdDetailView from '../AdDetailView.vue'

const activeAd = {
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
  isOwn: false,
}

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/login', name: 'login', component: { template: '<div />' } },
  { path: '/ads/:id', name: 'item-detail', component: AdDetailView },
]

let dialog: ReturnType<typeof stubDialogElement>

async function mountView(options: { authenticated?: boolean } = {}) {
  const pinia = createPinia()
  setActivePinia(pinia)

  const auth = useAuthStore()
  if (options.authenticated) {
    auth.user = {
      id: 'user-1',
      email: 'user@example.com',
      role: 'User',
      isTrusted: false,
      isEmailConfirmed: true,
    }
  }

  const router = createRouter({ history: createMemoryHistory(), routes })
  await router.push('/ads/ad-1')
  await router.isReady()

  const wrapper = mount(AdDetailView, {
    global: { plugins: [pinia, router] },
  })

  return { wrapper, router }
}

describe('AdDetailView', () => {
  beforeEach(() => {
    dialog = stubDialogElement()

    get.mockReset()
    cities.mockReset()
    sections.mockReset()
    fileComplaint.mockReset()

    cities.mockResolvedValue([{ id: 'city-1', name: 'Москва' }])
    sections.mockResolvedValue([
      { id: 'section-1', name: 'Транспорт', subsections: [{ id: 'sub-1', name: 'Велосипеды' }] },
    ])
    fileComplaint.mockResolvedValue(undefined)
  })

  afterEach(() => {
    dialog.restore()
  })

  it('показывает данные активного объявления и галерею', async () => {
    get.mockResolvedValue(activeAd)

    const { wrapper } = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Велосипед')
    expect(wrapper.text()).toContain('25000')
    expect(wrapper.text()).toContain('Велосипеды')
    expect(wrapper.findAll('.gallery__image')).toHaveLength(1)
    const crumbs = wrapper.find('.breadcrumbs')
    expect(crumbs.text()).toContain('Главная')
    expect(crumbs.text()).toContain('Велосипеды')
    expect(crumbs.text()).not.toContain('Транспорт')
    expect(crumbs.text()).not.toContain('Объявление')
  })

  it('показывает пустое состояние для недоступного объявления', async () => {
    get.mockRejectedValue(new ApiError(404, 'Объявление не найдено.'))

    const { wrapper } = await mountView()
    await flushPromises()

    expect(wrapper.find('.empty-state').text()).toContain('Объявление недоступно')
  })

  it('открывает окно просмотра по клику на фотографию', async () => {
    get.mockResolvedValue(activeAd)

    const { wrapper } = await mountView()
    await flushPromises()

    await wrapper.find('.gallery__button').trigger('click')

    expect(wrapper.find('.photo-viewer').exists()).toBe(true)
    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe('/api/media/ad-1/one.png')
  })

  it('показывает автору действие редактирования', async () => {
    get.mockResolvedValue({ ...activeAd, isOwn: true })

    const { wrapper } = await mountView({ authenticated: true })
    await flushPromises()

    const edit = wrapper.findAll('a').find((link) => link.text() === 'Редактировать')
    expect(edit?.attributes('href')).toBe('/ads/ad-1/edit')
  })

  it('не показывает редактирование и предлагает жалобу на чужое объявление', async () => {
    get.mockResolvedValue(activeAd)

    const { wrapper } = await mountView({ authenticated: true })
    await flushPromises()

    expect(wrapper.findAll('a').some((link) => link.text() === 'Редактировать')).toBe(false)
    expect(wrapper.text()).toContain('Пожаловаться')
  })

  it('не показывает жалобу на своё объявление', async () => {
    get.mockResolvedValue({ ...activeAd, isOwn: true })

    const { wrapper } = await mountView({ authenticated: true })
    await flushPromises()

    expect(wrapper.text()).not.toContain('Пожаловаться')
  })

  it('отправляет жалобу с причиной и показывает подтверждение', async () => {
    get.mockResolvedValue(activeAd)

    const { wrapper } = await mountView({ authenticated: true })
    await flushPromises()

    await wrapper.findAll('button').find((button) => button.text() === 'Пожаловаться')?.trigger('click')
    expect(wrapper.find('.modal-dialog').exists()).toBe(true)

    await wrapper.find('#complaint-reason').setValue('Подозрение на мошенничество')
    await wrapper.findAll('.modal-dialog__actions button')[1].trigger('click')
    await flushPromises()

    expect(fileComplaint).toHaveBeenCalledWith('ad-1', 'Подозрение на мошенничество')
    expect(wrapper.text()).toContain('Жалоба отправлена.')
    expect(wrapper.text()).not.toContain('Пожаловаться')
  })

  it('не отправляет жалобу без причины', async () => {
    get.mockResolvedValue(activeAd)

    const { wrapper } = await mountView({ authenticated: true })
    await flushPromises()

    await wrapper.findAll('button').find((button) => button.text() === 'Пожаловаться')?.trigger('click')
    await wrapper.findAll('.modal-dialog__actions button')[1].trigger('click')
    await flushPromises()

    expect(fileComplaint).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Укажите причину жалобы.')
  })

  it('показывает ошибку сервера при подаче жалобы', async () => {
    get.mockResolvedValue(activeAd)
    fileComplaint.mockRejectedValue(new ApiError(409, 'Жалоба на это объявление уже отправлена.'))

    const { wrapper } = await mountView({ authenticated: true })
    await flushPromises()

    await wrapper.findAll('button').find((button) => button.text() === 'Пожаловаться')?.trigger('click')
    await wrapper.find('#complaint-reason').setValue('Причина')
    await wrapper.findAll('.modal-dialog__actions button')[1].trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Жалоба на это объявление уже отправлена.')
  })

  it('направляет гостя на вход при попытке пожаловаться', async () => {
    get.mockResolvedValue(activeAd)

    const { wrapper, router } = await mountView()
    await flushPromises()

    await wrapper.findAll('button').find((button) => button.text() === 'Пожаловаться')?.trigger('click')
    await flushPromises()

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/ads/ad-1')
    expect(wrapper.find('.modal-dialog').exists()).toBe(false)
  })
})
