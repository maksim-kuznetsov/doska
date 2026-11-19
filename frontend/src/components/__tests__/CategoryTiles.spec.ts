import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'

import CategoryTiles from '../CategoryTiles.vue'

const sections = [
  {
    id: 's1',
    name: 'Транспорт',
    subsections: [
      { id: 'c1', name: 'Автомобили' },
      { id: 'c2', name: 'Мотоциклы' },
    ],
  },
  {
    id: 's2',
    name: 'Недвижимость',
    subsections: [{ id: 'c3', name: 'Квартиры' }],
  },
]

async function mountTiles(items = sections) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', name: 'home', component: { template: '<div />' } }],
  })

  await router.push('/')
  await router.isReady()

  return mount(CategoryTiles, {
    props: { sections: items },
    global: { plugins: [router] },
  })
}

describe('CategoryTiles', () => {
  it('ведёт в ленту по разделу и подразделу', async () => {
    const wrapper = await mountTiles()

    expect(wrapper.find('.category-tile__head').attributes('href')).toBe('/?categoryId=s1')
    expect(wrapper.findAll('.category-tile__link')[0].attributes('href')).toBe('/?categoryId=c1')
  })

  it('сообщает, если разделов нет', async () => {
    const wrapper = await mountTiles([])

    expect(wrapper.text()).toContain('Разделы пока не добавлены')
  })
})
