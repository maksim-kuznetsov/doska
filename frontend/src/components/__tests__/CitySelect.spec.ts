import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import CitySelect from '../CitySelect.vue'

const cities = [
  { id: 'city-1', name: 'Москва' },
  { id: 'city-2', name: 'Санкт-Петербург' },
]

describe('CitySelect', () => {
  it('показывает текущий город и вариант «Все города»', () => {
    const wrapper = mount(CitySelect, { props: { modelValue: 'city-1', cities } })
    const options = wrapper.findAll('option')

    expect(options[0].text()).toBe('Все города')
    expect(options[0].attributes('value')).toBe('')
    expect(options.map((option) => option.text())).toContain('Москва')
    expect((wrapper.find('select').element as HTMLSelectElement).value).toBe('city-1')
  })

  it('сообщает о смене города', async () => {
    const wrapper = mount(CitySelect, { props: { modelValue: '', cities } })

    await wrapper.find('select').setValue('city-2')

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['city-2'])
  })
})
