import { mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import { describe, expect, it } from 'vitest'

import DropdownMenu from '../DropdownMenu.vue'

const Host = defineComponent({
  render() {
    return h(
      DropdownMenu,
      { panelId: 'test-panel' },
      {
        trigger: ({ open, toggle }: { open: boolean; toggle: () => void }) =>
          h(
            'button',
            {
              class: 'dropdown__trigger',
              'aria-controls': 'test-panel',
              'aria-expanded': String(open),
              onClick: toggle,
            },
            'Бургер',
          ),
        default: () => h('a', { class: 'dropdown__item', href: '/one' }, 'Пункт'),
      },
    )
  },
})

describe('DropdownMenu', () => {
  it('раскрывается по наведению и сообщает состояние доступности', async () => {
    const wrapper = mount(Host)
    const trigger = wrapper.get('button')

    expect(trigger.attributes('aria-expanded')).toBe('false')

    await wrapper.trigger('pointerenter')

    expect(trigger.attributes('aria-expanded')).toBe('true')
    expect(wrapper.find('#test-panel').exists()).toBe(true)
    expect(trigger.attributes('aria-controls')).toBe('test-panel')
  })

  it('переключается нажатием', async () => {
    const wrapper = mount(Host)
    const trigger = wrapper.get('button')

    await trigger.trigger('click')
    expect(wrapper.find('#test-panel').exists()).toBe(true)

    await trigger.trigger('click')
    expect(wrapper.find('#test-panel').exists()).toBe(false)
  })

  it('закрывается клавишей Escape', async () => {
    const wrapper = mount(Host)

    await wrapper.trigger('pointerenter')
    expect(wrapper.find('#test-panel').exists()).toBe(true)

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    await wrapper.vm.$nextTick()

    expect(wrapper.find('#test-panel').exists()).toBe(false)
  })
})