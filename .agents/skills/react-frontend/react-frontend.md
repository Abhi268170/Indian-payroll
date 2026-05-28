# Skill: React Frontend — React 18 + Vite + TypeScript + Zod + React Hook Form

## Stack

- React 18 + Vite 5 + TypeScript 5 (strict)
- React Hook Form + Zod (forms + validation)
- Axios (HTTP, auth interceptor)
- Vitest + React Testing Library (unit/component tests)
- Playwright (E2E)
- TanStack Query (server state management)
- Zustand (minimal client state — auth, tenant context)

---

## Project Structure

```
web/src/
  api/                     # Axios instances, API functions per domain
    client.ts              # configured Axios instance (auth interceptor)
    employees.ts           # employee API calls
    payroll.ts             # payroll API calls
  components/
    ui/                    # primitives: Button, Input, Badge, Table (shadcn/ui)
    payroll/               # domain components: PayrollRunCard, PayslipTable
    employees/             # domain components: EmployeeForm, EmployeeList
    layout/                # Sidebar, TopBar, PageLayout
  features/                # co-locate everything for a feature
    payroll-run/
      PayrollRunPage.tsx
      PayrollRunPage.test.tsx
      usePayrollRun.ts
      payrollRunSchema.ts  # Zod schema
  hooks/                   # shared hooks
  lib/
    utils.ts               # cn(), formatCurrency(), formatDate()
    constants.ts
  types/                   # shared TypeScript types (mirrors API DTOs)
  routes/                  # React Router route definitions
  store/                   # Zustand stores (auth, tenant)
```

---

## TypeScript Rules

```json
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

- No `any`. Use `unknown` + type guards.
- No `as X` casts without a type guard function.
- Explicit return types on all exported functions and components.
- Use `satisfies` for type-checked object literals.
- `interface` for object shapes. `type` for unions/intersections/computed.

---

## Zod Schema Pattern

Define schemas in `{feature}Schema.ts`. Derive TypeScript types from schemas — never duplicate type definitions:

```typescript
// features/employees/employeeSchema.ts
import { z } from 'zod'

const PAN_REGEX = /^[A-Z]{5}[0-9]{4}[A-Z]{1}$/
const AADHAAR_REGEX = /^\d{12}$/

export const createEmployeeSchema = z.object({
  firstName: z.string().min(1, 'Required').max(100),
  lastName: z.string().min(1, 'Required').max(100),
  pan: z.string().regex(PAN_REGEX, 'Invalid PAN format (e.g. ABCDE1234F)'),
  aadhaar: z
    .string()
    .regex(AADHAAR_REGEX, 'Aadhaar must be 12 digits')
    .optional()
    .or(z.literal('')),
  dateOfJoining: z.string().refine(
    (val) => !isNaN(Date.parse(val)),
    'Invalid date'
  ),
  departmentId: z.string().uuid('Required'),
  salaryStructureId: z.string().uuid('Required'),
})

// Type derived from schema — single source of truth
export type CreateEmployeeInput = z.infer<typeof createEmployeeSchema>
```

---

## React Hook Form Pattern

```typescript
// features/employees/CreateEmployeeForm.tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createEmployeeSchema, type CreateEmployeeInput } from './employeeSchema'
import { useCreateEmployee } from './useEmployees'

export function CreateEmployeeForm(): JSX.Element {
  const { mutate: createEmployee, isPending, error } = useCreateEmployee()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<CreateEmployeeInput>({
    resolver: zodResolver(createEmployeeSchema),
  })

  const onSubmit = (data: CreateEmployeeInput): void => {
    createEmployee(data, {
      onSuccess: () => reset(),
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div>
        <label htmlFor="pan">PAN *</label>
        <input
          id="pan"
          {...register('pan')}
          aria-describedby={errors.pan ? 'pan-error' : undefined}
          aria-invalid={!!errors.pan}
        />
        {errors.pan && (
          <span id="pan-error" role="alert">{errors.pan.message}</span>
        )}
      </div>
      {/* ... */}
      <button type="submit" disabled={isSubmitting || isPending}>
        {isPending ? 'Creating...' : 'Create Employee'}
      </button>
    </form>
  )
}
```

---

## TanStack Query Pattern

```typescript
// api/employees.ts
import { apiClient } from './client'
import type { Employee, CreateEmployeeInput } from '@/types'

export const employeesApi = {
  getById: (id: string) =>
    apiClient.get<Employee>(`/employees/${id}`).then(r => r.data),

  create: (data: CreateEmployeeInput) =>
    apiClient.post<string>('/employees', data).then(r => r.data),

  list: (params: EmployeeListParams) =>
    apiClient.get<PagedResult<EmployeeSummary>>('/employees', { params })
      .then(r => r.data),
}

// features/employees/useEmployees.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { employeesApi } from '@/api/employees'

export const employeeKeys = {
  all: ['employees'] as const,
  lists: () => [...employeeKeys.all, 'list'] as const,
  detail: (id: string) => [...employeeKeys.all, id] as const,
}

export function useEmployee(id: string) {
  return useQuery({
    queryKey: employeeKeys.detail(id),
    queryFn: () => employeesApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateEmployee() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: employeesApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.lists() })
    },
  })
}
```

---

## Axios Auth Interceptor

```typescript
// api/client.ts
import axios from 'axios'
import { useAuthStore } from '@/store/auth'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Inject JWT on every request
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Handle 401 — redirect to login, clear token
apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().clearAuth()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)
```

---

## Currency & Date Formatting

```typescript
// lib/utils.ts
// Payroll-specific: always INR, always Indian locale

export function formatCurrency(amount: number | string): string {
  const num = typeof amount === 'string' ? parseFloat(amount) : amount
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(num)
}
// ₹1,25,000  ← Indian numbering (lakh/crore grouping)

export function formatPayPeriod(start: string, end: string): string {
  const s = new Date(start)
  return s.toLocaleDateString('en-IN', { month: 'long', year: 'numeric' })
}
// "April 2026"
```

---

## Sensitive Data Display Rules

- **PAN:** display masked except for authorised roles. `XXXXX1234F` format.
- **Aadhaar:** always `XXXX-XXXX-1234` in UI. Full reveal = explicit action + confirmation modal.
- **Bank account:** masked. Never in table views.
- Never log PAN/Aadhaar to `console.*` in any circumstances.

```typescript
export function maskPAN(pan: string): string {
  return pan.replace(/^[A-Z]{5}/, 'XXXXX')
}

export function maskAadhaar(aadhaar: string): string {
  return `XXXX-XXXX-${aadhaar.slice(-4)}`
}
```

---

## Component Rules

- Functional components only. No class components.
- `React.memo` only when profiling identifies unnecessary re-renders — not preemptively.
- `useMemo` / `useCallback` only for stable references passed to memoised children or dependency arrays — not for cheap computations.
- Custom hooks for business logic — components contain only rendering + event wiring.
- Accessibility: every form input has an associated `<label>`. Error messages use `role="alert"`. Interactive elements are keyboard-navigable.

---

## Error Boundary Pattern

```typescript
// Wrap route-level components
export function PayrollRunPage(): JSX.Element {
  return (
    <ErrorBoundary fallback={<PayrollRunErrorState />}>
      <Suspense fallback={<PayrollRunSkeleton />}>
        <PayrollRunContent />
      </Suspense>
    </ErrorBoundary>
  )
}
```

---

## ESLint Config

```json
// .eslintrc.json (key rules)
{
  "extends": [
    "eslint:recommended",
    "plugin:@typescript-eslint/strict-type-checked",
    "plugin:react-hooks/recommended",
    "plugin:jsx-a11y/recommended"
  ],
  "rules": {
    "@typescript-eslint/no-explicit-any": "error",
    "@typescript-eslint/explicit-function-return-type": "warn",
    "no-console": ["error", { "allow": ["error", "warn"] }],
    "react-hooks/exhaustive-deps": "error"
  }
}
```
