import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { describe, expect, it, vi } from 'vitest'

import FormErrors from '../FormErrors.vue'

describe('FormErrors', () => {
  it('показывает сводку со ссылками на поля', () => {
    const wrapper = mount(FormErrors, {
      props: {
        errors: [
          { fieldId: 'title', label: 'Укажите заголовок.' },
          { fieldId: 'price', label: 'Укажите цену.' },
        ],
      },
    })

    expect(wrapper.find('[data-test="form-errors"]').exists()).toBe(true)
    const links = wrapper.findAll('a')
    expect(links).toHaveLength(2)
    expect(links[0].attributes('href')).toBe('#title')
  })

  it('переводит фокус на первое неверное поле', async () => {
    const field = document.createElement('input')
    field.id = 'title'
    document.body.appendChild(field)
    const focus = vi.spyOn(field, 'focus')

    const wrapper = mount(FormErrors, { props: { errors: [] } })
    await wrapper.setProps({ errors: [{ fieldId: 'title', label: 'Укажите заголовок.' }] })
    await nextTick()
    await nextTick()

    expect(focus).toHaveBeenCalled()

    field.remove()
  })

  it('не показывает сводку без ошибок', () => {
    const wrapper = mount(FormErrors, { props: { errors: [] } })

    expect(wrapper.find('[data-test="form-errors"]').exists()).toBe(false)
  })
})