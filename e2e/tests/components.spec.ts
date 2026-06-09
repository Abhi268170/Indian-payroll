import { test, expect } from '@playwright/test'

// OrgAdmin storageState. Salary components page: 6 category tabs + an Add dropdown
// with 5 component types. Bootstrap's salary-structure defaults created components.

test.describe('F salary components', () => {
  test('components page renders with category tabs', async ({ page }) => {
    await page.goto('/settings/salary-components')
    await expect(page.getByText('Earnings').first()).toBeVisible()
    await expect(page.getByText('Deductions').first()).toBeVisible()
    await expect(page.getByText('Benefits').first()).toBeVisible()
  })

  test('default components exist after bootstrap (e.g. Basic / HRA)', async ({ page }) => {
    await page.goto('/settings/salary-components')
    await expect(page.getByText(/Basic/i).first()).toBeVisible()
  })

  test('Add Component → Earning opens the earning modal', async ({ page }) => {
    await page.goto('/settings/salary-components')
    await page.getByRole('button', { name: 'Add Component' }).click()
    await page.getByRole('button', { name: 'Earning', exact: true }).click()
    // Modal opened with its first field.
    await expect(page.getByText('Earning Name').first()).toBeVisible()
    await expect(page.getByText('Earning Type').first()).toBeVisible()
  })
})

test.describe('F salary structures', () => {
  test('structures list renders', async ({ page }) => {
    await page.goto('/settings/salary-structures')
    await expect(page.getByRole('heading', { name: /salary structure/i }).first()).toBeVisible()
  })

  test('structure builder opens', async ({ page }) => {
    await page.goto('/settings/salary-structures/new')
    // Builder page renders some composition UI (CTC / component related).
    await expect(page.getByText(/CTC|component|structure/i).first()).toBeVisible()
  })
})
