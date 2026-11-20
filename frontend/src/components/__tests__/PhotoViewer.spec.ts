import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import type { AdPhotoSummary } from '../../api/client'
import PhotoViewer from '../PhotoViewer.vue'

function photo(order: number): AdPhotoSummary {
  return {
    id: `photo-${order}`,
    url: `/api/media/ad-1/photo-${order}.png`,
    thumbnailUrl: `/api/media/ad-1/photo-${order}_thumb.jpg`,
    contentType: 'image/png',
    byteSize: 128,
    width: 800,
    height: 600,
    sortOrder: order,
  }
}

const photos = [photo(1), photo(2), photo(3)]

type DialogPrototype = { showModal?: () => void; close?: () => void }
const dialogProto = HTMLDialogElement.prototype as unknown as DialogPrototype

let showModal: ReturnType<typeof vi.fn>

beforeEach(() => {
  showModal = vi.fn()
  dialogProto.showModal = showModal as unknown as () => void
  dialogProto.close = vi.fn()
})

afterEach(() => {
  delete dialogProto.showModal
  delete dialogProto.close
  document.body.querySelectorAll('.viewer-host').forEach((node) => node.remove())
})

describe('PhotoViewer', () => {
  it('открывается как модальный диалог с названием', () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 } })

    expect(showModal).toHaveBeenCalledOnce()
    expect(wrapper.find('dialog').exists()).toBe(true)
    expect(wrapper.find('dialog').attributes('aria-label')).toContain('Просмотр фотографий')
  })

  it('показывает выбранную фотографию в полном размере по url', () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 1 } })

    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe(photos[1].url)
    expect(wrapper.find('.photo-viewer__counter').text()).toBe('2 из 3')
  })

  it('листает вперёд и назад и зацикливается по краям', async () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 } })

    await wrapper.find('.photo-viewer__nav--next').trigger('click')
    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe(photos[1].url)

    await wrapper.find('.photo-viewer__nav--prev').trigger('click')
    await wrapper.find('.photo-viewer__nav--prev').trigger('click')
    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe(photos[2].url)
  })

  it('листает клавишами-стрелками', async () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 } })

    await wrapper.find('dialog').trigger('keydown', { key: 'ArrowRight' })
    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe(photos[1].url)

    await wrapper.find('dialog').trigger('keydown', { key: 'ArrowLeft' })
    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe(photos[0].url)
  })

  it('переходит по миниатюре и отмечает её текущей', async () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 } })

    await wrapper.findAll('.photo-viewer__thumb')[2].trigger('click')

    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe(photos[2].url)
    expect(wrapper.findAll('.photo-viewer__thumb')[2].classes()).toContain(
      'photo-viewer__thumb--active',
    )
  })

  it('скрывает листание при одной фотографии', () => {
    const wrapper = mount(PhotoViewer, { props: { photos: [photo(1)], startIndex: 0 } })

    expect(wrapper.find('.photo-viewer__nav').exists()).toBe(false)
    expect(wrapper.find('.photo-viewer__thumbs').exists()).toBe(false)
    expect(wrapper.find('.photo-viewer__counter').text()).toBe('1 из 1')
  })

  it('закрывается кнопкой', async () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 } })

    await wrapper.find('.photo-viewer__close').trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('закрывается клавишей Escape', async () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 } })

    await wrapper.find('dialog').trigger('keydown', { key: 'Escape' })

    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('закрывается кликом вне изображения', async () => {
    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 } })

    await wrapper.find('dialog').trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('возвращает фокус на элемент, из которого открыто', () => {
    const opener = document.createElement('button')
    opener.className = 'viewer-host'
    document.body.appendChild(opener)
    opener.focus()

    const wrapper = mount(PhotoViewer, { props: { photos, startIndex: 0 }, attachTo: document.body })

    wrapper.unmount()

    expect(document.activeElement).toBe(opener)
  })
})
