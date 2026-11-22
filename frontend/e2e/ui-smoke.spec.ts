import { expect, test } from '@playwright/test'

async function login(page: import('@playwright/test').Page, email: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Пароль').fill('123789')
  await page.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByRole('button', { name: new RegExp(email) })).toBeVisible()
}

test('публичная лента показывает панель фильтров', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Объявления' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Показать' })).toBeVisible()
  await expect(page.getByText(/Найдено объявлений/)).toBeVisible()
})

test('модератор открывает очередь и жалобы', async ({ page }) => {
  await login(page, 'moderator@example.com')

  await page.goto('/moderation')
  await expect(page.getByRole('heading', { name: 'Очередь проверки' })).toBeVisible()

  await page.goto('/complaints')
  await expect(page.getByRole('heading', { name: 'Жалобы на объявления' })).toBeVisible()
})

test('администратор открывает справочники', async ({ page }) => {
  await login(page, 'admin@example.com')

  await page.goto('/admin/categories')
  await expect(page.getByRole('heading', { name: 'Категории' })).toBeVisible()

  await page.goto('/admin/cities')
  await expect(page.getByRole('heading', { name: 'Города' })).toBeVisible()

  await page.goto('/admin/users')
  await expect(page.getByRole('heading', { name: 'Пользователи' })).toBeVisible()
})

test('страница-гайд доступна в Development', async ({ page }) => {
  await page.goto('/design-system')

  await expect(page.getByRole('heading', { name: 'Дизайн-система' })).toBeVisible()
  await expect(page.getByText('Токены палитры')).toBeVisible()
})

test('неизвестный адрес открывает заглушку внутри общего макета', async ({ page }) => {
  await page.goto('/no-such-page')

  await expect(page.getByText('Страница не найдена')).toBeVisible()
  await expect(page.getByRole('link', { name: 'На главную' })).toBeVisible()
  await expect(page.locator('.site-header__brand')).toBeVisible()
  await expect(page.locator('.site-footer__brand')).toBeVisible()
})

test('недоступное объявление показывает пустое состояние', async ({ page }) => {
  await page.goto('/ads/00000000-0000-0000-0000-000000000000')

  await expect(page.getByText('Объявление недоступно')).toBeVisible()
  await expect(page.getByRole('link', { name: 'На главную' })).toBeVisible()
})

test.describe('мобильные меню шапки', () => {
  test.use({ hasTouch: true, viewport: { width: 390, height: 844 } })

  test('меню категорий открывается и закрывается касанием', async ({ page }) => {
    await page.goto('/')

    const trigger = page.getByRole('button', { name: 'Категории' })
    await trigger.tap()
    await expect(page.locator('#category-tree-panel')).toBeVisible()

    await trigger.tap()
    await expect(page.locator('#category-tree-panel')).toBeHidden()
  })

  test('касание меню пользователя закрывает меню категорий', async ({ page }) => {
    await login(page, 'admin@example.com')

    const categories = page.getByRole('button', { name: 'Категории' })
    const userMenu = page.getByRole('button', { name: /admin@example\.com/ })

    await categories.tap()
    await expect(page.locator('#category-tree-panel')).toBeVisible()

    await userMenu.tap()
    await expect(page.locator('#category-tree-panel')).toBeHidden()
    await expect(page.locator('#user-menu-panel')).toBeVisible()
  })
})