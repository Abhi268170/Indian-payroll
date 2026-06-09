import { expect, type Page } from '@playwright/test'

export function parseMoney(s: string): number {
  return Number(s.replace(/[^0-9.]/g, ''))
}

export interface PayableOpts {
  pan?: string // omit → no-PAN (triggers §206AA / 20% flat)
  basic?: number // not used by template default; kept for intent
}

/** Create a PAYABLE employee through the full 4-step wizard
 *  (salary structure + Father's Name + bank account). Returns the unique surname.
 *  Pass opts.pan to set a PAN (else the employee is treated as no-PAN). */
export async function createPayableEmployee(
  page: Page,
  ctc: number,
  tag: string,
  opts: PayableOpts = {},
): Promise<string> {
  const surname = `Cx${Date.now().toString().slice(-6)}${tag}`
  await page.goto('/employees/new')
  await page.locator('input[name="firstName"]').fill('Test')
  await page.locator('input[name="lastName"]').fill(surname)
  await page.locator('input[name="workEmail"]').fill(`test.${surname.toLowerCase()}@example.com`)
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

  // Step 3 — personal: Father's Name required; PAN optional.
  await expect(page).toHaveURL(/\/wizard\/personal/)
  await page.locator('input[name="fathersName"]').fill('Ramesh Test')
  if (opts.pan) {
    await page.locator('input[name="pan"]').fill(opts.pan)
  }
  await page.getByRole('button', { name: /save and continue/i }).click()

  // Step 4 — payment: bank details required for payroll readiness.
  await expect(page).toHaveURL(/\/wizard\/payment/)
  await page.locator('input[name="accountHolderName"]').fill('Test Holder')
  await page.locator('input[name="bankName"]').fill('HDFC Bank')
  await page.locator('input[name="accountNumber"]').fill('12345678901')
  await page.locator('input[name="confirmAccountNumber"]').fill('12345678901')
  await page.locator('select[name="accountType"]').selectOption({ index: 1 })
  await page.locator('input[name="ifscCode"]').fill('HDFC0001234')
  await page.getByRole('button', { name: 'Save and Finish' }).click()
  await expect(page).toHaveURL(/\/employees\/[0-9a-f-]+$/)
  return surname
}

/** Assign an Active employee (by unique surname) as the Tax Deductor in
 *  Settings → Tax Details. Required before an exit can be initiated. */
export async function assignTaxDeductor(page: Page, surname: string): Promise<void> {
  await page.goto('/settings/tax-details')
  await page.getByRole('button', { name: 'Edit' }).click()
  await page.getByPlaceholder(/search by name, email or code/i).fill(surname)
  const select = page.locator('select').last() // deductor-employee picker
  await expect(select.locator('option', { hasText: surname })).toBeAttached({ timeout: 10_000 })
  await select.selectOption({ index: 1 })
  await page.getByRole('button', { name: 'Save' }).click()
  await expect(page.getByText(/saved/i)).toBeVisible({ timeout: 15_000 })
}

/** Initiate the current-period payroll run and land on its detail page. */
export async function initiateRun(page: Page): Promise<void> {
  await page.goto('/pay-runs')
  const process = page.getByRole('button', { name: 'Process Payroll' })
  const cont = page.getByRole('link', { name: /continue/i })
  await expect(process.or(cont).first()).toBeVisible({ timeout: 20_000 })
  if (await process.isVisible()) await process.click()
  else await cont.first().click()
  await expect(page).toHaveURL(/\/pay-runs\/[0-9a-f-]+/, { timeout: 60_000 })
}

/** Start a FRESH draft run for the current period: a Draft run is a snapshot, so
 *  delete any existing draft first, then process — guarantees newly-created
 *  employees are included. (A Draft run can be deleted with no confirm dialog.) */
export async function freshRun(page: Page): Promise<void> {
  await page.goto('/pay-runs')
  const cont = page.getByRole('link', { name: /continue/i })
  if ((await cont.count()) > 0) {
    await cont.first().click()
    await expect(page).toHaveURL(/\/pay-runs\/[0-9a-f-]+/, { timeout: 20_000 })
    // Delete the existing draft via the header kebab (Draft only).
    const kebab = page.locator('button.w-8.h-8.rounded-lg.border')
    if ((await kebab.count()) > 0) {
      await kebab.first().click()
      const del = page.getByRole('button', { name: 'Delete Pay Run' })
      if (await del.isVisible().catch(() => false)) {
        await del.click()
        await expect(page).toHaveURL(/\/pay-runs$/, { timeout: 20_000 })
      }
    }
    await page.goto('/pay-runs')
  }
  const process = page.getByRole('button', { name: 'Process Payroll' })
  await expect(process).toBeVisible({ timeout: 20_000 })
  await process.click()
  await expect(page).toHaveURL(/\/pay-runs\/[0-9a-f-]+/, { timeout: 60_000 })
}
