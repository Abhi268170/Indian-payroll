import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  timeout: 60_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    viewport: { width: 1440, height: 900 },
  },
  projects: [
    // Bootstrap: builds SuperAdmin + OrgAdmin sessions and a fresh onboarded org.
    { name: 'setup', testMatch: /.*\.setup\.ts/ },

    // SuperAdmin-scoped specs (platform admin, auth uses no state).
    {
      name: 'superadmin',
      testMatch: /platform(-extra)?\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], storageState: '.auth/superadmin.json' },
      dependencies: ['setup'],
    },

    // OrgAdmin-scoped specs (everything inside the main tenant).
    {
      name: 'orgadmin',
      testMatch: /(settings|statutory|components|employees|payroll|fnf|dashboard|crosscutting|calc|files|employee-detail)\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], storageState: '.auth/orgadmin.json' },
      dependencies: ['setup'],
    },

    // Pay-run state machine — dedicated CLEAN tenant (zero hard blocks).
    {
      name: 'payrun',
      testMatch: /payrun-state\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], storageState: '.auth/payrun.json' },
      dependencies: ['setup'],
    },

    // Auth specs run unauthenticated (own login flows).
    {
      name: 'auth',
      testMatch: /auth\.spec\.ts/,
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
    },
  ],
})
