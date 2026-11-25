import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import ComplaintCard from '../ComplaintCard.vue'

const complaint = {
  id: 'complaint-1',
  adId: 'ad-1',
  adTitle: 'Ноутбук рабочий',
  adDescription: 'Рабочий ноутбук, состояние хорошее.',
  adPrice: 40000,
  adCityName: 'Казань',
  adCategoryName: 'Электроника',
  adContacts: '+7 900 111-22-33',
  adStatus: 'Active',
  adAuthorEmail: 'seller@example.com',
  adPhotos: [],
  authorEmail: 'reporter@example.com',
  reason: 'Подозрительно',
  openComplaintsCount: 2,
  createdAt: '2026-11-20T10:00:00Z',
}

describe('ComplaintCard', () => {
  it('показывает объявление, сведения о жалобе и число открытых жалоб', () => {
    const wrapper = mount(ComplaintCard, { props: { complaint } })

    expect(wrapper.find('[data-test="complaint-item"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="admin-ad-preview"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Ноутбук рабочий')
    expect(wrapper.text()).toContain('Рабочий ноутбук, состояние хорошее.')
    expect(wrapper.text()).toContain('40000 ₽')
    expect(wrapper.text()).toContain('Казань')
    expect(wrapper.text()).toContain('Электроника')
    expect(wrapper.text()).toContain('seller@example.com')
    expect(wrapper.text()).toContain('reporter@example.com')
    expect(wrapper.text()).toContain('Подозрительно')
    expect(wrapper.text()).toContain('открытых жалоб на объявление')
  })

  it('оставляет объявление и сообщает о решении', async () => {
    const wrapper = mount(ComplaintCard, { props: { complaint } })

    const leave = wrapper.findAll('button').find((button) => button.text() === 'Оставить объявление')
    await leave!.trigger('click')

    expect(wrapper.emitted('resolve')?.[0]).toEqual(['complaint-1', false, null])
  })

  it('не даёт скрыть объявление без причины и отправляет причину', async () => {
    const wrapper = mount(ComplaintCard, { props: { complaint } })

    const hide = wrapper.findAll('button').find((button) => button.text() === 'Скрыть объявление')

    expect(hide!.attributes('disabled')).toBeDefined()

    await wrapper.find('textarea').setValue('Нарушение правил')

    expect(hide!.attributes('disabled')).toBeUndefined()

    await hide!.trigger('click')

    expect(wrapper.emitted('resolve')?.[0]).toEqual(['complaint-1', true, 'Нарушение правил'])
  })
})
