import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'

import { stubDialogElement } from '../../__tests__/helpers/dialog'
import { photo } from '../../__tests__/helpers/photo'
import ImageGallery from '../ImageGallery.vue'

const photos = [photo(1), photo(2)]

let dialog: ReturnType<typeof stubDialogElement>

beforeEach(() => {
  dialog = stubDialogElement()
})

afterEach(() => {
  dialog.restore()
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
