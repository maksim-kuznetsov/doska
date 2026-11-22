import { mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import { afterEach, describe, expect, it, vi } from 'vitest'

import DropdownMenu from '../DropdownMenu.vue'

const Host = defineComponent({
  props: {
    panelId: { type: String, default: 'test-panel' },
    label: { type: String, default: 'Бургер' },
  },
  emits: ['select'],
  setup(props, { emit }) {
    return () =>
      h(
        DropdownMenu,
        { panelId: props.panelId },
        {
          trigger: ({ open, toggle }: { open: boolean; toggle: () => void }) =>
            h(
              'button',
              {
                class: 'dropdown__trigger',
                'aria-controls': props.panelId,
                'aria-expanded': String(open),
                onClick: toggle,
              },
              props.label,
            ),
          default: ({ close }: { close: () => void }) =>
            h(
              'button',
              {
                class: 'dropdown__item',
                onClick: () => {
                  emit('select')
                  close()
                },
              },
              'Пункт',
            ),
        },
      )
  },
})

const TwoMenusHost = defineComponent({
  render() {
    return h('div', [
      h(Host, { panelId: 'first-panel', label: 'Первое' }),
      h(Host, { panelId: 'second-panel', label: 'Второе' }),
    ])
  },
})

/** Создаёт событие указателя с нужным типом ввода. */
function pointerEvent(type: string, pointerType: string): Event {
  const event = new Event(type, { bubbles: type === 'pointerdown' })
  Object.defineProperty(event, 'pointerType', { value: pointerType })
  return event
}

function dispatchPointer(target: Element, type: string, pointerType: string): void {
  target.dispatchEvent(pointerEvent(type, pointerType))
}

describe('DropdownMenu', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('раскрывается по наведению мыши и сообщает состояние доступности', async () => {
    const wrapper = mount(Host)
    const trigger = wrapper.get('button')

    expect(trigger.attributes('aria-expanded')).toBe('false')

    dispatchPointer(wrapper.get('.dropdown').element, 'pointerenter', 'mouse')
    await wrapper.vm.$nextTick()

    expect(trigger.attributes('aria-expanded')).toBe('true')
    expect(wrapper.find('#test-panel').exists()).toBe(true)
    expect(trigger.attributes('aria-controls')).toBe('test-panel')
  })

  it('не раскрывается от эмуляции наведения при касании', async () => {
    const wrapper = mount(Host)

    dispatchPointer(wrapper.get('.dropdown').element, 'pointerenter', 'touch')
    await wrapper.vm.$nextTick()

    expect(wrapper.find('#test-panel').exists()).toBe(false)
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

    dispatchPointer(wrapper.get('.dropdown').element, 'pointerenter', 'mouse')
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#test-panel').exists()).toBe(true)

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    await wrapper.vm.$nextTick()

    expect(wrapper.find('#test-panel').exists()).toBe(false)
  })

  it('не закрывает меню от pointerleave при касании', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Host)
    const root = wrapper.get('.dropdown').element

    await wrapper.get('.dropdown__trigger').trigger('click')
    dispatchPointer(root, 'pointerleave', 'touch')
    vi.advanceTimersByTime(500)
    await wrapper.vm.$nextTick()

    expect(wrapper.find('#test-panel').exists()).toBe(true)
  })

  it('закрывает меню по pointerleave мыши с задержкой', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Host)
    const root = wrapper.get('.dropdown').element

    dispatchPointer(root, 'pointerenter', 'mouse')
    await wrapper.vm.$nextTick()
    dispatchPointer(root, 'pointerleave', 'mouse')
    vi.advanceTimersByTime(120)
    await wrapper.vm.$nextTick()

    expect(wrapper.find('#test-panel').exists()).toBe(false)
  })

  it('отменяет запланированное закрытие при pointerdown внутри меню', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Host)
    const root = wrapper.get('.dropdown').element

    dispatchPointer(root, 'pointerenter', 'mouse')
    await wrapper.vm.$nextTick()
    dispatchPointer(root, 'pointerleave', 'mouse')
    dispatchPointer(root, 'pointerdown', 'mouse')
    vi.advanceTimersByTime(500)
    await wrapper.vm.$nextTick()

    expect(wrapper.find('#test-panel').exists()).toBe(true)
  })

  it('выполняет действие пункта касанием и закрывает меню', async () => {
    const wrapper = mount(Host)
    await wrapper.get('.dropdown__trigger').trigger('click')

    const item = wrapper.get('.dropdown__item')
    dispatchPointer(item.element, 'pointerleave', 'touch')
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#test-panel').exists()).toBe(true)

    await item.trigger('click')

    expect(wrapper.emitted('select')).toHaveLength(1)
    expect(wrapper.find('#test-panel').exists()).toBe(false)
  })

  it('открытие второго меню закрывает первое, оставляя второй триггер раскрытым', async () => {
    const wrapper = mount(TwoMenusHost, { attachTo: document.body })
    const triggers = wrapper.findAll('.dropdown__trigger')

    await triggers[0].trigger('click')
    expect(wrapper.find('#first-panel').exists()).toBe(true)

    dispatchPointer(wrapper.findAll('.dropdown')[1].element, 'pointerdown', 'touch')
    await triggers[1].trigger('click')
    await wrapper.vm.$nextTick()

    expect(wrapper.find('#first-panel').exists()).toBe(false)
    expect(wrapper.find('#second-panel').exists()).toBe(true)
    expect(triggers[1].attributes('aria-expanded')).toBe('true')

    wrapper.unmount()
  })
})
