import { createPinia } from 'pinia'
import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import SiteFooter from '../SiteFooter.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/ads/new', name: 'ad-new', component: { template: '<div />' } },
  { path: '/ads/mine', name: 'ads-mine', component: { template: '<div />' } },
  { path: '/privacy', name: 'privacy', component: { template: '<div />' } },
  { path: '/terms', name: 'terms', component: { template: '<div />' } },
  { path: '/design-system', name: 'design-system', component: { template: '<div />' } },
]

describe('SiteFooter', () => {
  it('показывает ссылки на политику и условия использования', async () => {
    const wrapper = await mountWithRouter({
      component: SiteFooter,
      routes,
      path: '/',
      plugins: [createPinia()],
    })

    expect(wrapper.get('a[href="/privacy"]').text()).toBe('Политика конфиденциальности')
    expect(wrapper.get('a[href="/terms"]').text()).toBe('Условия использования')
    expect(wrapper.text()).toContain('Doska')
  })
})