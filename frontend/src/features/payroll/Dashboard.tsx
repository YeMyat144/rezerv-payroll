"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import { Alert, Button, Card, CardHeader } from "@/components/ui";
import { ApiError } from "@/lib/api";
import { formatMoney, formatPeriod, formatUtcDateTime } from "@/lib/format";
import { currentMonthPeriod, periodFromSearchParams, periodsEqual } from "@/lib/period";
import type { GeneratePayrollResponse, PayrollPeriod } from "@/lib/types";
import { useGeneratePayroll, usePayrollRuns, usePayrollSummary } from "./hooks";
import { PayrollSummaryTable } from "./PayrollSummaryTable";
import { PeriodFilter } from "./PeriodFilter";

export function Dashboard() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  // The selected period lives in the URL so a payroll view is shareable and survives refresh.
  const period = useMemo(() => periodFromSearchParams(searchParams, currentMonthPeriod()), [searchParams]);

  const setPeriod = useCallback(
    (p: PayrollPeriod) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("startDate", p.startDate);
      params.set("endDate", p.endDate);
      router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    },
    [pathname, router, searchParams],
  );

  const summary = usePayrollSummary(period);
  const runs = usePayrollRuns();
  const generate = useGeneratePayroll();

  const [lastResult, setLastResult] = useState<{ period: PayrollPeriod; result: GeneratePayrollResponse } | null>(null);
  const [confirmRegenerate, setConfirmRegenerate] = useState(false);

  const runForPeriod = runs.data?.find((r) => r.startDate === period.startDate && r.endDate === period.endDate);
  const hasRun = !!runForPeriod || (summary.data?.length ?? 0) > 0;

  const handleGenerate = (p: PayrollPeriod, regenerate = false) => {
    setConfirmRegenerate(false);
    generate.mutate(
      { ...p, regenerate },
      {
        onSuccess: (result) => setLastResult({ period: p, result }),
      },
    );
  };

  const generateError = generate.error instanceof ApiError ? generate.error : null;
  const overlapPeriods = generateError?.status === 409 ? (generateError.problem?.conflictingPeriods ?? []) : [];
  const showLastResult = lastResult && periodsEqual(lastResult.period, period) && !generate.isPending && !generate.error;

  return (
    <div className="flex flex-col gap-5">
      <Card>
        <CardHeader title="Payroll period" description="Pick an inclusive date range, then generate payroll for all instructors." />
        <div className="px-4 py-4 sm:px-5">
          <PeriodFilter
            period={period}
            onPeriodChange={(p) => {
              generate.reset();
              setPeriod(p);
            }}
            onGenerate={(p) => handleGenerate(p)}
            generating={generate.isPending}
            runs={runs.data}
          />
        </div>
      </Card>

      {generateError && (
        <Alert
          tone="error"
          title={generateError.problem?.title ?? "Could not generate payroll"}
          actions={
            overlapPeriods.length > 0 ? (
              overlapPeriods.map((p) => (
                <Button key={`${p.startDate}${p.endDate}`} variant="secondary" onClick={() => setPeriod(p)}>
                  Open {formatPeriod(p)}
                </Button>
              ))
            ) : (
              <Button variant="secondary" onClick={() => handleGenerate(period)}>
                Retry
              </Button>
            )
          }
        >
          {generateError.message}
        </Alert>
      )}

      {showLastResult && lastResult.result.alreadyExisted && (
        <Alert
          tone="warning"
          title="Payroll for this period was already generated"
          actions={
            confirmRegenerate ? (
              <>
                <Button variant="danger" onClick={() => handleGenerate(period, true)}>
                  Yes, regenerate
                </Button>
                <Button variant="ghost" onClick={() => setConfirmRegenerate(false)}>
                  Cancel
                </Button>
              </>
            ) : (
              <Button variant="secondary" onClick={() => setConfirmRegenerate(true)}>
                Regenerate
              </Button>
            )
          }
        >
          Showing the stored run from {formatUtcDateTime(lastResult.result.run.generatedAtUtc)}. Nothing was recalculated.
          {confirmRegenerate && (
            <span className="mt-1 block font-medium">
              Regenerating replaces this run with a fresh calculation from current bookings and sales. Only do this if source data was
              corrected.
            </span>
          )}
        </Alert>
      )}

      {showLastResult && !lastResult.result.alreadyExisted && (
        <Alert tone="success" title={lastResult.result.regenerated ? "Payroll regenerated" : "Payroll generated"}>
          {lastResult.result.run.instructorCount === 0
            ? "No instructor had activity in this period. An empty run was recorded."
            : `${lastResult.result.run.instructorCount} instructor${lastResult.result.run.instructorCount === 1 ? "" : "s"} · total payout ${formatMoney(
                lastResult.result.run.totalPayout,
                lastResult.result.summary[0]?.currency ?? "SGD",
              )}`}
        </Alert>
      )}

      {summary.isError && (
        <Alert
          tone="error"
          title="Could not load payroll summary"
          actions={
            <Button variant="secondary" onClick={() => summary.refetch()}>
              Retry
            </Button>
          }
        >
          {(summary.error as Error).message}
        </Alert>
      )}

      <Card>
        <CardHeader
          title="Payroll summary"
          description={
            runForPeriod
              ? `${formatPeriod(period)} · generated ${formatUtcDateTime(runForPeriod.generatedAtUtc)}`
              : formatPeriod(period)
          }
          actions={
            summary.isFetching && !summary.isLoading ? <span className="text-xs text-slate-500">Refreshing…</span> : undefined
          }
        />
        <PayrollSummaryTable rows={summary.data} period={period} loading={summary.isLoading} hasRun={hasRun} />
      </Card>
    </div>
  );
}
