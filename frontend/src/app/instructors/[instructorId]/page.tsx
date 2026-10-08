import { Suspense } from "react";
import { Skeleton } from "@/components/ui";
import { InstructorDetail } from "@/features/payroll/InstructorDetail";

export default function InstructorDetailPage() {
  // Route params and the period (search params) are read client-side inside InstructorDetail.
  return (
    <Suspense fallback={<Skeleton className="h-64" />}>
      <InstructorDetail />
    </Suspense>
  );
}
