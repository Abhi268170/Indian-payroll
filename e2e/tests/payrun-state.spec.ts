import { test, expect } from '@playwright/test'
import { createPayableEmployee, initiateRun } from '../helpers/employee'

// Uses the DEDICATED CLEAN tenant (.auth/payrun.json) so the run has exactly one
// fully-payable employee and ZERO hard blocks — approval can succeed.

const kebab = 'button.w-8.h-8.rounded-lg.border'

test('P pay-run state machine: Draft → Approved → Paid → Approved → Draft → deleted, with downloads', async ({ page }) => {
  await createPayableEmployee(page, 840000, 'st', { pan: 'ABCDE1234F' })
  await initiateRun(page)

  // --- Draft ---
  await expect(page.getByText('Draft').first()).toBeVisible()
  await expect(page.getByRole('button', { name: 'Approve Payroll' })).toBeVisible()
  // Edit control present in Draft (exact: 'Skip' must not match the 'Skipped' tab).
  await expect(page.getByRole('button', { name: 'Skip', exact: true }).first()).toBeVisible()

  // --- Approve ---
  await page.getByRole('button', { name: 'Approve Payroll' }).click()
  await page.getByRole('button', { name: /submit and approve/i }).click()
  await expect(page.getByText('Approved').first()).toBeVisible({ timeout: 20_000 })
  await expect(page.getByRole('button', { name: 'Record Payment' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Reject' })).toBeVisible()
  // Immutability: per-row Skip no longer offered (exact, not the 'Skipped' tab).
  await expect(page.getByRole('button', { name: 'Skip', exact: true })).toHaveCount(0)

  // --- Bank advice download (xlsx) ---
  await page.getByRole('button', { name: 'Bank Advice' }).click()
  const dlBank = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Download', exact: true }).click()
  const bank = await dlBank
  expect(bank.suggestedFilename()).toMatch(/\.xlsx$/)

  // --- Record Payment → Paid ---
  await page.getByRole('button', { name: 'Record Payment' }).click()
  await page.getByRole('button', { name: 'Record Payment' }).last().click()
  await expect(page.getByText('Paid').first()).toBeVisible({ timeout: 20_000 })

  // (Payslip-PDF download verified separately; excluded here so its async dialog
  //  doesn't intercept the state-machine clicks — see GAPS G-8.)

  // --- Delete Recorded Payment → Approved ---
  await page.locator(kebab).first().click()
  await page.getByRole('button', { name: 'Delete Recorded Payment' }).click()
  await page.getByRole('button', { name: 'Delete Payment' }).click()
  await expect(page.getByText('Approved').first()).toBeVisible({ timeout: 20_000 })

  // --- Reject → Draft (F-5 FIXED: reason is optional; no more 500) ---
  await page.getByRole('button', { name: 'Reject' }).click()
  await page.getByRole('button', { name: 'Reject' }).last().click()
  await expect(page.getByText('Draft').first()).toBeVisible({ timeout: 20_000 })

  // --- Delete Pay Run (Draft) → back to list ---
  await page.locator(kebab).first().click()
  await page.getByRole('button', { name: 'Delete Pay Run' }).click()
  await expect(page).toHaveURL(/\/pay-runs$/, { timeout: 20_000 })
})
