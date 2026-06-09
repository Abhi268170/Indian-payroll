import { test, expect } from '@playwright/test'
import { createPayableEmployee } from '../helpers/employee'

// OrgAdmin storageState. Employee detail tabs + sensitive-field masking.
// PAN 'ABCDE1234F' → masked 'XXXXX1234'; account '12345678901' → masked 'XXXX8901'.

test.describe('O employee detail tabs + masking', () => {
  test('five tabs render and switch', async ({ page }) => {
    await createPayableEmployee(page, 840000, 'tabs', { pan: 'ABCDE1234F' })
    await expect(page.getByRole('button', { name: 'Overview' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Salary Details' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Tax' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Investments' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Payslips & Forms' })).toBeVisible()
  })

  test('PAN and bank account render masked in Overview', async ({ page }) => {
    await createPayableEmployee(page, 840000, 'mask', { pan: 'ABCDE1234F' })
    // Force a fresh detail GET (the immediate post-wizard render can be stale re: masked fields).
    await page.reload()
    await expect(page.getByRole('heading', { name: 'Personal Information' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Payment Information' })).toBeVisible()
    // PAN 'ABCDE1234F' → maskedPAN 'XXXXX234F'; account '12345678901' → 'XXXX8901'.
    await expect(page.getByText('XXXXX234F')).toBeVisible()
    await expect(page.getByText('XXXX8901')).toBeVisible()
  })

  test('Investments tab shows coming-soon', async ({ page }) => {
    await createPayableEmployee(page, 840000, 'inv', { pan: 'ABCDE1234F' })
    await page.getByRole('button', { name: 'Investments' }).click()
    await expect(page.getByText(/coming soon/i)).toBeVisible()
  })

  test('Payslips tab shows empty state before any run', async ({ page }) => {
    await createPayableEmployee(page, 840000, 'paysl', { pan: 'ABCDE1234F' })
    await page.getByRole('button', { name: 'Payslips & Forms' }).click()
    await expect(page.getByText(/no payslips available yet/i)).toBeVisible()
  })

  test('Tax tab: set FY opening balances and persist', async ({ page }) => {
    await createPayableEmployee(page, 840000, 'tax', { pan: 'ABCDE1234F' })
    await page.getByRole('button', { name: 'Tax' }).click()
    // FY dropdown present.
    await expect(page.locator('select').first()).toBeVisible()
    // Enter edit mode (Add or Edit).
    await page.getByRole('button', { name: /^(add|edit)$/i }).first().click()
    const nums = page.getByRole('spinbutton')
    await nums.nth(0).fill('3') // Months
    await nums.nth(1).fill('150000') // Gross
    await nums.nth(2).fill('5000') // TDS
    await nums.nth(3).fill('6000') // PF
    await page.getByRole('button', { name: /^save/i }).click()
    // Persisted value shows in read view.
    await expect(page.getByText(/1,50,000/)).toBeVisible({ timeout: 15_000 })
  })
})
