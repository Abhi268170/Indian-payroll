import { type ReactElement } from 'react'
import { Outlet, useNavigate, useSearchParams, NavLink, Link } from 'react-router-dom'
import { X, ArrowLeft } from 'lucide-react'
import { clsx } from 'clsx'

interface NavItem {
  label: string
  to: string
}

interface NavSection {
  heading: string
  items: NavItem[]
}

const NAV: NavSection[] = [
  {
    heading: 'Org Structure',
    items: [
      { label: 'Work Locations', to: '/settings/work-locations' },
      { label: 'Departments', to: '/settings/departments' },
      { label: 'Designations', to: '/settings/designations' },
      { label: 'Business Units', to: '/settings/business-units' },
    ],
  },
  {
    heading: 'Setup & Configuration',
    items: [
      { label: 'Pay Schedule', to: '/settings/pay-schedule' },
      { label: 'Salary Components', to: '/settings/salary-components' },
      { label: 'Salary Structures', to: '/settings/salary-structures' },
      { label: 'Statutory Components', to: '/settings/statutory' },
    ],
  },
  {
    heading: 'Taxes',
    items: [
      { label: 'Tax Details', to: '/settings/tax-details' },
    ],
  },
]

function SidebarSection({ heading, items, query }: NavSection & { query: string }): ReactElement {
  return (
    <div className="mb-1">
      <div className="px-4 py-2">
        <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {heading}
        </span>
      </div>
      <ul>
        {items.map(item => (
          <li key={item.to}>
            <NavLink
              to={`${item.to}${query}`}
              end={false}
              className={({ isActive }) =>
                clsx(
                  'block px-4 py-2 text-[13px] transition-colors',
                  isActive
                    ? 'bg-[var(--color-primary-light)] text-[var(--color-primary)] font-medium'
                    : 'text-[var(--color-text-secondary)] hover:bg-gray-50 hover:text-[var(--color-text-primary)]',
                )
              }
            >
              {item.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </div>
  )
}

export default function SettingsLayout(): ReactElement {
  const navigate = useNavigate()
  const [search] = useSearchParams()
  // When ?return=onboarding is present (set by the onboarding wizard when it deep-links
  // into a settings page), surface a banner that takes the user back to the wizard step
  // instead of leaving them stranded in Settings.
  const returnTo = search.get('return')
  const wizardStep = search.get('step')
  const fromOnboarding = returnTo === 'onboarding'
  const onboardingHref = wizardStep ? `/onboarding/${wizardStep}` : '/onboarding'
  // Preserve ?return=onboarding (and the originating step) when navigating between
  // settings pages so the return banner stays sticky after a sidebar click. Without
  // this the user loses the "Back to setup" context after one navigation.
  const sidebarQuery = fromOnboarding
    ? `?return=onboarding${wizardStep ? `&step=${encodeURIComponent(wizardStep)}` : ''}`
    : ''

  function handleClose(): void {
    void navigate(fromOnboarding ? onboardingHref : '/dashboard')
  }

  return (
    <div className="fixed inset-0 z-50 bg-[var(--color-page-bg)] flex flex-col">
      <header className="h-14 flex-shrink-0 bg-white border-b border-[var(--color-border)] flex items-center justify-between px-6">
        <div className="flex items-center gap-3">
          <button
            onClick={() => { void navigate(-1) }}
            className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-[var(--color-text-secondary)] hover:bg-gray-100 transition-colors"
            aria-label="Go back"
          >
            <ArrowLeft className="w-4 h-4" />
          </button>
          <div className="flex items-center gap-2.5">
            <div className="w-7 h-7 rounded-lg bg-[var(--color-primary)] flex items-center justify-center flex-shrink-0">
              <span className="text-[11px] font-bold text-white">IP</span>
            </div>
            <span className="text-[15px] font-semibold text-[var(--color-text-primary)]">Settings</span>
          </div>
        </div>
        <button
          onClick={handleClose}
          className="inline-flex items-center gap-1.5 h-8 px-3 rounded-lg text-[13px] text-[var(--color-text-secondary)] hover:bg-gray-100 transition-colors"
        >
          <X className="w-4 h-4" />
          {fromOnboarding ? 'Back to setup' : 'Close Settings'}
        </button>
      </header>

      {fromOnboarding && (
        <div className="flex-shrink-0 bg-[var(--color-primary)]/5 border-b border-[var(--color-primary)]/20 px-6 py-2 flex items-center justify-between">
          <p className="text-[12px] text-[var(--color-text-primary)]">
            Configuring this from the setup wizard. Save here, then return to continue.
          </p>
          <Link to={onboardingHref} className="text-[12px] font-medium text-[var(--color-primary)] hover:underline">
            Return to setup →
          </Link>
        </div>
      )}

      <div className="flex flex-1 overflow-hidden">
        <nav className="w-52 flex-shrink-0 bg-white border-r border-[var(--color-border)] overflow-y-auto py-3">
          {NAV.map(section => (
            <SidebarSection key={section.heading} {...section} query={sidebarQuery} />
          ))}
        </nav>
        <div className="flex-1 overflow-y-auto">
          <Outlet />
        </div>
      </div>
    </div>
  )
}
