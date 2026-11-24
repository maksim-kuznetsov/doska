import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import CategoryStrip from '../CategoryStrip.vue'

const sections = [
  { id: 's1', name: 'Транспорт', subsections: [{ id: 'c1', name: 'Автомобили' }] },
  { id: 's2', name: 'Недвижимость', subsections: [] },
]

const routes: RouteRecordRaw[] = [{ path: '/', name: 'home', component: { template: '<div />' } }]

async function mountStrip(items = sections) {
  return mountWithRouter({
    component: CategoryStrip,
    routes,
    path: '/',
    props: { sections: items },
  })
}

/** Задаёт метрики прокрутки элемента списка, которые jsdom не вычисляет. */
function setMetrics(
  element: Element,
  metrics: { scrollLeft: number; clientWidth: number; scrollWidth: number },
): void {
  Object.defineProperty(element, 'scrollLeft', {
    value: metrics.scrollLeft,
    configurable: true,
    writable: true,
  })
  Object.defineProperty(element, 'clientWidth', {
    value: metrics.clientWidth,
    configurable: true,
  })
  Object.defineProperty(element, 'scrollWidth', {
    value: metrics.scrollWidth,
    configurable: true,
  })
}

describe('CategoryStrip', () => {
  beforeEach(() => {
    class ResizeObserverStub {
      observe(): void {}
      unobserve(): void {}
      disconnect(): void {}
    }

    vi.stubGlobal('ResizeObserver', ResizeObserverStub)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

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

  it('не гасит края, когда разделы помещаются целиком', async () => {
    const wrapper = await mountStrip()
    const list = wrapper.find('.category-strip__list')

    setMetrics(list.element, { scrollLeft: 0, clientWidth: 800, scrollWidth: 800 })
    await list.trigger('scroll')

    expect(list.classes()).not.toContain('category-strip__list--fade-start')
    expect(list.classes()).not.toContain('category-strip__list--fade-end')
  })

  it('гасит правый край в начале и левый — после прокрутки до конца', async () => {
    const wrapper = await mountStrip()
    const list = wrapper.find('.category-strip__list')

    setMetrics(list.element, { scrollLeft: 0, clientWidth: 800, scrollWidth: 1200 })
    await list.trigger('scroll')

    expect(list.classes()).toContain('category-strip__list--fade-end')
    expect(list.classes()).not.toContain('category-strip__list--fade-start')

    setMetrics(list.element, { scrollLeft: 400, clientWidth: 800, scrollWidth: 1200 })
    await list.trigger('scroll')

    expect(list.classes()).toContain('category-strip__list--fade-start')
    expect(list.classes()).not.toContain('category-strip__list--fade-end')
  })
})
