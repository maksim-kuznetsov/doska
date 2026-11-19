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

  await expect(page.getByRole('heading', { name: 'Doska' })).toBeVisible()
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