import { mount, RouterLinkStub } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import Breadcrumbs from '../Breadcrumbs.vue'

describe('Breadcrumbs', () => {
  it('выводит каждый шаг ссылкой с адресом перехода', () => {
    const wrapper = mount(Breadcrumbs, {
      props: {
        items: [
          { label: 'Главная', to: '/' },
          { label: 'Велосипеды', to: '/?categoryId=c1' },
        ],
      },
      global: { stubs: { RouterLink: RouterLinkStub } },
    })

    const links = wrapper.findAllComponents(RouterLinkStub)
    expect(links).toHaveLength(2)
    expect(links[0].text()).toBe('Главная')
    expect(links[0].props('to')).toBe('/')
    expect(links[1].props('to')).toBe('/?categoryId=c1')
    expect(wrapper.find('span[aria-current="page"]').exists()).toBe(false)
  })

  it('выводит разделителей на один меньше числа шагов', () => {
    const wrapper = mount(Breadcrumbs, {
      props: {
        items: [
          { label: 'Главная', to: '/' },
          { label: 'Транспорт', to: '/?categoryId=s1' },
          { label: 'Велосипеды', to: '/ads/1' },
        ],
      },
      global: { stubs: { RouterLink: RouterLinkStub } },
    })

    expect(wrapper.findAll('.breadcrumbs__separator')).toHaveLength(2)
  })

  it('не выводит навигацию без шагов', () => {
    const wrapper = mount(Breadcrumbs, { props: { items: [] } })

    expect(wrapper.find('.breadcrumbs').exists()).toBe(false)
  })
})
