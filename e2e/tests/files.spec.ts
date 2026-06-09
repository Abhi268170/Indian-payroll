import { test, expect } from '@playwright/test'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'
import { writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import * as XLSX from 'xlsx'

const here = dirname(fileURLToPath(import.meta.url))
const fx = (name: string): string => resolve(here, '../fixtures', name)

// Full import-template header set (order-independent; parser reads by name).
const IMPORT_HEADERS = [
  'EmployeeNumber', 'FirstName', 'MiddleName', 'LastName', 'Gender', 'DateOfJoining',
  'DateOfBirth', 'WorkEmail', 'PersonalEmail', 'MobileNumber', 'FathersName',
  'AddressLine1', 'AddressLine2', 'City', 'State', 'PinCode', 'PAN', 'Aadhaar',
  'Department', 'Designation', 'WorkLocation', 'EmploymentType', 'PaymentMode',
  'BankAccountHolderName', 'BankName', 'BankAccountNumber', 'IFSCCode',
  'BankAccountType', 'EpfEnabled', 'EsiEnabled', 'PtEnabled', 'LwfEnabled',
  'UAN', 'ESICNumber', 'AnnualCTC', 'SalaryStructureTemplate',
]

/** Write a populated import .xlsx (sheet "Employees") from a header→value map. */
function buildImportXlsx(row: Record<string, string>): string {
  const dataRow = IMPORT_HEADERS.map((h) => row[h] ?? '')
  const ws = XLSX.utils.aoa_to_sheet([IMPORT_HEADERS, dataRow])
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Employees')
  const path = resolve(tmpdir(), `emp-import-${Date.now()}.xlsx`)
  writeFileSync(path, XLSX.write(wb, { type: 'buffer', bookType: 'xlsx' }))
  return path
}

// OrgAdmin storageState. Logo upload + employee-import file flows.
// (Payslip PDF + bank advice downloads are covered in payrun-state.spec.ts where
//  an Approved/Paid run exists.)

test.describe('N file handling — logo', () => {
  test('valid PNG uploads', async ({ page }) => {
    await page.goto('/settings/org-profile')
    await page.setInputFiles('input[type="file"]', fx('logo.png'))
    await expect(page.getByText(/logo uploaded/i)).toBeVisible({ timeout: 15_000 })
  })

  test('wrong file type rejected', async ({ page }) => {
    await page.goto('/settings/org-profile')
    await page.setInputFiles('input[type="file"]', fx('wrong.txt'))
    await expect(page.getByText(/must be PNG\/JPEG under 2 MB/i)).toBeVisible({ timeout: 15_000 })
  })

  test('oversized file rejected', async ({ page }) => {
    await page.goto('/settings/org-profile')
    await page.setInputFiles('input[type="file"]', fx('too-big.png'))
    await expect(page.getByText(/must be PNG\/JPEG under 2 MB/i)).toBeVisible({ timeout: 15_000 })
  })
})

test.describe('N file handling — employee import', () => {
  test('download template produces an xlsx', async ({ page }) => {
    await page.goto('/employees/import')
    const dl = page.waitForEvent('download')
    await page.getByRole('button', { name: /download template/i }).click()
    const download = await dl
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/)
  })

  test('import page renders dropzone + overwrite toggle', async ({ page }) => {
    await page.goto('/employees/import')
    await expect(page.locator('input[type="file"]')).toBeAttached()
    await expect(page.getByText(/update existing/i)).toBeVisible()
  })

  // G-6 CLOSED: populated-xlsx validate → commit, using the org's real
  // dept/designation/work-location names (read from the wizard) so the row is valid.
  test('populated xlsx imports a new employee end-to-end', async ({ page }) => {
    // Read the org's actual org-structure names from the Add-Employee wizard.
    await page.goto('/employees/new')
    const dept = (await page.locator('select[name="departmentId"] option').nth(1).innerText()).trim()
    const desig = (await page.locator('select[name="designationId"] option').nth(1).innerText()).trim()
    const loc = (await page.locator('select[name="workLocationId"] option').nth(1).innerText()).trim()

    const stamp = String(Date.now()).slice(-7)
    const email = `import.${stamp}@example.com`
    const xlsxPath = buildImportXlsx({
      FirstName: 'Imported', LastName: `User${stamp}`, Gender: 'Female',
      DateOfJoining: '2026-04-01', DateOfBirth: '1994-03-10', WorkEmail: email,
      Department: dept, Designation: desig, WorkLocation: loc, EmploymentType: 'FullTime',
    })

    // Upload → validate.
    await page.goto('/employees/import')
    await page.setInputFiles('input[type="file"]', xlsxPath)
    await expect(page.getByText(/1 row.*ready|ready/i).first()).toBeVisible({ timeout: 20_000 })

    // Commit.
    await page.getByRole('button', { name: /import \d+ employee/i }).click()
    await expect(page.getByText(/import complete/i)).toBeVisible({ timeout: 30_000 })
    await expect(page.getByText(/1 employee.*added|added/i).first()).toBeVisible()

    // Verify the employee now appears in the list.
    await page.goto('/employees')
    await page.getByPlaceholder(/search by name/i).fill(`User${stamp}`)
    await expect(page.getByText(`Imported User${stamp}`).first()).toBeVisible({ timeout: 15_000 })
  })
})
