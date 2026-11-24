import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'

const { list, read } = vi.hoisted(() => ({ list: vi.fn(), read: vi.fn() }))

vi.mock('../../api/client', () =>
  createApiClientMock({
    adminOutbox: {
      list: () => list(),
      read: (fileName: string) => read(fileName),
    },
  }),
)

import router from '../../router'
import AdminOutboxView from '../AdminOutboxView.vue'

function mountView() {
  return mount(AdminOutboxView, { global: { plugins: [router] } })
}

describe('AdminOutboxView', () => {
  beforeEach(() => {
    list.mockReset()
    read.mockReset()
  })

  it('показывает письма с получателем и темой', async () => {
    list.mockResolvedValue([
      {
        fileName: 'mail.eml',
        size: 100,
        modifiedAt: '2026-11-20T12:00:00Z',
        recipient: 'user@example.com',
        subject: 'Подтверждение email',
        sentAt: '2026-11-20T12:00:00Z',
      },
    ])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.findAll('[data-test="outbox-message"]')).toHaveLength(1)
    expect(wrapper.text()).toContain('Подтверждение email')
    expect(wrapper.text()).toContain('user@example.com')
    expect(wrapper.find('.breadcrumbs').text()).toContain('Письма (outbox)')
  })

  it('открывает выбранное письмо', async () => {
    list.mockResolvedValue([
      {
        fileName: 'mail.eml',
        size: 100,
        modifiedAt: '2026-11-20T12:00:00Z',
        recipient: 'user@example.com',
        subject: 'Подтверждение email',
        sentAt: '2026-11-20T12:00:00Z',
      },
    ])
    read.mockResolvedValue({ content: 'To: user@example.com\n\ntoken=abc' })

    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('[data-test="outbox-message"] button').trigger('click')
    await flushPromises()

    expect(read).toHaveBeenCalledWith('mail.eml')
    expect(wrapper.find('[data-test="outbox-content"]').text()).toContain('token=abc')
  })

  it('сообщает о пустом ящике', async () => {
    list.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Писем нет')
  })

  it('показывает ошибку загрузки', async () => {
    list.mockRejectedValue(new Error('сбой'))

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Не удалось загрузить письма.')
  })
})
