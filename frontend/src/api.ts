export const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:8080";

export type ClassItem = {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
};

export type Hold = {
  holdId: string;
  classId: string;
  status: string;
  expiresAt: string;
};

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(
  path: string,
  opts: { method?: string; body?: unknown; studentId?: string } = {}
): Promise<T> {
  const headers: Record<string, string> = {};
  if (opts.body !== undefined) headers["Content-Type"] = "application/json";
  if (opts.studentId) headers["X-Student-Id"] = opts.studentId;

  let res: Response;
  try {
    res = await fetch(`${API_URL}${path}`, {
      method: opts.method ?? "GET",
      headers,
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    });
  } catch {
    throw new ApiError(0, "সার্ভারের সাথে সংযোগ করা যাচ্ছে না। একটু পরে আবার চেষ্টা করুন।");
  }

  if (res.status === 204) return undefined as T;
  const data = await res.json().catch(() => null);
  if (!res.ok) {
    throw new ApiError(res.status, data?.error ?? "কিছু একটা সমস্যা হয়েছে। আবার চেষ্টা করুন।");
  }
  return data as T;
}

export const api = {
  listClasses: () => request<ClassItem[]>("/classes"),
  createClass: (name: string, totalSeats: number) =>
    request<{ id: string }>("/classes", { method: "POST", body: { name, totalSeats } }),
  createHold: (classId: string, studentId: string) =>
    request<Hold>(`/classes/${classId}/holds`, { method: "POST", studentId }),
};