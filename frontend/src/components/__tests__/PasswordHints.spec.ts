import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import PasswordHints from '../PasswordHints.vue'

function states(wrapper: ReturnType<typeof mount>): (string | undefined)[] {
  return wrapper.findAll('[data-state]').map((item) => item.attributes('data-state'))
}

describe('PasswordHints', () => {
  it('показывает нейтральное состояние до ввода', () => {
    const wrapper = mount(PasswordHints, {
      props: { password: '', confirmation: '' },
    })

    expect(states(wrapper)).toEqual(['neutral', 'neutral', 'neutral'])
  })

  it('отмечает все выполненные правила', () => {
    const wrapper = mount(PasswordHints, {
      props: { password: 'secret123', confirmation: 'secret123' },
    })

    expect(states(wrapper)).toEqual(['met', 'met', 'met'])
  })

  it('отмечает нарушенные правила', () => {
    const wrapper = mount(PasswordHints, {
      props: { password: '12345678', confirmation: '12345679' },
    })

    expect(states(wrapper)).toEqual(['met', 'unmet', 'unmet'])
  })

  it('обновляет состояние по мере ввода', async () => {
    const wrapper = mount(PasswordHints, {
      props: { password: '', confirmation: '' },
    })

    await wrapper.setProps({ password: 'secret12', confirmation: 'secret12' })
    expect(states(wrapper)).toEqual(['met', 'met', 'met'])

    await wrapper.setProps({ password: '1234567' })
    expect(states(wrapper)).toEqual(['unmet', 'unmet', 'unmet'])
  })
})