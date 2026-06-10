import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import type { EmployeeVariableInputsDto } from '@/types/api'

// --- mocks ---

const mockInvalidateQueries = vi.fn()
const mockMutate = vi.fn()

let mockQueryData: EmployeeVariableInputsDto | undefined
let mockIsLoading = false

vi.mock('@tanstack/react-query', () => ({
  useQuery: () => ({ data: mockQueryData, isLoading: mockIsLoading }),
  useMutation: ({ onSuccess }: { onSuccess?: () => void }) => ({
    mutate: (payload: unknown) => {
      mockMutate(payload)
      onSuccess?.()
    },
    isPending: false,
    isError: false,
  }),
  useQueryClient: () => ({ invalidateQueries: mockInvalidateQueries }),
}))

vi.mock('@/lib/api', () => ({
  api: {
    get: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

vi.mock('../components/AddOneTimeEntryModal', () => ({
  default: ({ category, onClose }: { category: string; onClose: () => void }) => (
    <div data-testid="add-modal" data-category={category}>
      <button onClick={onClose}>close-modal</button>
    </div>
  ),
}))

import EmployeePayBreakdown from '../components/EmployeePayBreakdown'

// --- fixtures ---

function makeData(overrides: Partial<EmployeeVariableInputsDto> = {}): EmployeeVariableInputsDto {
  return {
    payrollRunId: 'run-1',
    employeeId: 'emp-1',
    lopDays: 0,
    baseDays: 26,
    actualPayableDays: 26,
    grossPay: 50000,
    netPay: 47090,
    tdsAmount: 1000,
    tdsOverrideAmount: null,
    tdsOverrideReason: null,
    employeePf: 1800,
    vpfAmount: 0,
    employerPf: 1800,
    employeeEsi: 0,
    employerEsi: 0,
    ptAmount: 200,
    lwfEmployeeAmount: 10,
    lwfEmployerAmount: 10,
    gratuityAmount: 0,
    epsAmount: 0,
    monthlyCTC: 55000,
    components: [],
    ...overrides,
  }
}

const defaultProps = {
  runId: 'run-1',
  employeeId: 'emp-1',
  employeeName: 'Arjun Sharma',
  readOnly: false,
}

beforeEach(() => {
  vi.clearAllMocks()
  mockQueryData = makeData()
  mockIsLoading = false
})

// --- tests ---

describe('EmployeePayBreakdown', () => {
  describe('summary strip', () => {
    it('renders Gross / Deductions / Net labels in summary strip', () => {
      render(<EmployeePayBreakdown {...defaultProps} />)
      // Summary strip has 3-column grid — all three labels appear at least once
      expect(screen.getAllByText('Gross Pay').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Deductions').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Net Pay').length).toBeGreaterThan(0)
    })

    it('shows loading spinner when isLoading', () => {
      mockIsLoading = true
      mockQueryData = undefined
      const { container } = render(<EmployeePayBreakdown {...defaultProps} />)
      expect(container.querySelector('.animate-spin')).toBeTruthy()
    })

    it('computes deductions as explicit sum not grossPay-netPay', () => {
      // grossPay=50000, netPay=47090 → grossPay-netPay = 2910
      // explicit: pf(1800)+esi(0)+pt(200)+lwf(10)+tds(900)+oneTime(300) = 3210
      mockQueryData = makeData({
        grossPay: 50000,
        netPay: 46490,
        employeePf: 1800,
        employeeEsi: 0,
        ptAmount: 200,
        lwfEmployeeAmount: 10,
        tdsAmount: 900,
        tdsOverrideAmount: null,
        components: [
          {
            id: 'ded-1',
            salaryComponentId: 'comp-ded',
            componentCode: 'MISC_DED',
            componentName: 'Misc Deduction',
            fullAmount: 300,
            proratedAmount: 300,
            isOneTimeEarning: true,
            isDeduction: true,
            isBenefit: false,
          },
        ],
      })
      render(<EmployeePayBreakdown {...defaultProps} />)
      // explicit sum = 1800+0+200+10+900+300 = 3210 → formatINR → ₹3,210.00
      // grossPay-netPay = 50000-46490 = 3510 (different; the Deductions cell must NOT show 3510)
      expect(screen.getAllByText('₹3,210.00').length).toBeGreaterThan(0)
      expect(screen.queryByText('₹3,510.00')).toBeNull()
    })
  })

  describe('readOnly=true', () => {
    it('renders LOP as div not input', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={true} />)
      expect(screen.queryByRole('spinbutton', { name: /lop/i })).toBeNull()
    })

    it('does not render Add Earning button', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={true} />)
      expect(screen.queryByText('Add Earning')).toBeNull()
    })

    it('does not render Add Deduction button', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={true} />)
      expect(screen.queryByText('Add Deduction')).toBeNull()
    })

    it('does not render Override TDS button', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={true} />)
      expect(screen.queryByText('Override TDS')).toBeNull()
    })

    it('does not render trash icons for one-time earnings', () => {
      mockQueryData = makeData({
        components: [
          {
            id: 'ot-1',
            salaryComponentId: 'comp-ot',
            componentCode: 'BONUS',
            componentName: 'Bonus',
            fullAmount: 5000,
            proratedAmount: 5000,
            isOneTimeEarning: true,
            isDeduction: false,
            isBenefit: false,
          },
        ],
      })
      const { container } = render(<EmployeePayBreakdown {...defaultProps} readOnly={true} />)
      // Trash2 icon rendered as svg — check no trash buttons exist
      expect(container.querySelectorAll('button[title]').length).toBe(0)
      // Check no button inside the one-time row
      const rows = container.querySelectorAll('tr')
      const bonusRow = Array.from(rows).find(r => r.textContent?.includes('Bonus'))
      expect(bonusRow?.querySelector('button')).toBeNull()
    })
  })

  describe('readOnly=false', () => {
    it('renders LOP as number input', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={false} />)
      const inputs = screen.getAllByRole('spinbutton')
      expect(inputs.length).toBeGreaterThan(0)
    })

    it('renders Add Earning button', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={false} />)
      expect(screen.getByText('Add Earning')).toBeDefined()
    })

    it('renders Add Deduction button', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={false} />)
      expect(screen.getByText('Add Deduction')).toBeDefined()
    })

    it('renders Override TDS button', () => {
      render(<EmployeePayBreakdown {...defaultProps} readOnly={false} />)
      expect(screen.getByText('Override TDS')).toBeDefined()
    })

    it('renders trash icons for one-time earnings', () => {
      mockQueryData = makeData({
        components: [
          {
            id: 'ot-1',
            salaryComponentId: 'comp-ot',
            componentCode: 'BONUS',
            componentName: 'Bonus',
            fullAmount: 5000,
            proratedAmount: 5000,
            isOneTimeEarning: true,
            isDeduction: false,
            isBenefit: false,
          },
        ],
      })
      const { container } = render(<EmployeePayBreakdown {...defaultProps} readOnly={false} />)
      const rows = container.querySelectorAll('tr')
      const bonusRow = Array.from(rows).find(r => r.textContent?.includes('Bonus'))
      expect(bonusRow?.querySelector('button')).toBeTruthy()
    })
  })

  describe('LOP dirty flag', () => {
    it('preserves lopDaysEdit when isDirtyLopRef is true during data refresh', async () => {
      const { rerender } = render(<EmployeePayBreakdown {...defaultProps} />)
      const lopInput = screen.getAllByRole('spinbutton')[0] as HTMLInputElement

      // User types a new value → sets isDirtyLopRef.current = true
      fireEvent.change(lopInput, { target: { value: '3' } })
      expect(lopInput.value).toBe('3')

      // Simulate data refetch with different lopDays
      act(() => {
        mockQueryData = makeData({ lopDays: 1 })
      })
      rerender(<EmployeePayBreakdown {...defaultProps} />)

      // lopDaysEdit should NOT reset to server value — user typed 3
      expect(lopInput.value).toBe('3')
    })

    it('syncs lopDaysEdit to server value when isDirtyLopRef is false', async () => {
      const { rerender } = render(<EmployeePayBreakdown {...defaultProps} />)
      const lopInput = screen.getAllByRole('spinbutton')[0] as HTMLInputElement

      // No user interaction → isDirtyLopRef.current stays false
      // Data refetch with updated lopDays=2
      act(() => {
        mockQueryData = makeData({ lopDays: 2 })
      })
      rerender(<EmployeePayBreakdown {...defaultProps} />)

      await waitFor(() => {
        expect(lopInput.value).toBe('2')
      })
    })

    it('resets dirty flag after lopMutation success', async () => {
      const { rerender } = render(<EmployeePayBreakdown {...defaultProps} />)
      const lopInput = screen.getAllByRole('spinbutton')[0] as HTMLInputElement

      // User types → dirty
      fireEvent.change(lopInput, { target: { value: '3' } })
      // Blur → triggers mutation → onSuccess resets isDirtyLopRef
      fireEvent.blur(lopInput)

      // After mutation success (mocked to call onSuccess immediately),
      // simulate a data refetch with lopDays=3
      act(() => {
        mockQueryData = makeData({ lopDays: 3 })
      })
      rerender(<EmployeePayBreakdown {...defaultProps} />)

      // Now dirty is false, so server value syncs
      await waitFor(() => {
        expect((lopInput as HTMLInputElement).value).toBe('3')
      })
    })
  })

  describe('TDS override', () => {
    it('shows TDS form when Override TDS clicked', () => {
      render(<EmployeePayBreakdown {...defaultProps} />)
      fireEvent.click(screen.getByText('Override TDS'))
      expect(screen.getByText('Override TDS Amount')).toBeDefined()
      expect(screen.getByText('Reason')).toBeDefined()
    })

    it('closes TDS form on cancel', () => {
      render(<EmployeePayBreakdown {...defaultProps} />)
      fireEvent.click(screen.getByText('Override TDS'))
      fireEvent.click(screen.getByText('Cancel'))
      expect(screen.queryByText('Override TDS Amount')).toBeNull()
    })

    it('calls tdsMutation on Save with amount and reason', () => {
      render(<EmployeePayBreakdown {...defaultProps} />)
      fireEvent.click(screen.getByText('Override TDS'))

      // TDS amount input — target first empty number input in the override form
      const amountInputs = screen.getAllByDisplayValue('')
      const amountInput = amountInputs[0] as HTMLInputElement
      fireEvent.change(amountInput, { target: { value: '1200' } })

      const textarea = screen.getByRole('textbox')
      fireEvent.change(textarea, { target: { value: 'Employee requested lower TDS' } })

      fireEvent.click(screen.getByText('Save'))
      expect(mockMutate).toHaveBeenCalledWith(
        expect.objectContaining({ amount: 1200, reason: 'Employee requested lower TDS' }),
      )
    })
  })

  describe('AddOneTimeEntryModal', () => {
    it('mounts modal with category Earning when Add Earning clicked', () => {
      render(<EmployeePayBreakdown {...defaultProps} />)
      fireEvent.click(screen.getByText('Add Earning'))
      expect(screen.getByTestId('add-modal')).toBeDefined()
      expect(screen.getByTestId('add-modal').dataset.category).toBe('Earning')
    })

    it('mounts modal with category Deduction when Add Deduction clicked', () => {
      render(<EmployeePayBreakdown {...defaultProps} />)
      fireEvent.click(screen.getByText('Add Deduction'))
      expect(screen.getByTestId('add-modal').dataset.category).toBe('Deduction')
    })

    it('unmounts modal when onClose called', () => {
      render(<EmployeePayBreakdown {...defaultProps} />)
      fireEvent.click(screen.getByText('Add Earning'))
      fireEvent.click(screen.getByText('close-modal'))
      expect(screen.queryByTestId('add-modal')).toBeNull()
    })
  })
})
