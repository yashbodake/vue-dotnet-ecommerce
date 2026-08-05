import { test, expect } from '@playwright/test'

test.describe('admin operations', () => {
  test('admin can view products and orders', async ({ page }) => {
    await page.goto('/login')

    await expect(page.getByRole('textbox', { name: /email/i })).toHaveValue('admin@legacy.local')
    await expect(page.getByRole('textbox', { name: /password/i })).toHaveValue('Admin123!')

    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page).toHaveURL('/')

    // Admin nav exposes Products + "Admin orders" links (plus a customer Orders link).
    const adminProductsLink = page.locator('header .primary-nav a.nav-link', { hasText: 'Products' })
    const adminOrdersLink = page.locator('header .primary-nav a.nav-link', { hasText: 'Admin orders' })
    await expect(adminProductsLink).toBeVisible()
    await expect(adminOrdersLink).toBeVisible()

    await adminProductsLink.click()
    await expect(page).toHaveURL(/\/admin\/products/)

    const productTable = page.locator('table.table')
    await expect(productTable).toBeVisible()
    await expect(productTable.locator('tbody tr').first()).toBeVisible()

    await adminOrdersLink.click()
    await expect(page).toHaveURL(/\/admin\/orders/)

    const orderTable = page.locator('table.table')
    await expect(orderTable).toBeVisible()
    await expect(orderTable.locator('tbody tr').first()).toBeVisible()

    const statusDropdowns = page.locator('.status-cell select')
    await expect(statusDropdowns.first()).toBeVisible()
    const optionsCount = await statusDropdowns.first().locator('option').count()
    expect(optionsCount).toBeGreaterThan(1)
  })
})
