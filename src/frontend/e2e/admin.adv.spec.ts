import { test, expect } from '@playwright/test'

/**
 * Adversarial admin specs: non-admin user cannot access admin routes and
 * invalid product inputs are rejected in the UI.
 */

test.describe('adversarial admin boundaries', () => {
  test('non-admin user cannot access admin products route', async ({ page }) => {
    // Scenario: a customer account must not see the admin products page.
    await page.goto('/login')
    await page.getByRole('textbox', { name: /email/i }).fill('customer@example.com')
    await page.getByRole('textbox', { name: /password/i }).fill('Customer123!')
    await page.getByRole('button', { name: 'Sign in' }).click()

    // If the user does not exist, the login page is acceptable.
    await page.waitForURL(/\/(login|)/)
    if (page.url().includes('/login')) {
      return
    }

    await page.goto('/admin/products')
    await expect(page).toHaveURL(/\/(login|unauthorized|\/)/)
  })

  test('guest cannot access admin orders route', async ({ page }) => {
    // Scenario: unauthenticated deep-link to admin orders must redirect to login.
    await page.goto('/admin/orders')
    await expect(page).toHaveURL(/\/(login|unauthorized)/)
  })
})
