"use client";

import Link from "next/link";
import { useParams, useSearchParams } from "next/navigation";
import { useMemo, useState } from "react";
import { Alert, Badge, Button, Card, CardHeader, EmptyState, Money, Skeleton } from "@/components/ui";
import { ApiError } from "@/lib/api";
import { formatDate, formatDateTime, formatPercent, formatPeriod, formatUtcDateTime } from "@/lib/format";
import { currentMonthPeriod, periodFromSearchParams } from "@/lib/period";
import type { InstructorPayrollDetail as Detail } from "@/lib/types";
import { useInstructorDetail } from "./hooks";

export function InstructorDetail() {
  const params = useParams<{ instructorId: string }>();
  const searchParams = useSearchParams();
  const period = useMemo(() => periodFromSearchParams(searchParams, currentMonthPeriod()), [searchParams]);
  const backHref = `/?startDate=${period.startDate}&endDate=${period.endDate}`;

  const detail = useInstructorDetail(params.instructorId, period);

  if (detail.isLoading) {
    return (
      <div className="flex flex-col gap-5" aria-busy="true">
        <BackLink href={backHref} />
        <Skeleton className="h-8 w-64" />
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-20" />
          ))}
        </div>
        <Skeleton className="h-48" />
      </div>
    );
  }

  if (detail.isError) {
    const err = detail.error as ApiError | Error;
    const notFound = err instanceof ApiError && err.status === 404;
    return (
      <div className="flex flex-col gap-5">
        <BackLink href={backHref} />
        <Alert
          tone={notFound ? "warning" : "error"}
          title={notFound ? "No payroll for this instructor in this period" : "Could not load payroll detail"}
          actions={
            notFound ? (
              <Link href={backHref}>
                <Button variant="secondary">Back to summary</Button>
              </Link>
            ) : (
              <Button variant="secondary" onClick={() => detail.refetch()}>
                Retry
              </Button>
            )
          }
        >
          {err.message}
        </Alert>
      </div>
    );
  }

  const d = detail.data!;
  return (
    <div className="flex flex-col gap-5">
      <BackLink href={backHref} />

      <header className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{d.instructorName}</h1>
          <p className="text-sm text-slate-500">
            {d.studioName} · {formatPeriod(d.period)} · generated {formatUtcDateTime(d.generatedAtUtc)}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Badge tone={d.noShowPayoutPolicy === "PayInstructor" ? "info" : "neutral"}>
            No-show policy: {d.noShowPayoutPolicy === "PayInstructor" ? "Pay instructor" : "No payout"}
          </Badge>
          <Badge>
            {d.classesCompleted} completed · {d.classesCancelled} cancelled
          </Badge>
        </div>
      </header>

      <Totals d={d} />

      <ClassesSection d={d} />
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
        <CommissionsSection d={d} />
        <RefundsSection d={d} />
        <BonusesSection d={d} />
        <AdjustmentsSection d={d} />
      </div>
      <LedgerSection d={d} />
    </div>
  );
}

// ---------------------------------------------------------------------------------------------

function BackLink({ href }: { href: string }) {
  return (
    <Link href={href} className="inline-flex w-fit items-center gap-1 text-sm text-indigo-600 hover:underline">
      ← Back to payroll summary
    </Link>
  );
}

function Totals({ d }: { d: Detail }) {
  const t = d.totals;
  const items = [
    { label: "Fixed class fees", value: t.fixedClassEarnings },
    { label: "Booking earnings", value: t.bookingEarnings, hint: `${d.payableBookings} payable · ${d.unpaidNoShowBookings} unpaid no-show` },
    { label: "Commission", value: t.commission },
    { label: "Attendance bonus", value: t.bonus },
    { label: "Adjustments", value: t.adjustment, signed: true },
  ];
  return (
    <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
      {items.map((i) => (
        <Card key={i.label} className="px-4 py-3">
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500">{i.label}</p>
          <Money value={i.value} currency={d.currency} signed={i.signed} className="mt-1 block text-lg font-semibold text-slate-900" />
          {i.hint && <p className="mt-0.5 text-xs text-slate-500">{i.hint}</p>}
        </Card>
      ))}
      <section className="rounded-xl border border-slate-900 bg-slate-900 px-4 py-3 text-white shadow-sm">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-300">Final payout</p>
        <Money value={t.finalPayout} currency={d.currency} className="mt-1 block text-lg font-semibold" />
        <p className="mt-0.5 text-xs text-slate-300">fixed + booking + commission + bonus + adjustments</p>
      </section>
    </div>
  );
}

function ClassesSection({ d }: { d: Detail }) {
  return (
    <Card>
      <CardHeader
        title="Classes"
        description="Cancelled classes are listed with zero payout so the full schedule is auditable. Attendance for the bonus counts attended bookings only."
      />
      {d.classes.length === 0 ? (
        <EmptyState title="No classes in this period" />
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <Th>Class</Th>
                <Th>Status</Th>
                <Th align="right">Attended</Th>
                <Th align="right">No-show</Th>
                <Th align="right">Cancelled</Th>
                <Th align="right">Payable bookings</Th>
                <Th align="right">Fixed fee</Th>
                <Th align="right">Booking payout</Th>
                <Th align="right">Bonus</Th>
                <Th align="right">Total</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {d.classes.map((c) => {
                const cancelled = c.status === "Cancelled";
                return (
                  <tr key={c.classId} className={cancelled ? "text-slate-400" : ""}>
                    <td className="whitespace-nowrap px-3 py-2">
                      <p className={`font-medium ${cancelled ? "" : "text-slate-900"}`}>{c.className}</p>
                      <p className="text-xs">{formatDateTime(c.startsAt)}</p>
                    </td>
                    <td className="px-3 py-2">
                      <Badge tone={cancelled ? "danger" : "success"}>{c.status}</Badge>
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">{c.attendedBookings}</td>
                    <td className="px-3 py-2 text-right tabular-nums">
                      {c.noShowBookings}
                      {c.unpaidNoShowBookings > 0 && <span className="ml-1 text-xs text-amber-700">({c.unpaidNoShowBookings} unpaid)</span>}
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">{c.cancelledBookings}</td>
                    <td className="px-3 py-2 text-right tabular-nums">
                      {c.payableBookings}
                      {c.perBookingPayout != null && !cancelled && (
                        <span className="ml-1 text-xs text-slate-500">× {c.perBookingPayout}</span>
                      )}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <Money value={c.fixedFeeEarned} currency={d.currency} />
                    </td>
                    <td className="px-3 py-2 text-right">
                      <Money value={c.bookingEarnings} currency={d.currency} />
                    </td>
                    <td className="px-3 py-2 text-right">
                      <Money value={c.bonusEarned} currency={d.currency} />
                    </td>
                    <td className="px-3 py-2 text-right font-semibold">
                      <Money value={c.total} currency={d.currency} />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}

function CommissionsSection({ d }: { d: Detail }) {
  return (
    <Card>
      <CardHeader title="Commission records" description="Memberships 10%, packages 5% (studio-configurable)." />
      {d.commissions.length === 0 ? (
        <EmptyState title="No sales in this period" />
      ) : (
        <ul className="divide-y divide-slate-100 text-sm">
          {d.commissions.map((c) => (
            <li key={c.saleId} className="flex items-start justify-between gap-3 px-4 py-2.5">
              <div className="min-w-0">
                <p className="truncate font-medium text-slate-900">{c.description}</p>
                <p className="text-xs text-slate-500">
                  Sold {formatDate(c.soldOn)} · sale <Money value={c.saleAmount} currency={d.currency} /> · {formatPercent(c.rate)}
                </p>
              </div>
              <Money value={c.commission} currency={d.currency} className="shrink-0 font-semibold text-slate-900" />
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function RefundsSection({ d }: { d: Detail }) {
  return (
    <Card>
      <CardHeader
        title="Refund adjustments"
        description="A refund reverses the commission in the period the refund was issued — even if the sale was paid out in an earlier payroll."
      />
      {d.refundAdjustments.length === 0 ? (
        <EmptyState title="No refunds in this period" />
      ) : (
        <ul className="divide-y divide-slate-100 text-sm">
          {d.refundAdjustments.map((r) => (
            <li key={r.saleId} className="flex items-start justify-between gap-3 px-4 py-2.5">
              <div className="min-w-0">
                <p className="truncate font-medium text-slate-900">{r.description}</p>
                <p className="text-xs text-slate-500">
                  Refunded {formatDate(r.refundedOn)} · sale <Money value={r.saleAmount} currency={d.currency} /> · {formatPercent(r.rate)}
                </p>
              </div>
              <Money value={r.amount} currency={d.currency} signed className="shrink-0 font-semibold" />
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function BonusesSection({ d }: { d: Detail }) {
  return (
    <Card>
      <CardHeader title="Attendance bonus records" description="Paid when attended bookings exceed the studio threshold." />
      {d.bonuses.length === 0 ? (
        <EmptyState title="No class crossed the attendance threshold" />
      ) : (
        <ul className="divide-y divide-slate-100 text-sm">
          {d.bonuses.map((b) => (
            <li key={b.classId} className="flex items-start justify-between gap-3 px-4 py-2.5">
              <div className="min-w-0">
                <p className="truncate font-medium text-slate-900">{b.description}</p>
                <p className="text-xs text-slate-500">
                  {formatDate(b.classDate)} · {b.attendance} attended
                </p>
              </div>
              <Money value={b.amount} currency={d.currency} className="shrink-0 font-semibold text-slate-900" />
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function AdjustmentsSection({ d }: { d: Detail }) {
  return (
    <Card>
      <CardHeader title="Payout adjustments" description="Everything that makes up the Adjustments total: refund reversals and manual corrections." />
      {d.payoutAdjustments.length === 0 ? (
        <EmptyState title="No adjustments in this period" />
      ) : (
        <ul className="divide-y divide-slate-100 text-sm">
          {d.payoutAdjustments.map((a) => (
            <li key={`${a.type}-${a.referenceId}`} className="flex items-start justify-between gap-3 px-4 py-2.5">
              <div className="min-w-0">
                <p className="truncate font-medium text-slate-900">{a.description}</p>
                <p className="text-xs text-slate-500">
                  <Badge tone={a.type === "RefundAdjustment" ? "warning" : "neutral"}>
                    {a.type === "RefundAdjustment" ? "Refund" : "Manual"}
                  </Badge>{" "}
                  {formatDate(a.effectiveDate)}
                </p>
              </div>
              <Money value={a.amount} currency={d.currency} signed className="shrink-0 font-semibold" />
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function LedgerSection({ d }: { d: Detail }) {
  const [open, setOpen] = useState(false);
  const sum = d.lineItems.reduce((acc, i) => acc + i.amount, 0);
  const reconciles = Math.abs(sum - d.totals.finalPayout) < 0.005;
  return (
    <Card>
      <CardHeader
        title="Audit ledger"
        description={`${d.lineItems.length} line items · sum ${reconciles ? "matches" : "DOES NOT match"} final payout`}
        actions={
          <Button variant="ghost" onClick={() => setOpen((o) => !o)} aria-expanded={open}>
            {open ? "Hide" : "Show"} ledger
          </Button>
        }
      />
      {open && (
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <Th>Date</Th>
                <Th>Type</Th>
                <Th>Description</Th>
                <Th align="right">Qty</Th>
                <Th align="right">Unit</Th>
                <Th align="right">Rate</Th>
                <Th align="right">Amount</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {d.lineItems.map((i) => (
                <tr key={i.id} className={i.amount === 0 ? "text-slate-400" : ""}>
                  <td className="whitespace-nowrap px-3 py-1.5">{formatDate(i.occurredOn)}</td>
                  <td className="whitespace-nowrap px-3 py-1.5 font-mono text-xs">{i.type}</td>
                  <td className="px-3 py-1.5">{i.description}</td>
                  <td className="px-3 py-1.5 text-right tabular-nums">{i.quantity ?? ""}</td>
                  <td className="px-3 py-1.5 text-right">{i.unitAmount != null ? <Money value={i.unitAmount} currency={d.currency} /> : ""}</td>
                  <td className="px-3 py-1.5 text-right">{i.rate != null ? formatPercent(i.rate) : ""}</td>
                  <td className="px-3 py-1.5 text-right">
                    <Money value={i.amount} currency={d.currency} signed={i.amount < 0} />
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="border-t-2 border-slate-200 bg-slate-50 font-semibold text-slate-900">
              <tr>
                <td colSpan={6} className="px-3 py-2">
                  Sum of line items
                </td>
                <td className="px-3 py-2 text-right">
                  <Money value={sum} currency={d.currency} />
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </Card>
  );
}

function Th({ children, align = "left" }: { children: React.ReactNode; align?: "left" | "right" }) {
  return (
    <th scope="col" className={`px-3 py-2 font-semibold ${align === "right" ? "text-right" : "text-left"}`}>
      {children}
    </th>
  );
}
