import { type ReactElement, useEffect } from 'react'
import { Navigate } from 'react-router-dom'

// Phase 1 of the onboarding UX redesign (plan 2026-05-25-014) replaces the
// full-screen wizard with a SetupChecklistCard on the dashboard. The /onboarding
// and /onboarding/:stepId routes stay registered for one release so existing
// bookmarks, welcome-email links, and internal docs do not dead-link — they
// just redirect to /dashboard. Phase 4 deletes the route once nginx + Serilog
// access logs confirm zero hits over ~2 weeks.
export default function OnboardingWizardPage(): ReactElement {
  useEffect(() => {
    console.warn('[deprecated-route] /onboarding redirect → /dashboard')
  }, [])
  return <Navigate to="/dashboard" replace />
}
