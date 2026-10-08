"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { payrollApi } from "@/lib/api";
import type { GeneratePayrollRequest, PayrollPeriod } from "@/lib/types";

export const payrollKeys = {
  all: ["payroll"] as const,
  runs: () => [...payrollKeys.all, "runs"] as const,
  summary: (p: PayrollPeriod) => [...payrollKeys.all, "summary", p.startDate, p.endDate] as const,
  detail: (instructorId: string, p: PayrollPeriod) =>
    [...payrollKeys.all, "detail", instructorId, p.startDate, p.endDate] as const,
};

export function usePayrollSummary(period: PayrollPeriod, enabled = true) {
  return useQuery({
    queryKey: payrollKeys.summary(period),
    queryFn: () => payrollApi.getSummary(period),
    enabled,
  });
}

export function usePayrollRuns() {
  return useQuery({
    queryKey: payrollKeys.runs(),
    queryFn: payrollApi.listRuns,
  });
}

export function useInstructorDetail(instructorId: string, period: PayrollPeriod, enabled = true) {
  return useQuery({
    queryKey: payrollKeys.detail(instructorId, period),
    queryFn: () => payrollApi.getInstructorDetail(instructorId, period),
    enabled,
    retry: (failureCount, error) => {
      // 404 is a definitive answer (no payroll for this instructor/period) — don't hammer the API.
      const status = (error as { status?: number }).status;
      return status !== 404 && failureCount < 2;
    },
  });
}

export function useGeneratePayroll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: GeneratePayrollRequest) => payrollApi.generate(body),
    onSuccess: (data, variables) => {
      const period = { startDate: variables.startDate, endDate: variables.endDate };
      queryClient.setQueryData(payrollKeys.summary(period), data.summary);
      void queryClient.invalidateQueries({ queryKey: payrollKeys.runs() });
      if (data.regenerated) {
        void queryClient.invalidateQueries({ queryKey: [...payrollKeys.all, "detail"] });
      }
    },
  });
}
