import { test as setup, expect } from '@playwright/test'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'
import { SUPERADMIN_EMAIL, SUPERADMIN_PASSWORD } from '../helpers/env'
import { provisionAndOnboard } from '../helpers/bootstrap'

const here = dirname(fileURLToPath(import.meta.url))
const SUPERADMIN_STATE = resolve(here, '../.auth/superadmin.json')

setup('bootstrap: superadmin session', async ({ page }) => {
  expect(SUPERADMIN_PASSWORD, 'SUPERADMIN_PASSWORD must be set in repo .env').not.toBe('')
  await page.goto('/login')
  await page.locator('input[name="username"]').fill(SUPERADMIN_EMAIL)
  await page.locator('input[name="password"]').fill(SUPERADMIN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).toHaveURL(/\/platform\/orgs/)
  await page.context().storageState({ path: SUPERADMIN_STATE })
})

// Main tenant used by most orgadmin specs.
setup('bootstrap: provision + onboard main org', async ({ browser }) => {
  await provisionAndOnboard(browser, SUPERADMIN_STATE, 'smoke', 'orgadmin.json', 'org.json')
})

// Dedicated CLEAN tenant for the pay-run state-machine spec, so its run has zero
// hard blocks (no incomplete employees leaked in from other specs).
setup('bootstrap: provision + onboard payrun org', async ({ browser }) => {
  await provisionAndOnboard(browser, SUPERADMIN_STATE, 'payrun', 'payrun.json', 'payrun-org.json')
})
