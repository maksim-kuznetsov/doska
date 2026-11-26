import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'

import { stubDialogElement } from '../../__tests__/helpers/dialog'
import ModalDialog from '../ModalDialog.vue'

let dialog: ReturnType<typeof stubDialogElement>

beforeEach(() => {
  dialog = stubDialogElement()
})

afterEach(() => {
  dialog.restore()
  document.body.querySelectorAll('.dialog-host').forEach((node) => node.remove())
})

function mountDialog(props: Record<string, unknown> = {}) {
  return mount(ModalDialog, {
    props: { title: 'Пожаловаться на объявление', ...props },
    slots: { default: '<p>Поле причины</p>' },
  })
}

describe('ModalDialog', () => {
  it('открывается как модальный диалог с заголовком и содержимым', () => {
    const wrapper = mountDialog()

    expect(dialog.showModal).toHaveBeenCalledOnce()
    expect(wrapper.find('dialog').attributes('aria-label')).toBe('Пожаловаться на объявление')
    expect(wrapper.find('.modal-dialog__title').text()).toBe('Пожаловаться на объявление')
    expect(wrapper.find('.modal-dialog__body').text()).toContain('Поле причины')
  })

  it('подтверждает действие', async () => {
    const wrapper = mountDialog()

    await wrapper.findAll('.modal-dialog__actions button')[1].trigger('click')

    expect(wrapper.emitted('confirm')).toHaveLength(1)
  })

  it('отменяет действие кнопкой', async () => {
    const wrapper = mountDialog()

    await wrapper.findAll('.modal-dialog__actions button')[0].trigger('click')

    expect(wrapper.emitted('cancel')).toHaveLength(1)
    expect(wrapper.emitted('confirm')).toBeUndefined()
  })

  it('закрывается клавишей Escape', async () => {
    const wrapper = mountDialog()

    await wrapper.find('dialog').trigger('keydown', { key: 'Escape' })

    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('закрывается кликом вне окна', async () => {
    const wrapper = mountDialog()

    await wrapper.find('dialog').trigger('click')

    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('не подтверждает действие при незаполненном обязательном поле', async () => {
    const wrapper = mountDialog({ confirmDisabled: true })

    const confirm = wrapper.findAll('.modal-dialog__actions button')[1]
    expect(confirm.attributes('disabled')).toBeDefined()

    await confirm.trigger('click')

    expect(wrapper.emitted('confirm')).toBeUndefined()
  })

  it('возвращает фокус на элемент, из которого открыт', () => {
    const opener = document.createElement('button')
    opener.className = 'dialog-host'
    document.body.appendChild(opener)
    opener.focus()

    const wrapper = mount(ModalDialog, {
      props: { title: 'Пожаловаться на объявление' },
      attachTo: document.body,
    })

    wrapper.unmount()

    expect(document.activeElement).toBe(opener)
  })
})
