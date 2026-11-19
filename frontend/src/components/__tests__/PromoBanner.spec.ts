import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'

import PromoBanner from '../PromoBanner.vue'

async function mountBanner() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/ads/new', name: 'ad-new', component: { template: '<div />' } },
    ],
  })

  await router.push('/')
  await router.isReady()

  return mount(PromoBanner, { global: { plugins: [router] } })
}

describe('PromoBanner', () => {
  it('показывает заголовок и ведёт на подачу объявления', async () => {
    const wrapper = await mountBanner()

    expect(wrapper.text()).toContain('Подайте объявление')
    expect(wrapper.find('a').attributes('href')).toBe('/ads/new')
  })
})
