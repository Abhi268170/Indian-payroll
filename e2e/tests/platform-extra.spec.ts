import { test, expect } from '@playwright/test'

// SuperAdmin storageState. Edge cases beyond the core platform spec.

test('Q displayName 101 chars rejected client-side (max 100, aligned with BE)', async ({ page }) => {
  const stamp = String(Date.now()).slice(-7)
  const longName = 'A'.repeat(101)
  await page.goto('/platform/orgs/new')
  await page.getByRole('textbox', { name: 'Acme Corp' }).fill(longName)
  await page.getByRole('textbox', { name: 'acme-corp' }).fill(`qa-long-${stamp}`)
  await page.getByRole('textbox', { name: 'admin@acme.com' }).fill(`qa.long.${stamp}@example.com`)
  await page.getByRole('button', { name: 'Provision Organisation' }).click()

  // F-4 FIXED: FE zod max aligned to BE (100), so the user gets an immediate
  // field-level error instead of a generic "Provisioning failed (HTTP 400)".
  await expect(page).toHaveURL(/\/platform\/orgs\/new/, { timeout: 10_000 })
  await expect(page.getByText(/Maximum 100 characters/i)).toBeVisible({ timeout: 10_000 })
})
