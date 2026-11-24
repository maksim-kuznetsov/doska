import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import CategoryMenu from '../CategoryMenu.vue'

const sections = [
  {
    id: 'section-1',
    name: 'Транспорт',
    subsections: [
      { id: 'sub-1', name: 'Автомобили' },
      { id: 'sub-2', name: 'Мотоциклы' },
    ],
  },
  {
    id: 'section-2',
    name: 'Недвижимость',
    subsections: [],
  },
]

const routes: RouteRecordRaw[] = [{ path: '/', name: 'home', component: { template: '<div />' } }]

async function mountMenu(items = sections) {
  return mountWithRouter({
    component: CategoryMenu,
    routes,
    path: '/',
    props: { sections: items },
  })
}

describe('CategoryMenu', () => {
  it('скрывает дерево до нажатия на бургер', async () => {
    const wrapper = await mountMenu()

    expect(wrapper.find('[data-test="category-tree"]').exists()).toBe(false)
  })

  it('раскрывает разделы и подразделы и ведёт в ленту подраздела', async () => {
    const wrapper = await mountMenu()

    await wrapper.find('button[aria-label="Категории"]').trigger('click')

    const tree = wrapper.find('[data-test="category-tree"]')
    expect(tree.exists()).toBe(true)
    expect(tree.text()).toContain('Транспорт')
    expect(tree.text()).toContain('Автомобили')

    const link = wrapper.find('a')
    expect(link.attributes('href')).toBe('/?categoryId=sub-1')
  })

  it('сообщает об отсутствии категорий', async () => {
    const wrapper = await mountMenu([])

    await wrapper.find('button[aria-label="Категории"]').trigger('click')

    expect(wrapper.text()).toContain('Категорий пока нет')
  })

  it('разделяет группы разделов визуальными разделителями', async () => {
    const wrapper = await mountMenu()

    await wrapper.find('button[aria-label="Категории"]').trigger('click')

    expect(wrapper.findAll('.dropdown__divider')).toHaveLength(sections.length - 1)
  })

  it('скрывает дерево после выбора подраздела', async () => {
    const wrapper = await mountMenu()

    await wrapper.find('button[aria-label="Категории"]').trigger('click')
    await wrapper.find('a').trigger('click')

    expect(wrapper.find('[data-test="category-tree"]').exists()).toBe(false)
  })
})