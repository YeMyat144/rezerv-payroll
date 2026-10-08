// DTOs mirroring the backend contracts (Rezerv.Payroll.Application/Payroll/Dtos.cs).
// Dates are ISO yyyy-MM-dd strings; money is a plain number in the entry's currency.

export type IsoDate = string;

export interface PayrollPeriod {
  startDate: IsoDate;
  endDate: IsoDate;
}

export interface PayrollRun {
  id: string;
  startDate: IsoDate;
  endDate: IsoDate;
  generatedAtUtc: string;
  status: string;
  instructorCount: number;
  totalPayout: number;
}

export interface PayrollSummary {
  instructorId: string;
  instructorName: string;
  studioName: string;
  currency: string;
  fixedClassEarnings: number;
  bookingEarnings: number;
  classEarnings: number;
  commission: number;
  bonus: number;
  adjustment: number;
  finalPayout: number;
  classesCompleted: number;
  payableBookings: number;
  unpaidNoShowBookings: number;
}

export interface GeneratePayrollRequest extends PayrollPeriod {
  regenerate?: boolean;
}

export interface GeneratePayrollResponse {
  run: PayrollRun;
  alreadyExisted: boolean;
  regenerated: boolean;
  summary: PayrollSummary[];
}

export interface PayrollTotals {
  fixedClassEarnings: number;
  bookingEarnings: number;
  classEarnings: number;
  commission: number;
  bonus: number;
  adjustment: number;
  finalPayout: number;
}

export interface ClassBreakdown {
  classId: string;
  className: string;
  startsAt: string;
  status: "Completed" | "Cancelled";
  fixedFee: number;
  perBookingPayout: number | null;
  attendedBookings: number;
  noShowBookings: number;
  cancelledBookings: number;
  payableBookings: number;
  unpaidNoShowBookings: number;
  fixedFeeEarned: number;
  bookingEarnings: number;
  bonusEarned: number;
  total: number;
}

export interface CommissionRecord {
  saleId: string;
  description: string;
  soldOn: IsoDate;
  saleAmount: number;
  rate: number;
  commission: number;
}

export interface RefundAdjustment {
  saleId: string;
  description: string;
  refundedOn: IsoDate;
  saleAmount: number;
  rate: number;
  amount: number;
}

export interface BonusRecord {
  classId: string;
  description: string;
  classDate: IsoDate;
  attendance: number;
  amount: number;
}

export interface PayoutAdjustment {
  referenceId: string;
  type: "RefundAdjustment" | "ManualAdjustment";
  description: string;
  effectiveDate: IsoDate;
  amount: number;
}

export interface LineItem {
  id: string;
  type: string;
  description: string;
  occurredOn: IsoDate;
  referenceType: string;
  referenceId: string;
  quantity: number | null;
  unitAmount: number | null;
  rate: number | null;
  amount: number;
}

export interface InstructorPayrollDetail {
  instructorId: string;
  instructorName: string;
  studioName: string;
  currency: string;
  noShowPayoutPolicy: "PayInstructor" | "NoPayout";
  period: PayrollPeriod;
  generatedAtUtc: string;
  totals: PayrollTotals;
  classesCompleted: number;
  classesCancelled: number;
  payableBookings: number;
  unpaidNoShowBookings: number;
  classes: ClassBreakdown[];
  commissions: CommissionRecord[];
  refundAdjustments: RefundAdjustment[];
  bonuses: BonusRecord[];
  payoutAdjustments: PayoutAdjustment[];
  lineItems: LineItem[];
}

/** RFC 7807 problem details as emitted by the API's exception handler. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  conflictingPeriods?: PayrollPeriod[];
  errors?: Record<string, string[]>;
}
