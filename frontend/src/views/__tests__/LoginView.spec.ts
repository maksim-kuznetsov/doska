import { createPinia } from 'pinia'
import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import LoginView from '../LoginView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/login', name: 'login', component: LoginView },
  { path: '/register', name: 'register', component: { template: '<div />' } },
  { path: '/forgot-password', name: 'forgot-password', component: { template: '<div />' } },
  {
    path: '/resend-confirmation',
    name: 'resend-confirmation',
    component: { template: '<div />' },
  },
  { path: '/terms', name: 'terms', component: { template: '<div />' } },
  { path: '/privacy', name: 'privacy', component: { template: '<div />' } },
]

describe('LoginView', () => {
  it('сообщает о принятии условий и ссылается на документы', async () => {
    const wrapper = await mountWithRouter({
      component: LoginView,
      routes,
      path: '/login',
      plugins: [createPinia()],
    })

    expect(wrapper.text()).toContain('Продолжая, вы принимаете')
    expect(wrapper.get('a[href="/terms"]').text()).toBe('условия использования')
    expect(wrapper.get('a[href="/privacy"]').text()).toBe('политику конфиденциальности')
  })
})