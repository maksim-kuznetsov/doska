import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import StatusBadge from '../StatusBadge.vue'

describe('StatusBadge', () => {
  it('различает статусы цветовыми модификаторами', () => {
    const cases: Array<[string, string, string]> = [
      ['OnModeration', 'На проверке', 'status-badge--on-moderation'],
      ['Active', 'Активно', 'status-badge--active'],
      ['Rejected', 'Отклонено', 'status-badge--rejected'],
      ['Hidden', 'Скрыто', 'status-badge--hidden'],
      ['Sold', 'Продано', 'status-badge--sold'],
      ['Archived', 'Архив', 'status-badge--archived'],
    ]

    for (const [status, label, modifier] of cases) {
      const wrapper = mount(StatusBadge, { props: { status } })

      expect(wrapper.text()).toBe(label)
      expect(wrapper.classes()).toContain(modifier)
      expect(wrapper.attributes('data-status')).toBe(status)
    }
  })
})