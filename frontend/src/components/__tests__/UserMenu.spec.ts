import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import UserMenu from '../UserMenu.vue'

const routes: RouteRecordRaw[] = [{ path: '/', name: 'home', component: { template: '<div />' } }]

async function mountMenu() {
  return mountWithRouter({
    component: UserMenu,
    routes,
    path: '/',
    props: { email: 'admin@example.com', canModerate: true, isAdmin: true },
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
