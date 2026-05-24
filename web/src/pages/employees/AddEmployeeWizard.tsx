import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import WizardProgress from './wizard/WizardProgress'
import WizardStep1Basic from './wizard/WizardStep1Basic'
import WizardStep2Salary from './wizard/WizardStep2Salary'
import WizardStep3Personal from './wizard/WizardStep3Personal'
import WizardStep4Payment from './wizard/WizardStep4Payment'

const STEP_LABELS: Record<string, number> = {
  salary: 2,
  personal: 3,
  payment: 4,
}

export default function AddEmployeeWizard(): React.ReactElement {
  const navigate = useNavigate()
  const { id, step } = useParams<{ id?: string; step?: string }>()
  const [searchParams] = useSearchParams()
  const isRevise = searchParams.get('revise') === '1'

  const currentStep = step ? (STEP_LABELS[step] ?? 1) : 1

  function goToStep(employeeId: string, nextStep: number): void {
    const stepNames = ['', '', 'salary', 'personal', 'payment']
    if (nextStep > 4) {
      navigate(`/employees/${employeeId}`)
    } else {
      navigate(`/employees/${employeeId}/wizard/${stepNames[nextStep]}`)
    }
  }

  return (
    <div className="max-w-3xl">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-[18px] font-semibold text-[var(--color-text-primary)]">
          {isRevise ? 'Revise Salary' : 'Add Employee'}
        </h1>
        <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
          {isRevise
            ? 'Update CTC, template, or component values. Changes apply from the next payroll run.'
            : currentStep < 4
              ? 'Complete all steps or skip individual sections — you can fill details from the employee profile later.'
              : 'Almost done — add payment details or skip to finish.'}
        </p>
      </div>

      {!isRevise && <WizardProgress current={currentStep} />}

      <div className="bg-white border border-[var(--color-border)] rounded-xl p-6">
        {currentStep === 1 && (
          <WizardStep1Basic
            onSuccess={newId => goToStep(newId, 2)}
            onCancel={() => navigate('/employees')}
          />
        )}
        {currentStep === 2 && id && (
          <WizardStep2Salary
            employeeId={id}
            isRevise={isRevise}
            onSuccess={() => isRevise ? navigate(`/employees/${id}?tab=salary`) : goToStep(id, 3)}
            onSkip={() => goToStep(id, 3)}
          />
        )}
        {currentStep === 3 && id && (
          <WizardStep3Personal
            employeeId={id}
            onSuccess={() => goToStep(id, 4)}
            onSkip={() => goToStep(id, 4)}
          />
        )}
        {currentStep === 4 && id && (
          <WizardStep4Payment
            employeeId={id}
            onSuccess={() => navigate(`/employees/${id}`)}
            onSkip={() => navigate(`/employees/${id}`)}
          />
        )}
      </div>
    </div>
  )
}
