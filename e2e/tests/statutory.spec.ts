import { test, expect } from '@playwright/test'

// OrgAdmin storageState. Statutory rates shown here are the source of truth for
// the payroll calculation assertions (H4/H5) — assert what the UI displays.

test.describe('E statutory components', () => {
  test('statutory page renders with tab navigation', async ({ page }) => {
    await page.goto('/settings/statutory')
    // Tabs: EPF, ESI, Professional Tax, Labour Welfare Fund, Statutory Bonus
    await expect(page.getByText(/EPF|Provident Fund/i).first()).toBeVisible()
    await expect(page.getByText(/ESI|State Insurance/i).first()).toBeVisible()
    await expect(page.getByText(/Professional Tax/i).first()).toBeVisible()
  })

  test('EPF tab shows statutory 12% employee contribution', async ({ page }) => {
    await page.goto('/settings/statutory')
    await page.getByRole('button', { name: /^EPF/ }).first().click().catch(() => {})
    await expect(page.getByText('12%').first()).toBeVisible()
  })

  test('ESI tab shows 0.75% employee and 3.25% employer rates', async ({ page }) => {
    await page.goto('/settings/statutory')
    await page.getByRole('button', { name: /ESI/ }).first().click().catch(() => {})
    await expect(page.getByText('0.75%')).toBeVisible()
    await expect(page.getByText('3.25%')).toBeVisible()
  })

  test('ESI tab states the 21,000 eligibility threshold', async ({ page }) => {
    await page.goto('/settings/statutory')
    await page.getByRole('button', { name: /ESI/ }).first().click().catch(() => {})
    await expect(page.getByText(/21,000/)).toBeVisible()
  })
})
