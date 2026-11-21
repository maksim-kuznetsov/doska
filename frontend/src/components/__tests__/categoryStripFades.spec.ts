import { describe, expect, it } from 'vitest'

import { computeStripFades } from '../categoryStripFades'

describe('computeStripFades', () => {
  it('не гасит края, когда разделы помещаются целиком', () => {
    expect(computeStripFades({ scrollLeft: 0, clientWidth: 800, scrollWidth: 800 })).toEqual({
      fadeStart: false,
      fadeEnd: false,
    })
  })

  it('не гасит края при незначительном расхождении размеров', () => {
    expect(computeStripFades({ scrollLeft: 0, clientWidth: 800, scrollWidth: 800.5 })).toEqual({
      fadeStart: false,
      fadeEnd: false,
    })
  })

  it('гасит только правый край в начале списка', () => {
    expect(computeStripFades({ scrollLeft: 0, clientWidth: 800, scrollWidth: 1200 })).toEqual({
      fadeStart: false,
      fadeEnd: true,
    })
  })

  it('гасит оба края, когда список переполнен и прокручен в середину', () => {
    expect(computeStripFades({ scrollLeft: 120, clientWidth: 800, scrollWidth: 1200 })).toEqual({
      fadeStart: true,
      fadeEnd: true,
    })
  })

  it('гасит только левый край в конце списка', () => {
    expect(computeStripFades({ scrollLeft: 400, clientWidth: 800, scrollWidth: 1200 })).toEqual({
      fadeStart: true,
      fadeEnd: false,
    })
  })
})
