import type { IsoDate, PayrollPeriod } from "./types";

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

export function toIsoDate(d: Date): IsoDate {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function isValidIsoDate(value: string | null | undefined): value is IsoDate {
  if (!value || !ISO_DATE.test(value)) return false;
  const [y, m, d] = value.split("-").map(Number);
  const date = new Date(y, m - 1, d);
  return date.getFullYear() === y && date.getMonth() === m - 1 && date.getDate() === d;
}

export function monthPeriod(year: number, monthIndex: number): PayrollPeriod {
  const start = new Date(year, monthIndex, 1);
  const end = new Date(year, monthIndex + 1, 0);
  return { startDate: toIsoDate(start), endDate: toIsoDate(end) };
}

export function currentMonthPeriod(now = new Date()): PayrollPeriod {
  return monthPeriod(now.getFullYear(), now.getMonth());
}

/** Shifts a period by whole months when it is exactly a calendar month; otherwise shifts by its own length. */
export function shiftPeriod(p: PayrollPeriod, direction: 1 | -1): PayrollPeriod {
  const start = parse(p.startDate);
  const end = parse(p.endDate);
  const isWholeMonth = start.getDate() === 1 && isLastDayOfMonth(end) && sameMonth(start, end);
  if (isWholeMonth) {
    return monthPeriod(start.getFullYear(), start.getMonth() + direction);
  }
  const lengthDays = Math.round((end.getTime() - start.getTime()) / 86_400_000) + 1;
  const newStart = new Date(start);
  newStart.setDate(start.getDate() + direction * lengthDays);
  const newEnd = new Date(newStart);
  newEnd.setDate(newStart.getDate() + lengthDays - 1);
  return { startDate: toIsoDate(newStart), endDate: toIsoDate(newEnd) };
}

/** Returns a validation message or null when the period is acceptable. Mirrors the API's rules. */
export function validatePeriod(p: PayrollPeriod): string | null {
  if (!isValidIsoDate(p.startDate) || !isValidIsoDate(p.endDate)) return "Please enter both a start and an end date.";
  if (p.endDate < p.startDate) return "End date must be on or after the start date.";
  const days = Math.round((parse(p.endDate).getTime() - parse(p.startDate).getTime()) / 86_400_000) + 1;
  if (days > 366) return "A payroll period cannot be longer than 366 days.";
  return null;
}

export function periodsEqual(a: PayrollPeriod, b: PayrollPeriod): boolean {
  return a.startDate === b.startDate && a.endDate === b.endDate;
}

export function periodFromSearchParams(params: URLSearchParams, fallback: PayrollPeriod): PayrollPeriod {
  const startDate = params.get("startDate");
  const endDate = params.get("endDate");
  return isValidIsoDate(startDate) && isValidIsoDate(endDate) ? { startDate, endDate } : fallback;
}

function parse(iso: IsoDate): Date {
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y, m - 1, d);
}

function isLastDayOfMonth(d: Date): boolean {
  const next = new Date(d);
  next.setDate(d.getDate() + 1);
  return next.getMonth() !== d.getMonth();
}

function sameMonth(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth();
}
