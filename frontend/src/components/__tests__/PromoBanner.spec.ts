import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import PromoBanner from '../PromoBanner.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/ads/new', name: 'ad-new', component: { template: '<div />' } },
]

async function mountBanner() {
  return mountWithRouter({
    component: PromoBanner,
    routes,
    path: '/',
  })
}

describe('PromoBanner', () => {
  it('показывает заголовок и ведёт на подачу объявления', async () => {
    const wrapper = await mountBanner()

    expect(wrapper.text()).toContain('Подайте объявление')
    expect(wrapper.find('a').attributes('href')).toBe('/ads/new')
  })
})
