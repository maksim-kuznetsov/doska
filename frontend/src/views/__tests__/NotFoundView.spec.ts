import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import NotFoundView from '../NotFoundView.vue'

const routes: RouteRecordRaw[] = [{ path: '/', name: 'home', component: { template: '<div />' } }]

describe('NotFoundView', () => {
  it('показывает пустое состояние со ссылкой на главную', async () => {
    const wrapper = await mountWithRouter({
      component: NotFoundView,
      routes,
      path: '/',
    })

    expect(wrapper.find('.empty-state').text()).toContain('Страница не найдена')
    expect(wrapper.find('a').attributes('href')).toBe('/')
  })
})
