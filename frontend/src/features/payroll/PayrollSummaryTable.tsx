"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { EmptyState, Money, Skeleton, SortableHeader, type SortDirection } from "@/components/ui";
import type { PayrollPeriod, PayrollSummary } from "@/lib/types";

type SortColumn = "instructorName" | "classEarnings" | "commission" | "bonus" | "adjustment" | "finalPayout";

interface Props {
  rows: PayrollSummary[] | undefined;
  period: PayrollPeriod;
  loading: boolean;
  /** True when a run exists for this period (even if it has zero rows). */
  hasRun: boolean;
}

const columns: { key: SortColumn; label: string; align: "left" | "right" }[] = [
  { key: "instructorName", label: "Instructor", align: "left" },
  { key: "classEarnings", label: "Class earnings", align: "right" },
  { key: "commission", label: "Commission", align: "right" },
  { key: "bonus", label: "Bonus", align: "right" },
  { key: "adjustment", label: "Adjustment", align: "right" },
  { key: "finalPayout", label: "Final payout", align: "right" },
];

export function PayrollSummaryTable({ rows, period, loading, hasRun }: Props) {
  const [sortColumn, setSortColumn] = useState<SortColumn>("instructorName");
  const [sortDirection, setSortDirection] = useState<SortDirection>("asc");

  const onSort = (column: SortColumn) => {
    if (column === sortColumn) {
      setSortDirection((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortColumn(column);
      // Money columns default to largest-first; names A→Z.
      setSortDirection(column === "instructorName" ? "asc" : "desc");
    }
  };

  const sorted = useMemo(() => {
    if (!rows) return [];
    const factor = sortDirection === "asc" ? 1 : -1;
    return [...rows].sort((a, b) => {
      if (sortColumn === "instructorName") {
        return factor * a.instructorName.localeCompare(b.instructorName, undefined, { sensitivity: "base" });
      }
      const diff = a[sortColumn] - b[sortColumn];
      return diff !== 0 ? factor * diff : a.instructorName.localeCompare(b.instructorName);
    });
  }, [rows, sortColumn, sortDirection]);

  const totals = useMemo(
    () =>
      (rows ?? []).reduce(
        (acc, r) => ({
          classEarnings: acc.classEarnings + r.classEarnings,
          commission: acc.commission + r.commission,
          bonus: acc.bonus + r.bonus,
          adjustment: acc.adjustment + r.adjustment,
          finalPayout: acc.finalPayout + r.finalPayout,
        }),
        { classEarnings: 0, commission: 0, bonus: 0, adjustment: 0, finalPayout: 0 },
      ),
    [rows],
  );

  const currency = rows?.[0]?.currency ?? "SGD";
  const detailHref = (r: PayrollSummary) =>
    `/instructors/${r.instructorId}?startDate=${period.startDate}&endDate=${period.endDate}`;

  if (loading) {
    return <TableSkeleton />;
  }

  if (!rows || rows.length === 0) {
    return hasRun ? (
      <EmptyState title="No instructor activity in this period">
        Payroll was generated, but no classes, sales, refunds or adjustments fall between these dates, so there is nothing to pay.
      </EmptyState>
    ) : (
      <EmptyState title="No payroll generated for this period yet">
        Choose a start and end date, then click <strong>Generate payroll</strong>. Generating is safe to repeat: the same period
        returns the stored result.
      </EmptyState>
    );
  }

  return (
    <>
      {/* Desktop / tablet: table */}
      <div className="hidden overflow-x-auto md:block">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50">
            <tr>
              {columns.map((c) => (
                <SortableHeader
                  key={c.key}
                  label={c.label}
                  column={c.key}
                  align={c.align}
                  active={sortColumn === c.key}
                  direction={sortDirection}
                  onSort={onSort}
                />
              ))}
              <th scope="col" className="px-3 py-2">
                <span className="sr-only">Details</span>
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {sorted.map((r) => (
              <tr key={r.instructorId} className="group hover:bg-indigo-50/40">
                <td className="min-w-[11rem] px-2.5 py-2.5">
                  <Link href={detailHref(r)} className="font-medium text-slate-900 hover:text-indigo-700 hover:underline">
                    {r.instructorName}
                  </Link>
                  <p className="text-xs text-slate-500">
                    {r.studioName}
                    <span className="whitespace-nowrap">
                      {" "}
                      · {r.classesCompleted} {r.classesCompleted === 1 ? "class" : "classes"}
                    </span>
                    {r.unpaidNoShowBookings > 0 && (
                      <span className="whitespace-nowrap">
                        {" "}
                        · {r.unpaidNoShowBookings} unpaid {r.unpaidNoShowBookings === 1 ? "no-show" : "no-shows"}
                      </span>
                    )}
                  </p>
                </td>
                <td className="whitespace-nowrap px-2.5 py-2.5 text-right">
                  <Money value={r.classEarnings} currency={r.currency} />
                </td>
                <td className="whitespace-nowrap px-2.5 py-2.5 text-right">
                  <Money value={r.commission} currency={r.currency} />
                </td>
                <td className="whitespace-nowrap px-2.5 py-2.5 text-right">
                  <Money value={r.bonus} currency={r.currency} />
                </td>
                <td className="whitespace-nowrap px-2.5 py-2.5 text-right">
                  <Money value={r.adjustment} currency={r.currency} signed />
                </td>
                <td className="whitespace-nowrap px-2.5 py-2.5 text-right font-semibold text-slate-900">
                  <Money value={r.finalPayout} currency={r.currency} />
                </td>
                <td className="whitespace-nowrap px-2.5 py-2.5 text-right">
                  <Link
                    href={detailHref(r)}
                    className="text-xs font-medium text-indigo-600 opacity-70 group-hover:opacity-100 hover:underline"
                    aria-label={`View payroll details for ${r.instructorName}`}
                  >
                    Details →
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot className="border-t-2 border-slate-200 bg-slate-50 font-semibold text-slate-900">
            <tr>
              <td className="px-3 py-2.5">
                Total <span className="font-normal text-slate-500">({rows.length} instructor{rows.length === 1 ? "" : "s"})</span>
              </td>
              <td className="px-3 py-2.5 text-right">
                <Money value={totals.classEarnings} currency={currency} />
              </td>
              <td className="px-3 py-2.5 text-right">
                <Money value={totals.commission} currency={currency} />
              </td>
              <td className="px-3 py-2.5 text-right">
                <Money value={totals.bonus} currency={currency} />
              </td>
              <td className="px-3 py-2.5 text-right">
                <Money value={totals.adjustment} currency={currency} signed />
              </td>
              <td className="px-3 py-2.5 text-right">
                <Money value={totals.finalPayout} currency={currency} />
              </td>
              <td />
            </tr>
          </tfoot>
        </table>
      </div>

      {/* Mobile: cards */}
      <div className="md:hidden">
        <div className="flex items-center justify-between gap-2 border-b border-slate-200 px-4 py-2 text-xs">
          <span className="text-slate-500">Sort by</span>
          <div className="flex gap-1">
            <MobileSortButton active={sortColumn === "instructorName"} direction={sortDirection} onClick={() => onSort("instructorName")}>
              Name
            </MobileSortButton>
            <MobileSortButton active={sortColumn === "finalPayout"} direction={sortDirection} onClick={() => onSort("finalPayout")}>
              Final payout
            </MobileSortButton>
          </div>
        </div>
        <ul className="divide-y divide-slate-100">
          {sorted.map((r) => (
            <li key={r.instructorId}>
              <Link href={detailHref(r)} className="block px-4 py-3 hover:bg-indigo-50/40">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-medium text-slate-900">{r.instructorName}</p>
                    <p className="text-xs text-slate-500">{r.studioName}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-xs text-slate-500">Final payout</p>
                    <Money value={r.finalPayout} currency={r.currency} className="font-semibold text-slate-900" />
                  </div>
                </div>
                <dl className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-xs">
                  <Row label="Class earnings">
                    <Money value={r.classEarnings} currency={r.currency} />
                  </Row>
                  <Row label="Commission">
                    <Money value={r.commission} currency={r.currency} />
                  </Row>
                  <Row label="Bonus">
                    <Money value={r.bonus} currency={r.currency} />
                  </Row>
                  <Row label="Adjustment">
                    <Money value={r.adjustment} currency={r.currency} signed />
                  </Row>
                </dl>
              </Link>
            </li>
          ))}
        </ul>
        <div className="flex items-center justify-between border-t-2 border-slate-200 bg-slate-50 px-4 py-3 text-sm font-semibold text-slate-900">
          <span>Total</span>
          <Money value={totals.finalPayout} currency={currency} />
        </div>
      </div>
    </>
  );
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-2">
      <dt className="text-slate-500">{label}</dt>
      <dd className="text-slate-800">{children}</dd>
    </div>
  );
}

function MobileSortButton({
  active,
  direction,
  onClick,
  children,
}: {
  active: boolean;
  direction: SortDirection;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-md px-2 py-1 ${active ? "bg-slate-900 text-white" : "bg-slate-100 text-slate-700"}`}
    >
      {children} {active && (direction === "asc" ? "▲" : "▼")}
    </button>
  );
}

function TableSkeleton() {
  return (
    <div className="p-4" aria-busy="true" aria-label="Loading payroll">
      <div className="space-y-3">
        <Skeleton className="h-4 w-1/3" />
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="grid grid-cols-6 gap-3">
            <Skeleton className="col-span-2 h-4" />
            <Skeleton className="h-4" />
            <Skeleton className="h-4" />
            <Skeleton className="h-4" />
            <Skeleton className="h-4" />
          </div>
        ))}
      </div>
    </div>
  );
}
