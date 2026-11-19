import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import PriceRange from '../PriceRange.vue'

describe('PriceRange', () => {
  it('показывает диапазон единым контролом с валютой', () => {
    const wrapper = mount(PriceRange, { props: { min: '100', max: '500' } })

    expect(wrapper.find('.price-range').exists()).toBe(true)
    expect(wrapper.text()).toContain('₽')
    expect(wrapper.findAll('input')).toHaveLength(2)
  })

  it('подсказывает границы диапазона плейсхолдерами', () => {
    const wrapper = mount(PriceRange, { props: { min: '', max: '' } })
    const inputs = wrapper.findAll('input')

    expect(inputs[0].attributes('placeholder')).toBe('от')
    expect(inputs[1].attributes('placeholder')).toBe('до')
  })

  it('сообщает об изменении границ', async () => {
    const wrapper = mount(PriceRange, { props: { min: '', max: '' } })
    const inputs = wrapper.findAll('input')

    await inputs[0].setValue('1000')
    await inputs[1].setValue('5000')

    expect(wrapper.emitted('update:min')?.[0]).toEqual(['1000'])
    expect(wrapper.emitted('update:max')?.[0]).toEqual(['5000'])
  })
})