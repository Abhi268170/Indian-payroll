import { test, expect } from '@playwright/test'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'
import { SUPERADMIN_EMAIL, SUPERADMIN_PASSWORD } from '../helpers/env'

const here = dirname(fileURLToPath(import.meta.url))
const org = JSON.parse(readFileSync(resolve(here, '../.auth/org.json'), 'utf8')) as {
  adminEmail: string
  adminPassword: string
}

// ---------- A1. Login form ----------
test.describe('A1 login form', () => {
  test('blank email shows validation error', async ({ page }) => {
    await page.goto('/login')
    await page.locator('input[name="password"]').fill('whatever')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByText('Enter a valid email'), 'blank email must be rejected').toBeVisible()
  })

  test('blank password shows Required', async ({ page }) => {
    await page.goto('/login')
    await page.locator('input[name="username"]').fill('someone@example.com')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByText('Required'), 'blank password must be rejected').toBeVisible()
  })

  test('wrong password for real user shows credential error and stays on /login', async ({ page }) => {
    await page.goto('/login')
    await page.locator('input[name="username"]').fill(org.adminEmail)
    await page.locator('input[name="password"]').fill('WrongPass@9999')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByText('Invalid credentials or server error.')).toBeVisible()
    await expect(page).toHaveURL(/\/login/)
  })

  test('unknown email shows same credential error (no enumeration)', async ({ page }) => {
    await page.goto('/login')
    await page.locator('input[name="username"]').fill('nobody.unknown@example.com')
    await page.locator('input[name="password"]').fill('WrongPass@9999')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByText('Invalid credentials or server error.')).toBeVisible()
  })

  test('valid SuperAdmin creds redirect to platform orgs', async ({ page }) => {
    await page.goto('/login')
    await page.locator('input[name="username"]').fill(SUPERADMIN_EMAIL)
    await page.locator('input[name="password"]').fill(SUPERADMIN_PASSWORD)
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page).toHaveURL(/\/platform\/orgs/)
  })

  test('valid OrgAdmin creds redirect to dashboard', async ({ page }) => {
    await page.goto('/login')
    await page.locator('input[name="username"]').fill(org.adminEmail)
    await page.locator('input[name="password"]').fill(org.adminPassword)
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page).toHaveURL(/\/dashboard/)
  })
})

// ---------- A2. Forgot / set password ----------
test.describe('A2 forgot/set password', () => {
  test('forgot password blank email shows Required', async ({ page }) => {
    await page.goto('/forgot-password')
    await page.getByRole('button', { name: /send|reset|link/i }).click()
    await expect(page.getByText('Required')).toBeVisible()
  })

  test('forgot password valid email shows non-enumeration message', async ({ page }) => {
    await page.goto('/forgot-password')
    await page.locator('input[type="email"], input[name="email"]').first().fill(org.adminEmail)
    await page.getByRole('button', { name: /send|reset|link/i }).click()
    await expect(page.getByText(/if that email exists/i)).toBeVisible()
  })

  test('forgot password nonexistent email shows identical message', async ({ page }) => {
    await page.goto('/forgot-password')
    await page.locator('input[type="email"], input[name="email"]').first().fill('ghost.user@example.com')
    await page.getByRole('button', { name: /send|reset|link/i }).click()
    await expect(page.getByText(/if that email exists/i)).toBeVisible()
  })

  test('set-password with no token/email params shows error', async ({ page }) => {
    await page.goto('/set-password')
    await expect(page.getByText(/invalid or missing link/i)).toBeVisible()
  })

  test('set-password rejects short password', async ({ page }) => {
    await page.goto(`/set-password?token=dummy&email=${encodeURIComponent(org.adminEmail)}`)
    await page.locator('input[name="newPassword"]').fill('Ab1@')
    await page.locator('input[name="confirmPassword"]').fill('Ab1@')
    await page.getByRole('button', { name: 'Set Password' }).click()
    await expect(page.getByText(/8|character/i).first()).toBeVisible()
  })

  // F-1 FIXED: client now enforces the real 12-char minimum (was min 8, diverging
  // from backend Identity min 12). An 8-11 char password is rejected client-side.
  test('set-password rejects an 8-11 char password client-side (min 12)', async ({ page }) => {
    await page.goto(`/set-password?token=dummy&email=${encodeURIComponent(org.adminEmail)}`)
    await page.locator('input[name="newPassword"]').fill('Valid@99x') // 9 chars
    await page.locator('input[name="confirmPassword"]').fill('Valid@99x')
    await page.getByRole('button', { name: 'Set Password' }).click()
    await expect(page.getByText(/Minimum 12 characters/i)).toBeVisible()
  })

  test('set-password rejects mismatched confirm', async ({ page }) => {
    await page.goto(`/set-password?token=dummy&email=${encodeURIComponent(org.adminEmail)}`)
    await page.locator('input[name="newPassword"]').fill('Valid@2026')
    await page.locator('input[name="confirmPassword"]').fill('Other@2026')
    await page.getByRole('button', { name: 'Set Password' }).click()
    await expect(page.getByText(/match/i)).toBeVisible()
  })
})

// ---------- A3. Guards & redirects ----------
test.describe('A3 guards', () => {
  test('anonymous hitting /dashboard redirects to /login', async ({ browser }) => {
    const ctx = await browser.newContext() // no storage state
    const page = await ctx.newPage()
    await page.goto('/dashboard')
    await expect(page).toHaveURL(/\/login/)
    await ctx.close()
  })

  test('anonymous hitting /platform/orgs redirects to /login', async ({ browser }) => {
    const ctx = await browser.newContext()
    const page = await ctx.newPage()
    await page.goto('/platform/orgs')
    await expect(page).toHaveURL(/\/login/)
    await ctx.close()
  })

  test('unknown route redirects to /login when anonymous', async ({ browser }) => {
    const ctx = await browser.newContext()
    const page = await ctx.newPage()
    await page.goto('/zzz-nonexistent')
    await expect(page).toHaveURL(/\/login/)
    await ctx.close()
  })
})
