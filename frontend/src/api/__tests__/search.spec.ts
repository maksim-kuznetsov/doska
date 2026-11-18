import { describe, expect, it } from 'vitest'

import { defaultFeedFilters, readFilters, toRouteQuery, toSearchParams } from '../search'

describe('readFilters', () => {
  it('возвращает значения по умолчанию для пустого запроса', () => {
    expect(readFilters({})).toEqual(defaultFeedFilters)
  })

  it('читает фильтры и нормализует страницу', () => {
    const filters = readFilters({
      query: 'велосипед',
      categoryId: 'category',
      cityId: 'city',
      minPrice: '100',
      maxPrice: '500',
      sort: 'price_desc',
      page: '3',
    })

    expect(filters).toEqual({
      query: 'велосипед',
      categoryId: 'category',
      cityId: 'city',
      minPrice: '100',
      maxPrice: '500',
      sort: 'price_desc',
      page: 3,
    })
  })

  it('игнорирует неизвестную сортировку и некорректную страницу', () => {
    expect(readFilters({ sort: 'unknown', page: '0' })).toMatchObject({
      sort: 'newest',
      page: 1,
    })
  })
})

describe('toRouteQuery', () => {
  it('опускает значения по умолчанию', () => {
    expect(toRouteQuery({ ...defaultFeedFilters })).toEqual({})
  })

  it('сохраняет фильтры и номер страницы', () => {
    expect(
      toRouteQuery({
        query: ' велосипед ',
        categoryId: 'category',
        cityId: '',
        minPrice: '100',
        maxPrice: '',
        sort: 'price_asc',
        page: 2,
      }),
    ).toEqual({
      query: 'велосипед',
      categoryId: 'category',
      minPrice: '100',
      sort: 'price_asc',
      page: '2',
    })
  })
})

describe('toSearchParams', () => {
  it('преобразует цены и убирает пустые значения', () => {
    expect(
      toSearchParams({
        ...defaultFeedFilters,
        query: ' велосипед ',
        minPrice: '100',
        maxPrice: '',
        page: 2,
      }),
    ).toEqual({
      query: 'велосипед',
      categoryId: undefined,
      cityId: undefined,
      minPrice: 100,
      maxPrice: undefined,
      sort: 'newest',
      page: 2,
    })
  })
})