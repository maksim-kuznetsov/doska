import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
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

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/ads/:id', name: 'item-detail', component: { template: '<div />' } },
]

async function mountCard() {
  return mountWithRouter({
    component: ItemCard,
    routes,
    path: '/',
    props: { ad },
  })
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
