import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import router from '../../router'
import DesignSystemGuide from '../DesignSystemGuide.vue'

describe('Страница-гайд дизайн-системы', () => {
  it('доступна только в Development по маршруту /design-system', () => {
    const resolved = router.resolve('/design-system')

    if (import.meta.env.DEV) {
      expect(resolved.name).toBe('design-system')
    } else {
      expect(resolved.name).not.toBe('design-system')
    }
  })

  it('показывает токены и компоненты с примерами', () => {
    const wrapper = mount(DesignSystemGuide, { global: { plugins: [router] } })
    const text = wrapper.text()

    expect(text).toContain('Дизайн-система')
    expect(text).toContain('#00aaff')
    expect(wrapper.find('[data-test="form-errors"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="queue-item"]').exists()).toBe(true)
    expect(wrapper.find('#guide-checkbox').exists()).toBe(true)
    expect(text).toContain('Модальный диалог')
  })
})