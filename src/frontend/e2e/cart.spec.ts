import { test, expect } from '@playwright/test'

test.describe('cart operations', () => {
  test('add product to cart and manage items', async ({ page }) => {
    await page.goto('/')

    const firstCard = page.locator('.product-card').first()
    await expect(firstCard).toBeVisible()

    const productName = await firstCard.locator('.name').textContent()
    await firstCard.getByRole('button', { name: 'Add to Cart' }).click()

    const cartBadge = page.locator('.badge')
    await expect(cartBadge).toBeVisible({ timeout: 5000 })
    await expect(cartBadge).toHaveText(/[1-9]/)

    await page.locator('header .primary-nav .cart-link').click()
    await expect(page).toHaveURL(/\/cart/)

    const cartItem = page.locator('.item').first()
    await expect(cartItem).toBeVisible()
    await expect(cartItem.locator('.name')).toContainText(productName ?? '')
    await expect(cartItem.locator('.unit-price')).toBeVisible()

    const quantityInput = cartItem.getByRole('spinbutton', { name: 'Quantity' })
    await expect(quantityInput).toHaveValue('1')

    const increaseButton = cartItem.getByRole('button', { name: 'Increase quantity' })
    await increaseButton.click()
    await expect(quantityInput).toHaveValue('2')

    await cartItem.getByRole('button', { name: 'Remove' }).click()
    await expect(page.locator('.empty')).toContainText(/Your bag is empty|Your cart is empty/)
    await expect(cartBadge).toHaveCount(0)
  })
})
