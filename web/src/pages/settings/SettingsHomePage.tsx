import { type ReactElement } from 'react'
import { Link } from 'react-router-dom'
import { Building2, SlidersHorizontal, Receipt } from 'lucide-react'
import { type LucideIcon } from 'lucide-react'

interface SettingsItem {
  label: string
  to: string
}

interface SettingsGroup {
  heading: string
  icon: LucideIcon
  iconBg: string
  iconColor: string
  items: SettingsItem[]
}

interface SettingsSection {
  title: string
  groups: SettingsGroup[]
}

const SECTIONS: SettingsSection[] = [
  {
    title: 'Organisation Settings',
    groups: [
      {
        heading: 'Organisation',
        icon: Building2,
        iconBg: 'bg-emerald-50',
        iconColor: 'text-emerald-600',
        items: [
          { label: 'Organisation Profile', to: '/settings/org-profile' },
          { label: 'Work Locations', to: '/settings/work-locations' },
          { label: 'Departments', to: '/settings/departments' },
          { label: 'Designations', to: '/settings/designations' },
          { label: 'Business Units', to: '/settings/business-units' },
        ],
      },
      {
        heading: 'Setup & Configuration',
        icon: SlidersHorizontal,
        iconBg: 'bg-orange-50',
        iconColor: 'text-orange-500',
        items: [
          { label: 'Pay Schedule', to: '/settings/pay-schedule' },
          { label: 'Salary Components', to: '/settings/salary-components' },
          { label: 'Salary Structures', to: '/settings/salary-structures' },
          { label: 'Statutory Components', to: '/settings/statutory' },
        ],
      },
      {
        heading: 'Taxes',
        icon: Receipt,
        iconBg: 'bg-red-50',
        iconColor: 'text-red-500',
        items: [
          { label: 'Tax Details', to: '/settings/tax-details' },
        ],
      },
    ],
  },
]

function SettingsCard({ heading, icon: Icon, iconBg, iconColor, items }: SettingsGroup): ReactElement {
  return (
    <div className="flex-1 min-w-[180px] border border-[var(--color-border)] rounded-xl p-5 flex flex-col">
      <div className="flex items-center gap-2.5 mb-4">
        <div className={`w-7 h-7 rounded-lg ${iconBg} flex items-center justify-center flex-shrink-0`}>
          <Icon className={`w-4 h-4 ${iconColor}`} />
        </div>
        <span className="text-[13px] font-semibold text-[var(--color-text-primary)]">{heading}</span>
      </div>
      <ul className="space-y-2.5">
        {items.map(item => (
          <li key={item.to}>
            <Link
              to={item.to}
              className="text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors"
            >
              {item.label}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}

export default function SettingsHomePage(): ReactElement {
  return (
    <div className="max-w-5xl mx-auto px-8 py-8 space-y-6">
      {SECTIONS.map(section => (
        <div key={section.title} className="bg-white rounded-2xl border border-[var(--color-border)] p-6">
          <h2 className="text-[15px] font-semibold text-[var(--color-text-primary)] mb-5">
            {section.title}
          </h2>
          <div className="flex gap-4">
            {section.groups.map(group => (
              <SettingsCard key={group.heading} {...group} />
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}
