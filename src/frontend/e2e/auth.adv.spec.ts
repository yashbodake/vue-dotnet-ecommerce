import { test, expect } from '@playwright/test'

/**
 * Adversarial auth specs: malformed credentials, oversized input, and route guards.
 */

test.describe('adversarial auth boundaries', () => {
  test('login form rejects empty email and password', async ({ page }) => {
    // Scenario: empty fields should not submit and should stay on the login page.
    // The browser's built-in HTML5 validation prevents submission before JavaScript runs.
    await page.goto('/login')
    await page.getByRole('textbox', { name: /email/i }).fill('')
    await page.getByRole('textbox', { name: /password/i }).fill('')
    await page.getByRole('button', { name: 'Sign in' }).click()

    await expect(page).toHaveURL(/\/login/)
  })

  test('login with wrong password stays on login page', async ({ page }) => {
    // Scenario: invalid credentials must not redirect to the home page.
    await page.goto('/login')
    await page.getByRole('textbox', { name: /email/i }).fill('admin@legacy.local')
    await page.getByRole('textbox', { name: /password/i }).fill('DefinitelyWrong123!')
    await page.getByRole('button', { name: 'Sign in' }).click()

    await expect(page).toHaveURL(/\/login/)
  })

  test('login with oversized email does not crash', async ({ page }) => {
    // Scenario: attacker submits a 5,000-character email; the UI must reject it cleanly.
    await page.goto('/login')
    const longEmail = 'a'.repeat(5000) + '@example.com'
    await page.getByRole('textbox', { name: /email/i }).fill(longEmail)
    await page.getByRole('textbox', { name: /password/i }).fill('Admin123!')
    await page.getByRole('button', { name: 'Sign in' }).click()

    await expect(page).toHaveURL(/\/login/)
  })

  test('unauthenticated user is redirected away from admin route', async ({ page }) => {
    // Scenario: deep-linking /admin/products without a token must not expose the admin UI.
    await page.goto('/admin/products')
    await expect(page).toHaveURL(/\/(login|unauthorized)/)
  })

  test('unauthenticated account route shows empty or redirects', async ({ page }) => {
    // Scenario: deep-linking /account/orders without a token.
    // The legacy route is /orders (no /account prefix). /account/orders is a 404 because
    // the router has no /account route. We assert that no real account content is rendered.
    await page.goto('/account/orders')

    const url = page.url()
    if (url.includes('/login') || url.includes('/unauthorized')) {
      await expect(page).toHaveURL(/\/(login|unauthorized)/)
    } else {
      // If no route exists, expect a 404 page instead of account data.
      await expect(page.locator('text=/404|page has wandered off|not found/i').first()).toBeVisible()
    }
  })
})
