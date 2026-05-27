import { type ReactElement, type ReactNode } from 'react'
import { ShieldCheck, FileText, Calculator } from 'lucide-react'
import authBgUrl from '@/assets/auth-bg.webp'
import BrandMark from '@/components/BrandMark'

interface AuthLayoutProps {
  // Right-column heading (e.g. "Sign in", "Forgot password").
  title: string
  // Right-column sub-heading directly under the title.
  subtitle?: string
  children: ReactNode
}

// Two-column auth chrome. Left = brand panel (dark, marketing). Right = form panel.
// Collapses to single column under md so the brand panel becomes a slim header strip.
export default function AuthLayout({ title, subtitle, children }: AuthLayoutProps): ReactElement {
  return (
    <div className="min-h-screen flex flex-col md:flex-row bg-[var(--color-page-bg)]">
      {/* Brand / marketing column */}
      <aside
        className="md:w-1/2 lg:w-2/5 text-white px-6 py-8 md:p-12 flex flex-col"
        style={{
          backgroundImage: `linear-gradient(rgba(15,23,42,0.35), rgba(15,23,42,0.65)), url(${authBgUrl})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
        }}
      >
        <div className="flex items-center gap-3 mb-12">
          <BrandMark size="lg" />
          <span className="text-[17px] font-semibold">Indian Payroll</span>
        </div>

        <div className="hidden md:flex flex-col flex-1 justify-center max-w-md">
          <h2 className="text-[28px] leading-tight font-semibold mb-3">
            Payroll built for Indian compliance.
          </h2>
          <p className="text-[14px] text-slate-300 mb-10">
            EPF, ESI, Professional Tax, LWF, TDS — calculated correctly, every month.
          </p>

          <ul className="space-y-5">
            <FeatureRow
              icon={<Calculator className="w-4 h-4" />}
              title="New regime TDS"
              body="Slab-correct annual projection with full prior-employer YTD handling."
            />
            <FeatureRow
              icon={<ShieldCheck className="w-4 h-4" />}
              title="Multi-tenant by design"
              body="Schema-per-tenant isolation. Your data never crosses an org boundary."
            />
            <FeatureRow
              icon={<FileText className="w-4 h-4" />}
              title="Audit-ready statutory"
              body="Every payroll run snapshots the config that produced it."
            />
          </ul>
        </div>

        <p className="hidden md:block text-[11px] text-slate-500 mt-10">
          &copy; {new Date().getFullYear()} Indian Payroll
        </p>
      </aside>

      {/* Form column */}
      <main className="flex-1 flex items-center justify-center px-4 py-10 md:py-12">
        <div className="w-full max-w-sm">
          <div className="mb-6">
            <h1 className="text-[22px] font-semibold text-[var(--color-text-primary)]">{title}</h1>
            {subtitle && (
              <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">{subtitle}</p>
            )}
          </div>
          {children}
        </div>
      </main>
    </div>
  )
}

function FeatureRow({
  icon, title, body,
}: { icon: ReactElement; title: string; body: string }): ReactElement {
  return (
    <li className="flex items-start gap-3">
      <div className="w-8 h-8 rounded-lg bg-white/10 text-white flex items-center justify-center flex-shrink-0 mt-0.5">
        {icon}
      </div>
      <div>
        <p className="text-[13px] font-semibold text-white">{title}</p>
        <p className="text-[12px] text-slate-300 mt-0.5">{body}</p>
      </div>
    </li>
  )
}
