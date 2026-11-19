import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const { list } = vi.hoisted(() => ({ list: vi.fn() }))

vi.mock('../../api/client', () => ({
  ApiError: class ApiError extends Error {
    readonly status = 0
  },
  api: {
    adminCities: {
      list: () => list(),
      create: vi.fn(),
      rename: vi.fn(),
      setActive: vi.fn(),
      remove: vi.fn(),
    },
  },
}))

import router from '../../router'
import AdminCitiesView from '../AdminCitiesView.vue'

function mountView() {
  return mount(AdminCitiesView, { global: { plugins: [router] } })
}

describe('AdminCitiesView', () => {
  beforeEach(() => {
    list.mockReset()
  })

  it('показывает города администратора со статусом', async () => {
    list.mockResolvedValue([
      { id: 'city-1', name: 'Москва', isActive: true },
      { id: 'city-2', name: 'Тверь', isActive: false },
    ])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Москва')
    expect(wrapper.text()).toContain('Тверь')
    expect(wrapper.text()).toContain('Доступен при подаче')
    expect(wrapper.text()).toContain('Скрыт из выбора')
    expect(wrapper.find('.breadcrumbs').text()).toContain('Города')
  })

  it('сообщает о пустом справочнике', async () => {
    list.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Городов пока нет')
  })
})