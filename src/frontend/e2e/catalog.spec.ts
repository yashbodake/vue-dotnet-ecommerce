import { test, expect } from '@playwright/test'

test.describe('catalog browsing', () => {
  test('browse catalog and view product detail', async ({ page }) => {
    await page.goto('/')

    const productGrid = page.locator('[aria-label="Product grid"]')
    await expect(productGrid).toBeVisible()

    const firstCard = page.locator('.product-card').first()
    await expect(firstCard).toBeVisible()

    const productName = await firstCard.locator('.name').textContent()
    await firstCard.locator('.name').click()

    await expect(page).toHaveURL(/\/products\/\d+/)
    await expect(page.locator('h1')).toContainText(productName ?? '')
    await expect(page.locator('.price')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Add to Cart' })).toBeVisible()
  })

  test('filter products by search', async ({ page }) => {
    await page.goto('/')

    await expect(page.locator('.product-card').first()).toBeVisible()
    const initialCount = await page.locator('.product-card').count()
    expect(initialCount).toBeGreaterThan(0)

    await page.getByRole('searchbox', { name: /search/i }).fill('zzzznonexistent')
    await page.waitForTimeout(350)

    await expect(page.locator('.empty')).toContainText('No products match')

    await page.getByRole('searchbox', { name: /search/i }).clear()
    await page.waitForTimeout(350)

    const restoredCount = await page.locator('.product-card').count()
    expect(restoredCount).toBeGreaterThan(0)
  })
})
