import { test, expect } from '@playwright/test'

// OrgAdmin storageState. Bootstrap onboarded the org (dept/designation/location/
// salary-structure defaults applied), so "Add Employee" must be enabled.

const STAMP = String(Date.now()).slice(-7)

// ---------- G1. Employees list ----------
test.describe('G1 employees list', () => {
  test('list renders header and Add Employee action', async ({ page }) => {
    await page.goto('/employees')
    await expect(page.getByRole('heading', { name: /employees|people/i }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: /add employee/i }).or(page.getByRole('link', { name: /add employee/i })).first()).toBeVisible()
  })

  test('Add Employee is enabled after onboarding', async ({ page }) => {
    await page.goto('/employees')
    const add = page.getByRole('button', { name: /add employee/i }).first()
    // Onboarding complete → control is enabled (not disabled).
    await expect(add).toBeEnabled()
  })

  test('status filter tabs are present', async ({ page }) => {
    await page.goto('/employees')
    await expect(page.getByText(/^All$/).first()).toBeVisible()
    await expect(page.getByText(/^Active$/).first()).toBeVisible()
  })
})

// ---------- G2. Add Employee wizard — Step 1 validation ----------
test.describe('G2 wizard step 1 validation', () => {
  test('submitting empty step 1 surfaces required errors', async ({ page }) => {
    await page.goto('/employees/new')
    await page.getByRole('button', { name: /save and continue/i }).click()
    // Required fields flag errors; still on the wizard.
    await expect(page.getByText(/required/i).first()).toBeVisible()
    await expect(page).toHaveURL(/\/employees\/new/)
  })

  test('invalid mobile number rejected', async ({ page }) => {
    await page.goto('/employees/new')
    await page.locator('input[name="mobileNumber"]').fill('123')
    await page.getByRole('button', { name: /save and continue/i }).click()
    await expect(page.getByText(/10 digits/i)).toBeVisible()
  })

  test('happy path: fill step 1 and continue to salary step', async ({ page }) => {
    await page.goto('/employees/new')
    await page.locator('input[name="firstName"]').fill('Priya')
    await page.locator('input[name="lastName"]').fill(`Sharma${STAMP}`)
    await page.locator('input[name="workEmail"]').fill(`priya.sharma.${STAMP}@example.com`)
    await page.locator('input[name="mobileNumber"]').fill('9876543210')
    await page.locator('select[name="gender"]').selectOption('Female')
    await page.locator('input[name="dateOfJoining"]').fill('2026-04-01')
    await page.locator('input[name="dateOfBirth"]').fill('1995-06-15')
    await page.locator('select[name="employmentType"]').selectOption('FullTime')
    // Org selects: pick the first real option from the bootstrap defaults.
    await page.locator('select[name="departmentId"]').selectOption({ index: 1 })
    await page.locator('select[name="designationId"]').selectOption({ index: 1 })
    await page.locator('select[name="workLocationId"]').selectOption({ index: 1 })
    await page.getByRole('button', { name: /save and continue/i }).click()
    // Advances to the salary step (URL contains /wizard/salary).
    await expect(page).toHaveURL(/\/wizard\/salary/)
  })
})
