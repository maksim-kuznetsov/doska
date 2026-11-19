import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'

import CategoryStrip from '../CategoryStrip.vue'

const sections = [
  { id: 's1', name: 'Транспорт', subsections: [{ id: 'c1', name: 'Автомобили' }] },
  { id: 's2', name: 'Недвижимость', subsections: [] },
]

async function mountStrip(items = sections) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', name: 'home', component: { template: '<div />' } }],
  })

  await router.push('/')
  await router.isReady()

  return mount(CategoryStrip, {
    props: { sections: items },
    global: { plugins: [router] },
  })
}

describe('CategoryStrip', () => {
  it('показывает ссылку на каждый раздел с фильтром ленты', async () => {
    const wrapper = await mountStrip()
    const links = wrapper.findAll('.category-strip__link')

    expect(links).toHaveLength(sections.length + 1)
    expect(links[1].attributes('href')).toBe('/?categoryId=s1')
    expect(links[2].attributes('href')).toBe('/?categoryId=s2')
  })

  it('выводит разделы одной прокручиваемой строкой без переноса', async () => {
    const wrapper = await mountStrip()

    expect(wrapper.find('.category-strip__list').exists()).toBe(true)
  })
})
