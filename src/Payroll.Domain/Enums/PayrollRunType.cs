namespace Payroll.Domain.Enums;

public enum PayrollRunType
{
    Regular,
    OffCycle,
    OneTimePayout,
    Resettlement,
    // Single-employee Final and Final Settlement: created when the operator picks
    // "Pay on a given date" while initiating exit. PayDay = chosen settlement date.
    FinalSettlement,
    // Multi-employee Final and Final Settlement: shared run created when the operator
    // picks "Pay as per the regular pay schedule". PayDay = first regular pay date
    // on/after the LWD. New exits that resolve to the same target date append a
    // PayrunEmployee to the same Draft run rather than spawning another.
    BulkFinalSettlement,
}
