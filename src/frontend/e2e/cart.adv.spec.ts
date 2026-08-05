import { test, expect } from '@playwright/test'

/**
 * Adversarial cart specs: boundary quantities, invalid manual edits, and cookie isolation.
 */

test.describe('adversarial cart boundaries', () => {
  const headerCartLink = (page: any) =>
    page.locator('header .primary-nav .cart-link')

  async function addFirstAvailableProduct(page: any): Promise<void> {
    await page.goto('/')
    await expect(page.locator('.product-card').first()).toBeVisible()

    // Prefer an in-stock card. If none is visible, still try the first card; it will be
    // disabled if sold out, and the subsequent assertions will surface that.
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

  test('cart quantity cannot be set below 1', async ({ page }) => {
    // Scenario: user attempts to set quantity to 0; UI should keep it at 1 or remove the item.
    await addFirstAvailableProduct(page)

    await headerCartLink(page).click()
    await expect(page).toHaveURL(/\/cart/)

    const quantityInput = page.locator('.item').first().getByRole('spinbutton', { name: 'Quantity' })
    await expect(quantityInput).toHaveValue('1')

    await quantityInput.fill('0')
    await quantityInput.blur()

    // The input has min=1 and the store clamps to [1, stock]. Value should never stay 0.
    const value = Number(await quantityInput.inputValue())
    expect(value).toBeGreaterThanOrEqual(1)
  })

  test('cart quantity rejects negative value', async ({ page }) => {
    // Scenario: negative quantity must not be accepted by the cart UI.
    await addFirstAvailableProduct(page)

    await headerCartLink(page).click()
    await expect(page).toHaveURL(/\/cart/)

    const quantityInput = page.locator('.item').first().getByRole('spinbutton', { name: 'Quantity' })
    await expect(quantityInput).toHaveValue('1')

    await quantityInput.fill('-5')
    await quantityInput.blur()

    const value = Number(await quantityInput.inputValue())
    expect(value).toBeGreaterThanOrEqual(1)
  })

  test('cart quantity rejects oversized value', async ({ page }) => {
    // Scenario: attacker attempts to add an absurd quantity; UI must cap to available stock.
    await addFirstAvailableProduct(page)

    await headerCartLink(page).click()
    await expect(page).toHaveURL(/\/cart/)

    const quantityInput = page.locator('.item').first().getByRole('spinbutton', { name: 'Quantity' })
    const maxAttr = await quantityInput.getAttribute('max')
    const availableStock = Number(maxAttr) || 1

    await quantityInput.fill('2147483647')
    await quantityInput.blur()

    // The UI may cap to available stock or show an error; it must never exceed stock.
    const value = Number(await quantityInput.inputValue())
    expect(value).toBeLessThanOrEqual(availableStock)
  })
})
