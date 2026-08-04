import { test, expect } from '@playwright/test'

test.describe('admin operations', () => {
  test('admin can view products and orders', async ({ page }) => {
    await page.goto('/login')

    await expect(page.getByRole('textbox', { name: /email/i })).toHaveValue('admin@legacy.local')
    await expect(page.getByRole('textbox', { name: /password/i })).toHaveValue('Admin123!')

    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page).toHaveURL('/')

    const adminProductsLink = page.getByRole('link', { name: 'Admin: Products' })
    await expect(adminProductsLink).toBeVisible()

    await adminProductsLink.click()
    await expect(page).toHaveURL(/\/admin\/products/)

    const productTable = page.locator('.data-table')
    await expect(productTable).toBeVisible()
    await expect(productTable.locator('tbody tr')).toHaveCountGreaterThan(0)

    await page.goto('/admin/orders')
    await expect(page).toHaveURL(/\/admin\/orders/)

    const orderTable = page.locator('.data-table')
    await expect(orderTable).toBeVisible()
    await expect(orderTable.locator('tbody tr')).toHaveCountGreaterThan(0)

    const statusDropdowns = page.locator('.status-cell select')
    await expect(statusDropdowns.first()).toBeVisible()
    const optionsCount = await statusDropdowns.first().locator('option').count()
    expect(optionsCount).toBeGreaterThan(1)
  })
})
