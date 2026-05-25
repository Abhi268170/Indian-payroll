import { type ReactElement } from 'react'
import { BookOpen, Mail, MessageSquare, Inbox } from 'lucide-react'

interface Resource {
  icon: ReactElement
  label: string
  href: string
  external?: boolean
  description?: string
}

// Tasteful help block — no walkthrough video / phone yet (we don't have those
// assets), but covers the realistic Day-0 channels: docs (GitHub), forum,
// support email, and the local MailHog inbox (dev only) so test users can find
// welcome emails.
//
// IS_DEV controls the MailHog tile so it never ships in production builds.
// Vite injects import.meta.env.DEV at build time.
const IS_DEV = import.meta.env.DEV

const RESOURCES: Resource[] = [
  {
    icon: <BookOpen className="w-4 h-4" />,
    label: 'Documentation',
    // Point at the repo's docs/ tree on GitHub until an in-app /docs route exists.
    // The SPA wildcard would otherwise eat /docs and bounce to /.
    href: 'https://github.com/Abhi268170/Indian-payroll/tree/master/docs',
    external: true,
    description: 'Setup guides, payroll concepts, and API reference.',
  },
  {
    icon: <MessageSquare className="w-4 h-4" />,
    label: 'Forum & community',
    href: 'https://github.com/Abhi268170/Indian-payroll/discussions',
    external: true,
    description: 'Ask questions and share patterns with other admins.',
  },
  {
    icon: <Mail className="w-4 h-4" />,
    label: 'Email support',
    href: 'mailto:support@indianpayroll.local',
    description: "Reach the team directly — we reply within one business day.",
  },
  ...(IS_DEV
    ? [{
        icon: <Inbox className="w-4 h-4" />,
        label: 'Test inbox (MailHog)',
        href: 'http://localhost:8025',
        external: true,
        description: 'Dev: welcome emails, payslips, and notifications land here.',
      } satisfies Resource]
    : []),
]

export default function HelpResourcesCard(): ReactElement {
  return (
    <div className="bg-white rounded-xl border border-[var(--color-border)] p-5">
      <div className="mb-4">
        <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)]">Need help?</h2>
        <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
          Reach out anytime — we&apos;re here while you get the first run out.
        </p>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {RESOURCES.map(r => (
          <a
            key={r.label}
            href={r.href}
            target={r.external ? '_blank' : undefined}
            rel={r.external ? 'noopener noreferrer' : undefined}
            className="flex items-start gap-3 rounded-lg border border-[var(--color-border)] px-3 py-2.5 hover:border-[var(--color-border-strong)] transition-colors"
          >
            <div className="w-7 h-7 rounded-md bg-[var(--color-primary)]/10 text-[var(--color-primary)] flex items-center justify-center flex-shrink-0">
              {r.icon}
            </div>
            <div className="min-w-0">
              <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{r.label}</p>
              {r.description && (
                <p className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">{r.description}</p>
              )}
            </div>
          </a>
        ))}
      </div>
    </div>
  )
}
