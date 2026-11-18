import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import PhotoUploader from '../PhotoUploader.vue'

function file(name: string): File {
  return new File(['bytes'], name, { type: 'image/png' })
}

beforeEach(() => {
  URL.createObjectURL = vi.fn(() => 'blob:preview')
  URL.revokeObjectURL = vi.fn()
})

describe('PhotoUploader', () => {
  it('показывает счётчик принятых файлов и подсказку о лимитах', () => {
    const wrapper = mount(PhotoUploader, { props: { files: [] } })

    expect(wrapper.text()).toContain('Фотографии (0/5)')
    expect(wrapper.text()).toContain('JPEG, PNG или WebP')
    expect(wrapper.text()).toContain('до 5 МБ')
  })

  it('показывает превью выбранных файлов', () => {
    const wrapper = mount(PhotoUploader, { props: { files: [file('a.png'), file('b.png')] } })

    expect(wrapper.text()).toContain('Фотографии (2/5)')
    expect(wrapper.findAll('[data-test="photo-previews"] img')).toHaveLength(2)
  })

  it('исключает файл из набора при удалении превью', async () => {
    const wrapper = mount(PhotoUploader, { props: { files: [file('a.png'), file('b.png')] } })

    await wrapper.findAll('.photo-uploader__remove')[0].trigger('click')

    const emitted = wrapper.emitted('update:files')?.[0]?.[0] as File[]
    expect(emitted).toHaveLength(1)
    expect(emitted[0].name).toBe('b.png')
  })
})