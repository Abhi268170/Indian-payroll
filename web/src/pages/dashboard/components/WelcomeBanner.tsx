import { type ReactElement, useState } from 'react'
import { X } from 'lucide-react'
import { useAuthStore } from '@/stores/authStore'
import { useOnboardingStatus } from '@/hooks/useOnboardingStatus'

const DISMISS_KEY_PREFIX = 'welcome-dismissed:'

// Friendly first-screen greeting for new tenant admins. Shows once per user
// (localStorage key includes the user id) while setup is incomplete. No
// confetti — this is a B2B payroll product, not a consumer onboarding flow.
//
// First-name source: per plan §7, Phase 1 uses an email-prefix fallback.
// A real firstName from AspNetUsers is a separate, deferred ticket.
export default function WelcomeBanner(): ReactElement | null {
  const user = useAuthStore(s => s.user)
  const { data: status } = useOnboardingStatus()
  const userKey = user?.sub ?? user?.email ?? ''
  const dismissKey = userKey ? `${DISMISS_KEY_PREFIX}${userKey}` : ''
  const [dismissed, setDismissed] = useState(() => {
    if (!dismissKey) return true
    try { return localStorage.getItem(dismissKey) === '1' } catch { return false }
  })

  if (dismissed) return null
  if (!status || status.setupComplete) return null

  const displayName = deriveDisplayName(user?.email)

  function handleDismiss(): void {
    if (dismissKey) {
      try { localStorage.setItem(dismissKey, '1') } catch { /* localStorage disabled — best-effort only */ }
    }
    setDismissed(true)
  }

  return (
    <div className="rounded-xl border border-[var(--color-primary)]/20 bg-gradient-to-r from-[var(--color-primary)]/8 to-transparent px-5 py-4 flex items-start justify-between gap-4">
      <div>
        <h2 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
          Welcome, {displayName} 👋
        </h2>
        <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">
          Let&apos;s get you ready to run your first payroll. Follow the checklist below — most steps take under a minute.
        </p>
      </div>
      <button
        type="button"
        onClick={handleDismiss}
        aria-label="Dismiss welcome message"
        className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] flex-shrink-0"
      >
        <X className="w-4 h-4" />
      </button>
    </div>
  )
}

function deriveDisplayName(email: string | undefined): string {
  if (!email) return 'there'
  const prefix = email.split('@')[0] ?? ''
  if (!prefix) return 'there'
  // Title-case the prefix and replace common separators with spaces so
  // "asha.nair" → "Asha Nair", "admin_user" → "Admin User", "abhi" → "Abhi".
  return prefix
    .split(/[._-]+/)
    .filter(s => s.length > 0)
    .map(s => s.charAt(0).toUpperCase() + s.slice(1))
    .join(' ')
}
