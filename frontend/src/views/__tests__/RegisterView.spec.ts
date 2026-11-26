import { createPinia } from 'pinia'
import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import RegisterView from '../RegisterView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/register', name: 'register', component: RegisterView },
  { path: '/login', name: 'login', component: { template: '<div />' } },
  { path: '/terms', name: 'terms', component: { template: '<div />' } },
  { path: '/privacy', name: 'privacy', component: { template: '<div />' } },
]

describe('RegisterView', () => {
  it('сообщает о принятии условий и ссылается на документы', async () => {
    const wrapper = await mountWithRouter({
      component: RegisterView,
      routes,
      path: '/register',
      plugins: [createPinia()],
    })

    expect(wrapper.text()).toContain('Регистрируясь, вы принимаете')
    expect(wrapper.get('a[href="/terms"]').text()).toBe('условия использования')
    expect(wrapper.get('a[href="/privacy"]').text()).toBe('политику конфиденциальности')
  })
})