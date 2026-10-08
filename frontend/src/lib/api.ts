import type {
  GeneratePayrollRequest,
  GeneratePayrollResponse,
  InstructorPayrollDetail,
  PayrollPeriod,
  PayrollRun,
  PayrollSummary,
  ProblemDetails,
} from "./types";

export const API_BASE_URL = (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5193").replace(/\/$/, "");

/** Thrown for any non-2xx response. Carries the server's ProblemDetails so the UI can show a precise message. */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null, fallback: string) {
    super(problem?.detail ?? problem?.title ?? fallback);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: { Accept: "application/json", ...(init?.body ? { "Content-Type": "application/json" } : {}), ...init?.headers },
      cache: "no-store",
    });
  } catch {
    throw new ApiError(0, null, `Cannot reach the payroll API at ${API_BASE_URL}. Is the backend running?`);
  }

  if (!response.ok) {
    let problem: ProblemDetails | null = null;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      /* non-JSON error body */
    }
    throw new ApiError(response.status, problem, `Request failed with HTTP ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

const periodQuery = (p: PayrollPeriod) =>
  `startDate=${encodeURIComponent(p.startDate)}&endDate=${encodeURIComponent(p.endDate)}`;

export const payrollApi = {
  generate: (body: GeneratePayrollRequest) =>
    request<GeneratePayrollResponse>("/api/payroll/generate", { method: "POST", body: JSON.stringify(body) }),

  getSummary: (period: PayrollPeriod) => request<PayrollSummary[]>(`/api/payroll?${periodQuery(period)}`),

  getInstructorDetail: (instructorId: string, period: PayrollPeriod) =>
    request<InstructorPayrollDetail>(`/api/payroll/${encodeURIComponent(instructorId)}?${periodQuery(period)}`),

  listRuns: () => request<PayrollRun[]>("/api/payroll/runs"),
};
