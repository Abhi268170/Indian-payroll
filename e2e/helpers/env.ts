import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const here = dirname(fileURLToPath(import.meta.url))

/** Parse the repo-root .env (one level above e2e/) for the seeded SuperAdmin creds.
 *  The SuperAdmin account is env-seeded and has no UI creation path — this is the
 *  single documented exception to "everything through the UI" (see GAPS.md). */
function loadDotEnv(): Record<string, string> {
  const envPath = resolve(here, '../../.env')
  const out: Record<string, string> = {}
  for (const line of readFileSync(envPath, 'utf8').split('\n')) {
    const m = line.match(/^([A-Z0-9_]+)=(.*)$/)
    if (m) out[m[1]] = m[2].replace(/^"(.*)"$/, '$1')
  }
  return out
}

const env = loadDotEnv()

export const SUPERADMIN_EMAIL = env.SUPERADMIN_EMAIL ?? 'admin@payroll.local'
export const SUPERADMIN_PASSWORD = env.SUPERADMIN_PASSWORD ?? ''

export const MAILHOG_URL = 'http://localhost:8025'

/** Run-scoped unique suffix so repeated runs don't collide (no DB reset available). */
export const RUN_ID = String(Date.now()).slice(-7)
