import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import ModerationQueueList from '../ModerationQueueList.vue'

const items = [
  {
    id: 'ad-1',
    title: 'Велосипед',
    description: 'Аккуратный велосипед.',
    price: 25000,
    cityName: 'Москва',
    categoryName: 'Автомобили',
    contacts: '+7 900 000-00-00',
    authorEmail: 'author@example.com',
    authorIsTrusted: false,
    authorApprovedAdsCount: 2,
    submittedAt: '2026-11-18T10:00:00Z',
    photos: [],
  },
]

describe('ModerationQueueList', () => {
  it('показывает объявления очереди', () => {
    const wrapper = mount(ModerationQueueList, { props: { items } })

    expect(wrapper.findAll('[data-test="queue-item"]')).toHaveLength(1)
    expect(wrapper.text()).toContain('Велосипед')
    expect(wrapper.text()).toContain('author@example.com')
  })

  it('показывает данные объявления и контекст автора', () => {
    const wrapper = mount(ModerationQueueList, { props: { items } })

    expect(wrapper.find('[data-test="admin-ad-preview"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('25000 ₽')
    expect(wrapper.text()).toContain('Москва')
    expect(wrapper.text()).toContain('Аккуратный велосипед.')
    expect(wrapper.text()).toContain('без доверия')
    expect(wrapper.text()).toContain('одобрено объявлений: 2')
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
