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
// assets), but covers the realistic Day-0 channels: docs, support email, the
// public issue tracker, and the local MailHog inbox so dev/test users can find
// welcome emails.
//
// MailHog tile is only useful in dev; for now we render it always since this
// repo is dev-only. Production deployment will swap the URL or hide the tile
// via a build flag.
const RESOURCES: Resource[] = [
  {
    icon: <BookOpen className="w-4 h-4" />,
    label: 'Documentation',
    href: '/docs',
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
  {
    icon: <Inbox className="w-4 h-4" />,
    label: 'Test inbox (MailHog)',
    href: 'http://localhost:8025',
    external: true,
    description: 'Dev: welcome emails, payslips, and notifications land here.',
  },
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
