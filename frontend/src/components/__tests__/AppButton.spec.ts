import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import AppButton from '../AppButton.vue'

describe('AppButton', () => {
  it('оформляет вариант и размер классами дизайн-системы', () => {
    const wrapper = mount(AppButton, {
      props: { variant: 'primary', size: 'compact' },
      slots: { default: 'Отправить' },
    })

    const button = wrapper.get('button')
    expect(button.classes()).toContain('btn')
    expect(button.classes()).toContain('btn--primary')
    expect(button.classes()).toContain('btn--compact')
    expect(button.text()).toBe('Отправить')
  })

  it('делает недоступную кнопку визуально отличимой', () => {
    const wrapper = mount(AppButton, { props: { disabled: true }, slots: { default: 'Нельзя' } })

    expect(wrapper.get('button').attributes('disabled')).toBeDefined()
  })
})