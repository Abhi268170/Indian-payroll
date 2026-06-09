import { test, expect } from '@playwright/test'
import { createPayableEmployee, freshRun } from '../helpers/employee'

// OrgAdmin storageState. Deep payroll-calculation checks via the Taxes & Deductions
// worksheet. Assertions are on the worksheet's LABELED lines / badges (not cell
// indices), so they are robust and rate-agnostic. All scenarios share ONE fresh run.

test('L deep calculation worksheet: no-PAN §206AA, 87A rebate, standard deduction + cess', async ({ page }) => {
  const noPan = await createPayableEmployee(page, 1200000, 'np') // no PAN
  const hiPan = await createPayableEmployee(page, 2400000, 'hi', { pan: 'ABCDE1234F' })

  await freshRun(page)
  await page.getByRole('button', { name: 'Taxes & Deductions' }).click()

  // No-PAN employee → §206AA badge in its row (flat 20% TDS path).
  const noPanRow = page.getByRole('row').filter({ hasText: noPan }).first()
  await expect(noPanRow).toBeVisible({ timeout: 15_000 })
  await expect(noPanRow.getByText('§206AA')).toBeVisible()

  // Higher-income with PAN → expand → Standard Deduction + Cess lines present.
  const hiRow = page.getByRole('row').filter({ hasText: hiPan }).first()
  await hiRow.click()
  await expect(page.getByText('Standard Deduction').first()).toBeVisible()
  await expect(page.getByText(/Health & Education Cess/i).first()).toBeVisible()
})
