import { expect, type Browser } from '@playwright/test'
import { writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'
import { getSetPasswordLink } from './mailhog'

const here = dirname(fileURLToPath(import.meta.url))
const authDir = resolve(here, '../.auth')

const ORG_ADMIN_PASSWORD = 'QaSmoke@2026'

export interface OrgInfo {
  orgName: string
  slug: string
  adminEmail: string
  adminPassword: string
}

/** Provision a fresh tenant as SuperAdmin and fully onboard its OrgAdmin entirely
 *  through the UI (apply-defaults + pay schedule). Saves the OrgAdmin storageState
 *  to `.auth/<stateFile>` and the org metadata to `.auth/<infoFile>`. */
export async function provisionAndOnboard(
  browser: Browser,
  superStatePath: string,
  scope: string,
  stateFile: string,
  infoFile: string,
): Promise<OrgInfo> {
  const runId = `${Date.now().toString().slice(-7)}${scope}`
  const orgName = `QA ${scope} ${runId}`
  const slug = `qa-${scope}-${runId}`.toLowerCase()
  const adminEmail = `qa.${scope}.${runId}@example.com`.toLowerCase()

  // SuperAdmin provisions the org.
  const adminCtx = await browser.newContext({ storageState: superStatePath })
  const admin = await adminCtx.newPage()
  await admin.goto('/platform/orgs/new')
  await admin.getByRole('textbox', { name: 'Acme Corp' }).fill(orgName)
  await admin.getByRole('textbox', { name: 'admin@acme.com' }).fill(adminEmail)
  await admin.getByRole('textbox', { name: 'acme-corp' }).fill(slug)
  await admin.getByRole('button', { name: 'Provision Organisation' }).click()
  // Provisioning creates a PG schema + runs migrations — can take >10s under load.
  await expect(admin).toHaveURL(/\/platform\/orgs$/, { timeout: 45_000 })
  await expect(admin.getByText(slug)).toBeVisible({ timeout: 15_000 })
  await adminCtx.close()

  // OrgAdmin sets password from the emailed link (MailHog = the inbox).
  const link = await getSetPasswordLink(adminEmail)
  const orgCtx = await browser.newContext()
  const org = await orgCtx.newPage()
  await org.goto(link)
  await org.locator('input[name="newPassword"]').fill(ORG_ADMIN_PASSWORD)
  await org.locator('input[name="confirmPassword"]').fill(ORG_ADMIN_PASSWORD)
  await org.getByRole('button', { name: 'Set Password' }).click()
  await expect(org.getByRole('heading', { name: 'Password set' })).toBeVisible()

  // OrgAdmin logs in.
  await org.goto('/login')
  await org.locator('input[name="username"]').fill(adminEmail)
  await org.locator('input[name="password"]').fill(ORG_ADMIN_PASSWORD)
  await org.getByRole('button', { name: 'Sign in' }).click()
  await expect(org).toHaveURL(/\/dashboard/)

  // Apply onboarding defaults (real UI buttons), waiting for each row to complete.
  await expect(org.getByText('Get started')).toBeVisible({ timeout: 15_000 })
  for (const stepText of ['Work Locations', 'Departments', 'Salary Structure']) {
    const row = org.getByRole('listitem').filter({ hasText: stepText })
    const applyBtn = row.getByRole('button', { name: 'Apply defaults' })
    if ((await applyBtn.count()) > 0) {
      await applyBtn.first().click()
      await expect(row.getByText('Completed')).toBeVisible({ timeout: 15_000 })
    }
  }

  // Configure pay schedule (default Mon-Fri) so a current period exists.
  await org.goto('/settings/pay-schedule')
  const saveSchedule = org.getByRole('button', { name: /^save/i })
  await expect(saveSchedule).toBeVisible({ timeout: 15_000 })
  await saveSchedule.click()
  await expect(org.getByText(/saved/i)).toBeVisible({ timeout: 15_000 })

  // Verify Add Employee enabled, then persist state + info.
  await org.goto('/employees')
  await expect(org.getByRole('button', { name: /add employee/i }).first()).toBeEnabled({ timeout: 15_000 })

  const info: OrgInfo = { orgName, slug, adminEmail, adminPassword: ORG_ADMIN_PASSWORD }
  await orgCtx.storageState({ path: resolve(authDir, stateFile) })
  writeFileSync(resolve(authDir, infoFile), JSON.stringify(info, null, 2))
  await orgCtx.close()
  return info
}
