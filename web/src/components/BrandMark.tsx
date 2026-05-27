import { type ReactElement } from 'react'
import logoUrl from '@/assets/payroll-logo.webp'

type Size = 'sm' | 'md' | 'lg'

// Single source of truth for the Indian Payroll mark across every surface.
// Renders the rupee/hands logo from web/src/assets/payroll-logo.webp.
// `size` covers the realistic placements: sm = sidebar nav header, md = top
// chrome / settings overlay, lg = auth screens hero.
export default function BrandMark({
  size = 'sm',
  className = '',
}: { size?: Size; className?: string }): ReactElement {
  const dims = SIZE_MAP[size]
  return (
    <img
      src={logoUrl}
      alt="Indian Payroll"
      className={`${dims} object-contain ${className}`}
    />
  )
}

const SIZE_MAP: Record<Size, string> = {
  sm: 'h-7 w-7',
  md: 'h-8 w-8',
  lg: 'h-10 w-10',
}
