import { test, expect } from '@playwright/test'

test.describe('checkout flow', () => {
  test('login, checkout, and view order confirmation', async ({ page }) => {
    await page.goto('/login')

    await expect(page.getByRole('textbox', { name: /email/i })).toHaveValue('admin@legacy.local')
    await expect(page.getByRole('textbox', { name: /password/i })).toHaveValue('Admin123!')

    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page).toHaveURL('/')
    await expect(page.locator('.user')).toContainText('admin@legacy.local')

    await page.goto('/')
    const firstCard = page.locator('.product-card').first()
    await expect(firstCard).toBeVisible()
    await firstCard.getByRole('button', { name: 'Add to Cart' }).click()

    const cartBadge = page.locator('.badge')
    await expect(cartBadge).toBeVisible({ timeout: 5000 })

    await page.getByRole('link', { name: /cart/i }).click()
    await expect(page).toHaveURL(/\/cart/)
    await page.getByRole('link', { name: /proceed to checkout/i }).click()
    await expect(page).toHaveURL(/\/checkout/)

    await page.getByRole('textbox', { name: /full name/i }).fill('Test User')
    await page.getByRole('textbox', { name: /address line 1/i }).fill('123 Test Street')
    await page.getByRole('textbox', { name: /city/i }).fill('Test City')
    await page.getByRole('textbox', { name: /state/i }).fill('TS')
    await page.getByRole('textbox', { name: /postal code/i }).fill('12345')
    await page.getByRole('textbox', { name: /country/i }).fill('US')

    await page.getByRole('button', { name: /continue to shipping/i }).click()

    const standardOption = page.getByRole('radio', { name: /standard/i })
    await expect(standardOption).toBeVisible({ timeout: 10000 })
    await standardOption.check()

    await page.getByRole('button', { name: /continue to payment/i }).click()

    await page.getByRole('textbox', { name: /card name/i }).fill('Test User')
    await page.getByRole('textbox', { name: /card number/i }).fill('4111111111111111')
    await page.getByRole('textbox', { name: /expiry/i }).fill('12/28')
    await page.getByRole('textbox', { name: /cvv/i }).fill('123')

    await page.getByRole('button', { name: 'Place Order' }).click()

    await expect(page).toHaveURL(/\/checkout\/confirmation\/\d+/)
    await expect(page.locator('.order-number')).toContainText(/#\d+/)
  })
})
