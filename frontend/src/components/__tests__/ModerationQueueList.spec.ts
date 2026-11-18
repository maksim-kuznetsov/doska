import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import ModerationQueueList from '../ModerationQueueList.vue'

const items = [
  {
    id: 'ad-1',
    title: 'Велосипед',
    authorEmail: 'author@example.com',
    submittedAt: '2026-11-18T10:00:00Z',
  },
]

describe('ModerationQueueList', () => {
  it('показывает объявления очереди', () => {
    const wrapper = mount(ModerationQueueList, { props: { items } })

    expect(wrapper.findAll('[data-test="queue-item"]')).toHaveLength(1)
    expect(wrapper.text()).toContain('Велосипед')
    expect(wrapper.text()).toContain('author@example.com')
  })

  it('сообщает о пустой очереди', () => {
    const wrapper = mount(ModerationQueueList, { props: { items: [] } })

    expect(wrapper.text()).toContain('Очередь проверки пуста')
  })

  it('не даёт отклонить без причины и отправляет событие с причиной', async () => {
    const wrapper = mount(ModerationQueueList, { props: { items } })

    const reject = wrapper.findAll('button').find((button) => button.text() === 'Отклонить')

    expect(reject).toBeDefined()
    expect(reject!.attributes('disabled')).toBeDefined()

    await wrapper.find('textarea').setValue('Спам')

    expect(reject!.attributes('disabled')).toBeUndefined()

    await reject!.trigger('click')

    expect(wrapper.emitted('reject')?.[0]).toEqual(['ad-1', 'Спам'])
  })

  it('одобряет объявление без причины', async () => {
    const wrapper = mount(ModerationQueueList, { props: { items } })

    const approve = wrapper.findAll('button').find((button) => button.text() === 'Одобрить')
    await approve!.trigger('click')

    expect(wrapper.emitted('approve')?.[0]).toEqual(['ad-1'])
  })
})