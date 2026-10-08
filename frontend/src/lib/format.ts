import type { IsoDate, PayrollPeriod } from "./types";

const currencyFormatters = new Map<string, Intl.NumberFormat>();

export function formatMoney(amount: number, currency = "SGD"): string {
  let f = currencyFormatters.get(currency);
  if (!f) {
    // currencyDisplay "code" → "SGD 403.00" rather than an ambiguous "$"; studios may run in different currencies.
    f = new Intl.NumberFormat("en-SG", {
      style: "currency",
      currency,
      currencyDisplay: "code",
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
    currencyFormatters.set(currency, f);
  }
  return f.format(amount);
}

/** Signed money for adjustments: "+SGD 15.00" / "−SGD 7.50" / "SGD 0.00". */
export function formatSignedMoney(amount: number, currency = "SGD"): string {
  if (amount > 0) return `+${formatMoney(amount, currency)}`;
  if (amount < 0) return `−${formatMoney(Math.abs(amount), currency)}`;
  return formatMoney(0, currency);
}

export function formatPercent(rate: number): string {
  return `${Math.round(rate * 10000) / 100}%`;
}

const dateFormatter = new Intl.DateTimeFormat("en-SG", { day: "2-digit", month: "short", year: "numeric" });
const dateTimeFormatter = new Intl.DateTimeFormat("en-SG", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

/** Parses yyyy-MM-dd as a local calendar date (avoids the UTC-shift bug of `new Date("2026-06-01")`). */
export function parseIsoDate(iso: IsoDate): Date {
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y, m - 1, d);
}

export function formatDate(iso: IsoDate): string {
  return dateFormatter.format(parseIsoDate(iso));
}

export function formatDateTime(isoDateTime: string): string {
  // API emits local studio time without offset; display as-is.
  return dateTimeFormatter.format(new Date(isoDateTime));
}

export function formatUtcDateTime(isoUtc: string): string {
  const d = new Date(isoUtc.endsWith("Z") ? isoUtc : `${isoUtc}Z`);
  return `${dateTimeFormatter.format(d)}`;
}

export function formatPeriod(p: PayrollPeriod): string {
  return `${formatDate(p.startDate)} – ${formatDate(p.endDate)}`;
}
