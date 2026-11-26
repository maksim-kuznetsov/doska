import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import LegalDocument from '../LegalDocument.vue'

describe('LegalDocument', () => {
  it('показывает заголовок, дату редакции и содержимое документа', () => {
    const wrapper = mount(LegalDocument, {
      props: {
        title: 'Политика конфиденциальности',
        updatedAt: '2026-11-26',
      },
      slots: {
        default: '<p>Текст документа</p>',
      },
    })

    expect(wrapper.get('.legal__title').text()).toBe('Политика конфиденциальности')
    expect(wrapper.get('.legal__updated').text()).toBe('Дата редакции: 26 ноября 2026 г.')
    expect(wrapper.get('.legal__content').text()).toContain('Текст документа')
  })
})