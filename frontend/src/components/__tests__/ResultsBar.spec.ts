import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import ResultsBar from '../ResultsBar.vue'

describe('ResultsBar', () => {
  it('показывает число найденных объявлений и сортировку', () => {
    const wrapper = mount(ResultsBar, { props: { total: 42, sort: 'newest' } })

    expect(wrapper.text()).toContain('Найдено объявлений: 42')
    expect(wrapper.find('select').exists()).toBe(true)
  })

  it('сообщает о смене сортировки', async () => {
    const wrapper = mount(ResultsBar, { props: { total: 1, sort: 'newest' } })

    await wrapper.find('select').setValue('price_desc')

    expect(wrapper.emitted('update:sort')?.[0]).toEqual(['price_desc'])
  })
})