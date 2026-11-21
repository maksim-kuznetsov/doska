/** Порог в пикселях, ниже которого расхождение размеров не считается переполнением. */
const OVERFLOW_EPSILON = 1

export interface CategoryStripMetrics {
  /** Текущее смещение прокрутки по горизонтали. */
  scrollLeft: number
  /** Ширина видимой области списка. */
  clientWidth: number
  /** Полная ширина содержимого списка. */
  scrollWidth: number
}

export interface CategoryStripFades {
  /** Гасить левый край: есть разделы левее видимой области. */
  fadeStart: boolean
  /** Гасить правый край: есть разделы правее видимой области. */
  fadeEnd: boolean
}

/**
 * Определяет, нужно ли гасить края горизонтальной полосы разделов, по её
 * метрикам прокрутки. Затухание показывается только когда содержимое шире
 * видимой области и у соответствующего края остались непросмотренные разделы.
 */
export function computeStripFades({
  scrollLeft,
  clientWidth,
  scrollWidth,
}: CategoryStripMetrics): CategoryStripFades {
  const overflowing = scrollWidth - clientWidth > OVERFLOW_EPSILON
  if (!overflowing) {
    return { fadeStart: false, fadeEnd: false }
  }

  return {
    fadeStart: scrollLeft > OVERFLOW_EPSILON,
    fadeEnd: scrollLeft < scrollWidth - clientWidth - OVERFLOW_EPSILON,
  }
}
