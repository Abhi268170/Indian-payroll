import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import type { PayrunEmployeeDto } from '@/types/api'

vi.mock('../components/EmployeePayBreakdown', () => ({
  default: ({
    employeeId,
    readOnly,
  }: {
    employeeId: string
    readOnly: boolean
  }) => (
    <div data-testid={`breakdown-${employeeId}`} data-readonly={String(readOnly)}>
      breakdown
    </div>
  ),
}))

import EmployeeSummaryTable from '../components/EmployeeSummaryTable'

const defaultProps = {
  runStatus: 'Draft',
  runId: 'run-1',
  onSkipEmployee: vi.fn(),
  onDownloadPayslip: vi.fn(),
  onReEvaluate: vi.fn(),
  isReEvaluating: false,
  onShowImport: vi.fn(),
  onShowExport: vi.fn(),
}

function makeEmployee(overrides: Partial<PayrunEmployeeDto> = {}): PayrunEmployeeDto {
  return {
    employeeId: 'emp-1',
    employeeCode: 'EMP001',
    employeeName: 'Arjun Sharma',
    department: 'Engineering',
    designation: 'SDE',
    status: 'Active',
    lopDays: 0,
    baseDays: 26,
    grossPay: 50000,
    netPay: 47000,
    employeePf: 1800,
    employeeEsi: 0,
    ptAmount: 200,
    lwfEmployeeAmount: 10,
    tdsAmount: 1000,
    tdsOverrideAmount: null,
    skipReason: null,
    lastWorkingDay: null,
    exitReason: null,
    ...overrides,
  }
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('EmployeeSummaryTable', () => {
  describe('accordion expand/collapse', () => {
    it('clicking chevron on Draft Active row expands breakdown with readOnly=false', () => {
      const emp = makeEmployee()
      render(<EmployeeSummaryTable {...defaultProps} employees={[emp]} />)

      const chevrons = screen.getAllByRole('cell').filter(td =>
        td.querySelector('svg'),
      )
      fireEvent.click(chevrons[0]!)

      const bd = screen.getByTestId('breakdown-emp-1')
      expect(bd).toBeDefined()
      expect(bd.dataset.readonly).toBe('false')
    })

    it('clicking expanded row chevron again collapses it', () => {
      const emp = makeEmployee()
      render(<EmployeeSummaryTable {...defaultProps} employees={[emp]} />)

      const chevrons = screen.getAllByRole('cell').filter(td => td.querySelector('svg'))
      fireEvent.click(chevrons[0]!)
      expect(screen.getByTestId('breakdown-emp-1')).toBeDefined()

      fireEvent.click(chevrons[0]!)
      expect(screen.queryByTestId('breakdown-emp-1')).toBeNull()
    })

    it('single-expand: expanding row B collapses row A', () => {
      const empA = makeEmployee({ employeeId: 'emp-a', employeeName: 'Alice' })
      const empB = makeEmployee({ employeeId: 'emp-b', employeeName: 'Bob' })
      render(<EmployeeSummaryTable {...defaultProps} employees={[empA, empB]} />)

      const chevrons = screen.getAllByRole('cell').filter(td => td.querySelector('svg'))
      fireEvent.click(chevrons[0]!) // expand A
      expect(screen.getByTestId('breakdown-emp-a')).toBeDefined()

      fireEvent.click(chevrons[1]!) // expand B → A collapses
      expect(screen.queryByTestId('breakdown-emp-a')).toBeNull()
      expect(screen.getByTestId('breakdown-emp-b')).toBeDefined()
    })

    it('Approved status row expands with readOnly=true', () => {
      const emp = makeEmployee()
      render(
        <EmployeeSummaryTable {...defaultProps} runStatus="Approved" employees={[emp]} />,
      )

      const chevrons = screen.getAllByRole('cell').filter(td => td.querySelector('svg'))
      fireEvent.click(chevrons[0]!)

      const bd = screen.getByTestId('breakdown-emp-1')
      expect(bd.dataset.readonly).toBe('true')
    })

    it('Paid status row expands with readOnly=true', () => {
      const emp = makeEmployee()
      render(<EmployeeSummaryTable {...defaultProps} runStatus="Paid" employees={[emp]} />)

      const chevrons = screen.getAllByRole('cell').filter(td => td.querySelector('svg'))
      fireEvent.click(chevrons[0]!)

      expect(screen.getByTestId('breakdown-emp-1').dataset.readonly).toBe('true')
    })
  })

  describe('actions column', () => {
    it('Draft Active row renders Skip button, no Download button', () => {
      render(
        <EmployeeSummaryTable
          {...defaultProps}
          employees={[makeEmployee({ status: 'Active' })]}
        />,
      )
      expect(screen.getByText('Skip')).toBeDefined()
      expect(screen.queryByTitle('Download payslip')).toBeNull()
    })

    it('Draft Skipped row renders no Skip or Download button', () => {
      render(
        <EmployeeSummaryTable
          {...defaultProps}
          employees={[makeEmployee({ status: 'Skipped' })]}
        />,
      )
      expect(screen.queryByText('Skip')).toBeNull()
      expect(screen.queryByTitle('Download payslip')).toBeNull()
    })

    it('Approved row renders Download button, no Skip button', () => {
      render(
        <EmployeeSummaryTable
          {...defaultProps}
          runStatus="Approved"
          employees={[makeEmployee()]}
        />,
      )
      expect(screen.queryByText('Skip')).toBeNull()
      expect(screen.getByTitle('Download payslip')).toBeDefined()
    })

    it('no Eye icon in any row', () => {
      render(
        <EmployeeSummaryTable
          {...defaultProps}
          employees={[makeEmployee(), makeEmployee({ employeeId: 'emp-2', employeeName: 'Bob' })]}
        />,
      )
      // Eye icon from lucide would render as an SVG with specific path data
      // Check that no button with title "View details" exists
      expect(screen.queryByTitle('View details')).toBeNull()
    })
  })

  describe('filter tab hides expanded row', () => {
    it('when filtered employee is not in visible list, breakdown is not shown', () => {
      const active = makeEmployee({ employeeId: 'emp-active', status: 'Active' })
      const skipped = makeEmployee({
        employeeId: 'emp-skipped',
        employeeName: 'Skipped Employee',
        status: 'Skipped',
      })
      render(<EmployeeSummaryTable {...defaultProps} employees={[active, skipped]} />)

      // Expand the skipped employee row
      const chevrons = screen.getAllByRole('cell').filter(td => td.querySelector('svg'))
      fireEvent.click(chevrons[1]!) // skipped is second
      expect(screen.getByTestId('breakdown-emp-skipped')).toBeDefined()

      // Switch to Active filter — skipped employee not visible, breakdown must not show
      fireEvent.click(screen.getByText(/^Active/))
      expect(screen.queryByTestId('breakdown-emp-skipped')).toBeNull()
    })
  })

  describe('pagination: new employees prop collapses expanded row', () => {
    it('re-renders with new employees page collapses previously expanded row', () => {
      const page1Employee = makeEmployee({ employeeId: 'p1-emp', employeeName: 'Page 1 Emp' })
      const { rerender } = render(
        <EmployeeSummaryTable {...defaultProps} employees={[page1Employee]} />,
      )

      // Expand page 1 employee
      const chevrons = screen.getAllByRole('cell').filter(td => td.querySelector('svg'))
      fireEvent.click(chevrons[0]!)
      expect(screen.getByTestId('breakdown-p1-emp')).toBeDefined()

      // Simulate page change: new employees with different ID
      const page2Employee = makeEmployee({ employeeId: 'p2-emp', employeeName: 'Page 2 Emp' })
      rerender(<EmployeeSummaryTable {...defaultProps} employees={[page2Employee]} />)

      // Old breakdown gone (derived: p1-emp not in new visible list)
      expect(screen.queryByTestId('breakdown-p1-emp')).toBeNull()
    })
  })
})
