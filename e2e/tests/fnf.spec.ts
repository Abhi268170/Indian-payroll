import { test, expect, type Page } from '@playwright/test'
import { createPayableEmployee, assignTaxDeductor } from '../helpers/employee'

// OrgAdmin storageState. Exit initiation → FnF settlement → cancel exit.
// F-3 FIXED: relieving-letter generation is now best-effort, so exit no longer
// 500s; the exit commits and the FnF settlement page is reached.

async function fillExitForm(page: Page): Promise<void> {
  const lwd = page.getByLabel('Last Working Day')
  await lwd.click()
  await lwd.pressSequentially('30/06/2026')
  await lwd.blur()
  await page.locator('select').first().selectOption('ResignedByEmployee')
  const proceed = page.getByRole('button', { name: /proceed/i })
  await expect(proceed).toBeEnabled({ timeout: 10_000 })
  await proceed.click()
}

test.describe('M FnF settlement + exit', () => {
  test('initiate exit → FnF page → save settlement inputs', async ({ page }) => {
    const deductor = await createPayableEmployee(page, 700000, 'ded1', { pan: 'ABCDE1234F' })
    await assignTaxDeductor(page, deductor)

    await createPayableEmployee(page, 840000, 'fnf', { pan: 'ABCDE1234F' })
    const empUrl = page.url()

    await page.getByRole('button', { name: /more actions/i }).click()
    await page.getByRole('button', { name: 'Initiate Exit Process' }).click()
    await expect(page).toHaveURL(/\/exit\/initiate/)
    await fillExitForm(page)

    await expect(page).toHaveURL(/\/pay-runs\/[0-9a-f-]+\/fnf/, { timeout: 30_000 })
    await expect(page.getByRole('heading', { name: /final settlement/i })).toBeVisible()

    // Bonus = 2nd number field (1st = LOP Days).
    await page.getByRole('spinbutton').nth(1).fill('25000')
    await page.getByRole('button', { name: /save and continue/i }).click()
    await expect(page).toHaveURL(/\/pay-runs/, { timeout: 30_000 })

    // Employee now in exit state.
    await page.goto(empUrl)
    await page.getByRole('button', { name: /more actions/i }).click()
    await expect(page.getByRole('button', { name: 'Cancel Exit Process' })).toBeVisible()
  })

  test('cancel exit restores employee to Active', async ({ page }) => {
    const deductor = await createPayableEmployee(page, 700000, 'ded2', { pan: 'ABCDE1234F' })
    await assignTaxDeductor(page, deductor)

    await createPayableEmployee(page, 840000, 'fnfcx', { pan: 'ABCDE1234F' })
    const empUrl = page.url()

    await page.getByRole('button', { name: /more actions/i }).click()
    await page.getByRole('button', { name: 'Initiate Exit Process' }).click()
    await fillExitForm(page)
    await expect(page).toHaveURL(/\/fnf/, { timeout: 30_000 })

    await page.goto(empUrl)
    page.on('dialog', d => d.accept())
    await page.getByRole('button', { name: /more actions/i }).click()
    await page.getByRole('button', { name: 'Cancel Exit Process' }).click()

    await expect(page.getByText('Active').first()).toBeVisible({ timeout: 15_000 })
    await page.getByRole('button', { name: /more actions/i }).click()
    await expect(page.getByRole('button', { name: 'Initiate Exit Process' })).toBeVisible()
  })
})
