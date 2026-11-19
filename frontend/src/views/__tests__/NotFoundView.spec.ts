import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'

import NotFoundView from '../NotFoundView.vue'

describe('NotFoundView', () => {
  it('показывает пустое состояние со ссылкой на главную', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/', name: 'home', component: { template: '<div />' } }],
    })

    await router.push('/')
    await router.isReady()

    const wrapper = mount(NotFoundView, { global: { plugins: [router] } })

    expect(wrapper.find('.empty-state').text()).toContain('Страница не найдена')
    expect(wrapper.find('a').attributes('href')).toBe('/')
  })
})
