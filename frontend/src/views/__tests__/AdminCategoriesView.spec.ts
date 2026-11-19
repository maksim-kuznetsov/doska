import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

const { tree } = vi.hoisted(() => ({ tree: vi.fn() }))

vi.mock('../../api/client', () => ({
  ApiError: class ApiError extends Error {
    readonly status = 0
  },
  api: {
    adminCategories: {
      tree: () => tree(),
      createSection: vi.fn(),
      createSubsection: vi.fn(),
      update: vi.fn(),
      setActive: vi.fn(),
      remove: vi.fn(),
    },
  },
}))

import router from '../../router'
import AdminCategoriesView from '../AdminCategoriesView.vue'

describe('AdminCategoriesView', () => {
  it('показывает разделы и цепочку крошек', async () => {
    tree.mockResolvedValue([
      {
        id: 'section-1',
        name: 'Транспорт',
        sortOrder: 1,
        isActive: true,
        subsections: [{ id: 'sub-1', name: 'Автомобили', sortOrder: 0, isActive: true }],
      },
    ])

    const wrapper = mount(AdminCategoriesView, { global: { plugins: [router] } })
    await flushPromises()

    expect(wrapper.find('.breadcrumbs').text()).toContain('Категории')
    expect(wrapper.text()).toContain('Транспорт')
    expect(wrapper.text()).toContain('Автомобили')
  })
})
