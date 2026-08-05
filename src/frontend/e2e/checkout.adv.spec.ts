import { test, expect } from '@playwright/test'

/**
 * Adversarial checkout specs: empty cart checkout, oversized shipping address,
 * invalid card numbers, and missing required fields.
 */

test.describe('adversarial checkout boundaries', () => {
  const headerCartLink = (page: any) =>
    page.locator('header .primary-nav .cart-link')

  async function loginAsAdmin(page: any): Promise<void> {
    await page.goto('/login')
    await page.getByRole('textbox', { name: /email/i }).fill('admin@legacy.local')
    await page.getByRole('textbox', { name: /password/i }).fill('Admin123!')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page).toHaveURL('/')
  }

  async function addFirstAvailableProduct(page: any): Promise<void> {
    await page.goto('/')
    await expect(page.locator('.product-card').first()).toBeVisible()

    let inStockCard = page.locator('.product-card:has(.add-btn:not(:disabled))').first()
    let count = await inStockCard.count()
    if (count === 0) {
      inStockCard = page.locator('.product-card').first()
    }

    const addButton = inStockCard.getByRole('button', { name: /add to cart/i })
    await expect(addButton).toBeVisible()
    await addButton.click()

    const cartBadge = page.locator('header .primary-nav .badge')
    await expect(cartBadge).toBeVisible({ timeout: 5000 })
  }

  async function clearCart(page: any): Promise<void> {
    await page.goto('/cart')
    const cartBadge = page.locator('header .primary-nav .badge')
    // Remove items one at a time and wait for the API round-trip; fallback to page reload if any remain.
    for (let attempts = 0; attempts < 20; attempts++) {
      const removeButton = page.locator('.item button.remove').first()
      if ((await removeButton.count()) === 0) break
      const responsePromise = page.waitForResponse(
        (resp: any) => resp.url().includes('/api/cart') && resp.request().method() === 'DELETE',
        { timeout: 5000 },
      )
      await removeButton.click()
      await responsePromise.catch(() => {})
      await page.waitForTimeout(150)
    }
    // If the store still shows items (e.g., mergeOnLogin re-fetched), refresh the cart page.
    if ((await page.locator('.item').count()) > 0 || (await cartBadge.count()) > 0) {
      await page.reload()
      await page.waitForTimeout(200)
    }
  }

  test('checkout with empty cart is rejected', async ({ page }) => {
    // Scenario: a logged-in user proceeding to checkout with an empty cart must be blocked.
    await loginAsAdmin(page)
    await clearCart(page)

    await page.goto('/checkout')
    await expect(page).toHaveURL(/\/checkout/)

    // When the cart is empty the checkout view renders the .empty state, not the address/payment flow.
    await expect(page.locator('.empty')).toContainText(/empty|bag/i)
    await expect(page.locator('form.step-form')).toHaveCount(0)
  })

  test('checkout requires shipping address fields', async ({ page }) => {
    // Scenario: leaving required address fields blank must prevent progression.
    await loginAsAdmin(page)
    await clearCart(page)
    await addFirstAvailableProduct(page)

    await headerCartLink(page).click()
    await page.getByRole('link', { name: /proceed to checkout/i }).click()

    await expect(page).toHaveURL(/\/checkout/)

    // HTML5 validation prevents form submission; we should still be on the address step.
    await page.getByRole('button', { name: /continue to shipping/i }).click()
    await expect(page.locator('form.step-form h2').first()).toContainText(/shipping address/i)
  })

  test('checkout accepts only valid card number length', async ({ page }) => {
    // Scenario: a card number that is too short must not allow placing the order.
    await loginAsAdmin(page)
    await clearCart(page)
    await addFirstAvailableProduct(page)

    await headerCartLink(page).click()
    await page.getByRole('link', { name: /proceed to checkout/i }).click()

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

    await expect(page.locator('form.step-form h2').first()).toContainText(/payment/i)

    // The inputs are not textboxes (no type="text" role in Playwright for these fields).
    await page.locator('#cardName').fill('Test User')
    await page.locator('#cardNumber').fill('123')
    await page.locator('#expiry').fill('12/28')
    await page.locator('#cvv').fill('123')

    await page.getByRole('button', { name: /place order/i }).click()

    // HTML5 required/maxlength should prevent a 3-digit card from submitting.
    await expect(page).toHaveURL(/\/checkout/)
  })
})
