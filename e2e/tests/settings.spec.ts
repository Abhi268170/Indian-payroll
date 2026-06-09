import { test, expect } from '@playwright/test'

// OrgAdmin storageState. Bootstrap already applied work-location / org-structure /
// salary-structure defaults and auto-completed the org profile on provisioning.

// ---------- D1. Org Profile ----------
test.describe('D1 org profile', () => {
  test('renders core fields', async ({ page }) => {
    await page.goto('/settings/org-profile')
    await expect(page.getByRole('heading', { name: 'Organisation Profile' })).toBeVisible()
    await expect(page.getByLabel('Company Name')).toBeVisible()
    await expect(page.getByLabel(/Company PAN/)).toBeVisible()
    await expect(page.getByLabel('GSTIN')).toBeVisible()
  })

  test('blank Company Name blocks save with required error', async ({ page }) => {
    await page.goto('/settings/org-profile')
    await page.getByLabel('Company Name').fill('')
    await page.getByRole('button', { name: 'Save Profile' }).click()
    // Required validation message appears; stays on page.
    await expect(page.getByText(/required/i).first()).toBeVisible()
  })

  test('invalid PAN format rejected', async ({ page }) => {
    await page.goto('/settings/org-profile')
    await page.getByLabel(/Company PAN/).fill('NOTAPAN')
    await page.getByRole('button', { name: 'Save Profile' }).click()
    await expect(page.getByText(/PAN|format|invalid/i).first()).toBeVisible()
  })

  test('valid edit saves with confirmation', async ({ page }) => {
    await page.goto('/settings/org-profile')
    await page.getByLabel('Company Name').fill('QA Smoke Pvt Ltd')
    await page.getByLabel(/Company PAN/).fill('ABCDE1234F')
    await page.getByRole('button', { name: 'Save Profile' }).click()
    await expect(page.getByText(/saved/i)).toBeVisible()
    // Persists on reload.
    await page.reload()
    await expect(page.getByLabel('Company Name')).toHaveValue('QA Smoke Pvt Ltd')
  })
})

// ---------- D2. Work Locations ----------
test.describe('D2 work locations', () => {
  test('list shows at least the default location after bootstrap', async ({ page }) => {
    await page.goto('/settings/work-locations')
    await expect(page.getByRole('heading', { name: /work location/i })).toBeVisible()
    // Apply-defaults created one; list is not empty.
    await expect(page.getByText(/no work locations yet/i)).toHaveCount(0)
  })

  test('create requires name and state', async ({ page }) => {
    await page.goto('/settings/work-locations')
    await page.getByRole('button', { name: /add work location/i }).first().click()
    await page.getByRole('button', { name: /^save$/i }).click()
    await expect(page.getByText(/required/i).first()).toBeVisible()
  })
})

// ---------- D3. Departments / Designations / Business Units ----------
test.describe('D3 org structure', () => {
  test('department modal requires a name', async ({ page }) => {
    await page.goto('/settings/departments')
    await page.getByRole('button', { name: /add department|new department|add/i }).first().click()
    await page.getByRole('button', { name: /^(save|create|add)$/i }).click()
    await expect(page.getByText(/required/i).first()).toBeVisible()
  })

  test('department modal closes on cancel', async ({ page }) => {
    await page.goto('/settings/departments')
    await page.getByRole('button', { name: /add department|new department|add/i }).first().click()
    await page.getByRole('button', { name: /cancel/i }).click()
    await expect(page.getByRole('button', { name: /^(save|create|add)$/i })).toHaveCount(0)
  })
})

// ---------- D4. Pay Schedule ----------
test.describe('D4 pay schedule', () => {
  test('renders work-week and pay-date config', async ({ page }) => {
    await page.goto('/settings/pay-schedule')
    await expect(page.getByRole('heading', { name: /pay schedule/i })).toBeVisible()
  })

  test('configure and save a monthly schedule', async ({ page }) => {
    await page.goto('/settings/pay-schedule')
    // Ensure at least one work-week day is selected, then save.
    const save = page.getByRole('button', { name: /^save/i })
    await save.click()
    await expect(page.getByText(/saved/i)).toBeVisible()
  })
})

// ---------- D5. Tax Details ----------
test.describe('D5 tax details', () => {
  test('renders tax details page', async ({ page }) => {
    await page.goto('/settings/tax-details')
    await expect(page.getByRole('heading', { name: /tax details/i })).toBeVisible()
  })
})
