import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'

import UserMenu from '../UserMenu.vue'

async function mountMenu() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', name: 'home', component: { template: '<div />' } }],
  })

  await router.push('/')
  await router.isReady()

  return mount(UserMenu, {
    props: { email: 'admin@example.com', canModerate: true, isAdmin: true },
    global: { plugins: [router] },
  })
}

describe('UserMenu', () => {
  it('закрывает меню и сообщает о выходе', async () => {
    const wrapper = await mountMenu()

    await wrapper.get('.dropdown__trigger').trigger('click')
    expect(wrapper.find('#user-menu-panel').exists()).toBe(true)

    const logout = wrapper.findAll('button').find((button) => button.text() === 'Выйти')
    expect(logout).toBeDefined()

    await logout!.trigger('click')

    expect(wrapper.emitted('logout')).toHaveLength(1)
    expect(wrapper.find('#user-menu-panel').exists()).toBe(false)
  })
})
