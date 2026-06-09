import { test, expect, type Page } from '@playwright/test'
import { getSetPasswordLink } from '../helpers/mailhog'

// Uses SuperAdmin storageState (see playwright.config.ts "superadmin" project).
// Each mutating test provisions its OWN throwaway org so it never disturbs the
// bootstrap fixture org and does not depend on sibling test ordering.
// NOTE: /connect/token and /api/auth/* share a 5-requests/60s rate limiter, so
// auth-heavy tests (B3) are kept lean (set-password + one blocked login).

async function provision(page: Page, name: string, email: string): Promise<void> {
  await page.goto('/platform/orgs/new')
  await page.getByRole('textbox', { name: 'Acme Corp' }).fill(name)
  await page.getByRole('textbox', { name: 'admin@acme.com' }).fill(email)
  await page.getByRole('button', { name: 'Provision Organisation' }).click()
}

// ---------- B1. Tenants list ----------
test('B1 tenants list renders header, button, columns', async ({ page }) => {
  await page.goto('/platform/orgs')
  await expect(page.getByRole('heading', { name: 'Organisations' })).toBeVisible()
  await expect(page.getByRole('button', { name: '+ Provision New Organisation' })).toBeVisible()
  await expect(page.getByRole('columnheader', { name: 'Name' })).toBeVisible()
  await expect(page.getByRole('columnheader', { name: 'Slug' })).toBeVisible()
  await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  await expect(page.getByRole('columnheader', { name: 'Created' })).toBeVisible()
})

// ---------- B2. Provision validation + slug rules ----------
test('B2 slug auto-derives from organisation name', async ({ page }) => {
  await page.goto('/platform/orgs/new')
  await page.getByRole('textbox', { name: 'Acme Corp' }).fill('Hello World Inc')
  await expect(page.getByRole('textbox', { name: 'acme-corp' })).toHaveValue('hello-world-inc')
})

test('B2 provision valid org appears in list', async ({ page }) => {
  const stamp = String(Date.now()).slice(-7)
  const slug = `qa-prov-${stamp}`
  await provision(page, `QA Prov ${stamp}`, `qa.prov.${stamp}@example.com`)
  await expect(page).toHaveURL(/\/platform\/orgs$/)
  await expect(page.getByText(slug)).toBeVisible()
})

test('B2 duplicate slug rejected with conflict message', async ({ page }) => {
  const stamp = String(Date.now()).slice(-7)
  const name = `QA Dup ${stamp}`
  const slug = `qa-dup-${stamp}`
  // First provision succeeds.
  await provision(page, name, `qa.dup.${stamp}@example.com`)
  await expect(page.getByText(slug)).toBeVisible()
  // Second provision of the SAME slug must be rejected.
  await provision(page, name, `qa.dup2.${stamp}@example.com`)
  await expect(page.getByText('That slug is already taken. Choose a different one.')).toBeVisible()
  await expect(page).toHaveURL(/\/platform\/orgs\/new/)
})

// ---------- B3. Org detail suspend / activate + suspended login block ----------
test('B3 suspend then activate; suspended org blocks login', async ({ page, browser }) => {
  const stamp = String(Date.now()).slice(-7)
  const name = `QA Plat ${stamp}`
  const slug = `qa-plat-${stamp}`
  const email = `qa.plat.${stamp}@example.com`

  await provision(page, name, email)
  await expect(page.getByText(slug)).toBeVisible()

  // Open detail via the row that contains the slug.
  await page.locator('tr', { hasText: slug }).click()
  await expect(page).toHaveURL(/\/platform\/orgs\/[0-9a-f-]+/)

  // Set the org admin password (so the suspended-login test is meaningful).
  const link = await getSetPasswordLink(email)
  const orgCtx = await browser.newContext()
  const orgPage = await orgCtx.newPage()
  await orgPage.goto(link)
  // NOTE: backend Identity requires >=12 chars (frontend says "Minimum 8" — see
  // divergence finding in RESULTS.md / auth.spec). Use a 12+ char password here.
  await orgPage.locator('input[name="newPassword"]').fill('QaPlat@2026!')
  await orgPage.locator('input[name="confirmPassword"]').fill('QaPlat@2026!')
  await orgPage.getByRole('button', { name: 'Set Password' }).click()
  await expect(orgPage.getByRole('heading', { name: 'Password set' })).toBeVisible()

  // Suspend from detail page.
  await page.getByRole('button', { name: /suspend/i }).click()
  await expect(page.getByText('Suspended')).toBeVisible()

  // Suspended org admin cannot log in.
  await orgPage.goto('/login')
  await orgPage.locator('input[name="username"]').fill(email)
  await orgPage.locator('input[name="password"]').fill('QaPlat@2026!')
  await orgPage.getByRole('button', { name: 'Sign in' }).click()
  await expect(orgPage.getByText('Invalid credentials or server error.')).toBeVisible()
  await expect(orgPage).toHaveURL(/\/login/)

  // Reactivate.
  await page.getByRole('button', { name: /activate/i }).click()
  await expect(page.getByText('Active')).toBeVisible()
  await orgCtx.close()
})
