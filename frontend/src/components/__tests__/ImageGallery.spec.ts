import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import type { AdPhotoSummary } from '../../api/client'
import ImageGallery from '../ImageGallery.vue'

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

const photos = [photo(1), photo(2)]

type DialogPrototype = { showModal?: () => void; close?: () => void }
const dialogProto = HTMLDialogElement.prototype as unknown as DialogPrototype

beforeEach(() => {
  dialogProto.showModal = vi.fn() as unknown as () => void
  dialogProto.close = vi.fn() as unknown as () => void
})

afterEach(() => {
  delete dialogProto.showModal
  delete dialogProto.close
})

describe('ImageGallery', () => {
  it('показывает миниатюры кнопками', () => {
    const wrapper = mount(ImageGallery, { props: { photos } })

    expect(wrapper.findAll('.gallery__image')).toHaveLength(2)
    expect(wrapper.findAll('.gallery__button')).toHaveLength(2)
  })

  it('открывает окно с выбранной фотографией по клику на миниатюру', async () => {
    const wrapper = mount(ImageGallery, { props: { photos } })

    await wrapper.findAll('.gallery__button')[1].trigger('click')

    expect(wrapper.find('.photo-viewer').exists()).toBe(true)
    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe(photos[1].url)
  })

  it('закрывает окно по его сигналу', async () => {
    const wrapper = mount(ImageGallery, { props: { photos } })

    await wrapper.findAll('.gallery__button')[0].trigger('click')
    await wrapper.find('.photo-viewer__close').trigger('click')

    expect(wrapper.find('.photo-viewer').exists()).toBe(false)
  })

  it('не рендерит галерею и окно без фотографий', () => {
    const wrapper = mount(ImageGallery, { props: { photos: [] } })

    expect(wrapper.find('.gallery').exists()).toBe(false)
    expect(wrapper.find('.photo-viewer').exists()).toBe(false)
  })
})
