"use client";

import { useState, type FormEvent } from "react";
import { Button, Spinner } from "@/components/ui";
import { formatDate } from "@/lib/format";
import { currentMonthPeriod, periodsEqual, shiftPeriod, validatePeriod } from "@/lib/period";
import type { PayrollPeriod, PayrollRun } from "@/lib/types";

interface Props {
  period: PayrollPeriod;
  onPeriodChange: (p: PayrollPeriod) => void;
  onGenerate: (p: PayrollPeriod) => void;
  generating: boolean;
  runs?: PayrollRun[];
}

export function PeriodFilter({ period, onPeriodChange, onGenerate, generating, runs }: Props) {
  // Local draft so typing an invalid intermediate date doesn't fire queries on every keystroke.
  const [draft, setDraft] = useState<PayrollPeriod>(period);
  const [touched, setTouched] = useState(false);

  // Reset the draft when the committed period changes from outside (URL, quick-select, overlap "Open" button).
  // "Adjust state during render" pattern — avoids an effect + extra render cycle.
  const [syncedPeriod, setSyncedPeriod] = useState(period);
  if (!periodsEqual(syncedPeriod, period)) {
    setSyncedPeriod(period);
    setDraft(period);
    setTouched(false);
  }

  const error = validatePeriod(draft);
  const isDirty = !periodsEqual(draft, period);

  const apply = () => {
    if (error) {
      setTouched(true);
      return false;
    }
    if (isDirty) onPeriodChange(draft);
    return true;
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (apply()) onGenerate(draft);
  };

  const quick = (p: PayrollPeriod) => {
    setDraft(p);
    onPeriodChange(p);
  };

  const recentRuns = (runs ?? []).slice(0, 6);

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[auto_1fr_1fr_auto] sm:items-end">
        <div className="flex items-end gap-1">
          <Button variant="secondary" aria-label="Previous period" onClick={() => quick(shiftPeriod(draft, -1))} className="px-2.5">
            ‹
          </Button>
          <Button variant="secondary" aria-label="Next period" onClick={() => quick(shiftPeriod(draft, 1))} className="px-2.5">
            ›
          </Button>
        </div>

        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-slate-700">Start date</span>
          <input
            type="date"
            required
            value={draft.startDate}
            max={draft.endDate || undefined}
            onChange={(e) => setDraft({ ...draft, startDate: e.target.value })}
            onBlur={() => setTouched(true)}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-slate-700">End date</span>
          <input
            type="date"
            required
            value={draft.endDate}
            min={draft.startDate || undefined}
            onChange={(e) => setDraft({ ...draft, endDate: e.target.value })}
            onBlur={() => setTouched(true)}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          />
        </label>

        <div className="flex gap-2">
          <Button variant="secondary" onClick={apply} disabled={!isDirty || !!error} className="flex-1 sm:flex-none">
            View
          </Button>
          <Button type="submit" disabled={generating || !!error} className="flex-1 sm:flex-none">
            {generating && <Spinner />}
            {generating ? "Generating…" : "Generate payroll"}
          </Button>
        </div>
      </div>

      {touched && error && (
        <p className="text-sm text-rose-700" role="alert">
          {error}
        </p>
      )}

      <div className="flex flex-wrap items-center gap-2 text-xs">
        <span className="text-slate-500">Quick select:</span>
        <Chip onClick={() => quick(currentMonthPeriod())} active={periodsEqual(draft, currentMonthPeriod())}>
          This month
        </Chip>
        <Chip
          onClick={() => quick(shiftPeriod(currentMonthPeriod(), -1))}
          active={periodsEqual(draft, shiftPeriod(currentMonthPeriod(), -1))}
        >
          Last month
        </Chip>
        {recentRuns.length > 0 && <span className="ml-2 text-slate-500">Generated runs:</span>}
        {recentRuns.map((run) => {
          const p = { startDate: run.startDate, endDate: run.endDate };
          return (
            <Chip key={run.id} onClick={() => quick(p)} active={periodsEqual(draft, p)} title={`${run.instructorCount} instructor(s)`}>
              {formatDate(run.startDate)} – {formatDate(run.endDate)}
            </Chip>
          );
        })}
      </div>
    </form>
  );
}

function Chip({
  children,
  onClick,
  active,
  title,
}: {
  children: React.ReactNode;
  onClick: () => void;
  active?: boolean;
  title?: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-pressed={active}
      className={`rounded-full border px-2.5 py-1 transition ${
        active
          ? "border-indigo-600 bg-indigo-600 text-white"
          : "border-slate-300 bg-white text-slate-700 hover:border-indigo-400 hover:text-indigo-700"
      }`}
    >
      {children}
    </button>
  );
}
