import { Suspense } from "react";
import { Skeleton } from "@/components/ui";
import { Dashboard } from "@/features/payroll/Dashboard";

export default function DashboardPage() {
  return (
    <>
      <div className="mb-5">
        <h1 className="text-2xl font-semibold">Payroll dashboard</h1>
        <p className="text-sm text-slate-500">Generate and review instructor payroll for a period.</p>
      </div>
      {/* Dashboard reads the period from the URL (useSearchParams), so it must be inside a Suspense boundary. */}
      <Suspense fallback={<Skeleton className="h-64" />}>
        <Dashboard />
      </Suspense>
    </>
  );
}
