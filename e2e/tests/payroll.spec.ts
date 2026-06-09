import { test, expect, type Page } from '@playwright/test'
import { freshRun } from '../helpers/employee'

// OrgAdmin storageState; org onboarded by bootstrap (dept/desig/location/structure).
// Payroll skips employees without a salary structure OR without Father's Name, so a
// PAYABLE employee needs step-2 salary + step-3 Father's Name. Calc checks are
// rate-agnostic: assert Net = Gross - Deductions - Taxes from displayed figures.

const STAMP = String(Date.now()).slice(-7)

function parseMoney(s: string): number {
  return Number(s.replace(/[^0-9.]/g, ''))
}

/** Create a PAYABLE employee (salary + Father's Name). Returns the unique surname. */
async function createPayableEmployee(page: Page, ctc: number, tag: string): Promise<string> {
  const surname = `Rao${STAMP}${tag}`
  await page.goto('/employees/new')
  await page.locator('input[name="firstName"]').fill('Arjun')
  await page.locator('input[name="lastName"]').fill(surname)
  await page.locator('input[name="workEmail"]').fill(`arjun.${STAMP}.${tag}@example.com`)
  await page.locator('input[name="mobileNumber"]').fill('9876500000')
  await page.locator('select[name="gender"]').selectOption('Male')
  await page.locator('input[name="dateOfJoining"]').fill('2026-04-01')
  await page.locator('input[name="dateOfBirth"]').fill('1990-01-01')
  await page.locator('select[name="employmentType"]').selectOption('FullTime')
  await page.locator('select[name="departmentId"]').selectOption({ index: 1 })
  await page.locator('select[name="designationId"]').selectOption({ index: 1 })
  await page.locator('select[name="workLocationId"]').selectOption({ index: 1 })
  await page.getByRole('button', { name: /save and continue/i }).click()

  // Step 2 — salary (template auto-selected): enter CTC.
  await expect(page).toHaveURL(/\/wizard\/salary/)
  await page.getByRole('spinbutton').first().fill(String(ctc))
  await page.getByRole('button', { name: /save and continue/i }).click()

  // Step 3 — personal: Father's Name is required for payroll readiness.
  await expect(page).toHaveURL(/\/wizard\/personal/)
  await page.locator('input[name="fathersName"]').fill('Ramesh Rao')
  await page.getByRole('button', { name: /save and continue/i }).click()

  // Step 4 — payment: bank details are required for payroll readiness.
  await expect(page).toHaveURL(/\/wizard\/payment/)
  await page.locator('input[name="accountHolderName"]').fill('Arjun Rao')
  await page.locator('input[name="bankName"]').fill('HDFC Bank')
  await page.locator('input[name="accountNumber"]').fill('12345678901')
  await page.locator('input[name="confirmAccountNumber"]').fill('12345678901')
  await page.locator('select[name="accountType"]').selectOption({ index: 1 })
  await page.locator('input[name="ifscCode"]').fill('HDFC0001234') // 11 chars
  await page.getByRole('button', { name: 'Save and Finish' }).click()
  await expect(page).toHaveURL(/\/employees\/[0-9a-f-]+$/)
  return surname
}

// ---------- H1. Pay runs list ----------
test('H1 pay runs page renders', async ({ page }) => {
  await page.goto('/pay-runs')
  await expect(page.getByRole('heading', { name: 'Pay Runs' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Run Payroll' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Payroll History' })).toBeVisible()
})

// ---------- G/H. Create payable employee with salary ----------
test('create employee with salary structure persists CTC', async ({ page }) => {
  const surname = await createPayableEmployee(page, 840000, 'ctc')
  await expect(page.getByText(`Arjun ${surname}`).first()).toBeVisible()
  // CTC lives on the Salary Details tab.
  await page.getByText('Salary Details').first().click()
  await expect(page.getByText(/8,40,000|840,000/).first()).toBeVisible()
})

// ---------- H4. Calculation integrity on a real run (rate-agnostic) ----------
test('payroll run: Net Pay equals Gross minus Deductions minus Taxes', async ({ page }) => {
  const surname = await createPayableEmployee(page, 840000, 'calc')

  await freshRun(page)

  // The payable employee must appear as Active (not skipped) with a non-zero gross.
  await page.getByRole('button', { name: /^Active/ }).click()
  const row = page.getByRole('row').filter({ hasText: surname }).first()
  await expect(row).toBeVisible({ timeout: 15_000 })

  const cells = row.getByRole('cell')
  // Columns: [chevron, Employee, Gross, Deductions, Taxes, Net, LOP, Actions]
  const gross = parseMoney(await cells.nth(2).innerText())
  const deductions = parseMoney(await cells.nth(3).innerText())
  const taxes = parseMoney(await cells.nth(4).innerText())
  const net = parseMoney(await cells.nth(5).innerText())

  expect(gross, 'gross must be non-zero for a payable employee').toBeGreaterThan(0)
  // Core invariant — no statutory rate assumed:
  expect(
    Math.abs(net - (gross - deductions - taxes)),
    `Net (${net}) must equal Gross (${gross}) - Deductions (${deductions}) - Taxes (${taxes})`,
  ).toBeLessThanOrEqual(0.01)
})
