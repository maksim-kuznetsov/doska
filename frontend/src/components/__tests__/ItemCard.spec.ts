import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'

import ItemCard from '../ItemCard.vue'

const ad = {
  id: 'ad-1',
  title: 'Велосипед',
  description: 'Аккуратный городской велосипед.',
  price: 25000,
  cityName: 'Москва',
  categoryName: 'Велосипеды',
  sectionName: 'Транспорт',
  publishedAt: '2026-11-19T00:00:00Z',
  thumbnailUrl: null,
}

async function mountCard() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/ads/:id', name: 'item-detail', component: { template: '<div />' } },
    ],
  })

  await router.push('/')
  await router.isReady()

  return mount(ItemCard, { props: { ad }, global: { plugins: [router] } })
}

describe('ItemCard', () => {
  it('ведёт на публичную страницу объявления', async () => {
    const wrapper = await mountCard()

    expect(wrapper.element.tagName).toBe('A')
    expect(wrapper.attributes('href')).toBe('/ads/ad-1')
  })

  it('показывает плейсхолдер вместо фото', async () => {
    const wrapper = await mountCard()

    expect(wrapper.text()).toContain('Нет фото')
  })
})
