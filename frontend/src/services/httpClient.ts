import { authSession } from "./authSession";
import type { ProblemDetails, ValidationErrors } from "../types/api";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api";

const DEFAULT_MESSAGES: Record<number, string> = {
  0: "Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.",
  401: "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
  403: "Bạn không có quyền thực hiện thao tác này.",
  404: "Không tìm thấy dữ liệu.",
  413: "Tệp tải lên quá lớn.",
  429: "Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.",
};

/** Lỗi từ API, đọc từ ProblemDetails (docs/API_DESIGN.md mục 1.4). */
export class ApiError extends Error {
  readonly status: number;
  /** Mã ổn định để rẽ nhánh, ví dụ `VALIDATION_FAILED`, `KQHT_KY_HIEU_CONFLICT`. */
  readonly code: string;
  /** Lỗi theo từng trường, khóa trùng tên trường JSON. */
  readonly errors: ValidationErrors;
  readonly traceId?: string;
  readonly problem?: ProblemDetails;

  constructor(status: number, code: string, message: string, problem?: ProblemDetails) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.errors = problem?.errors ?? {};
    this.traceId = problem?.traceId;
    this.problem = problem;
  }
}

type QueryValue = string | number | boolean | null | undefined | ReadonlyArray<string | number>;
export type QueryParams = Record<string, QueryValue>;

export type RequestOptions = Omit<RequestInit, "body" | "method"> & {
  query?: QueryParams;
  /** Mặc định `json`; dùng `blob` khi tải file xuất. */
  responseType?: "json" | "blob" | "text";
  /** Không gắn token và không tự làm mới phiên khi 401 (đăng nhập, làm mới). */
  skipAuth?: boolean;
};

type Method = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

/** `{ trang: 2, trangThai: ["a", "b"], tuKhoa: "" }` → `?trang=2&trangThai=a%2Cb`. */
export function buildQuery(params?: QueryParams): string {
  if (!params) return "";
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    if (Array.isArray(value)) {
      if (value.length > 0) search.append(key, value.join(","));
    } else {
      search.append(key, String(value));
    }
  }
  const text = search.toString();
  return text ? `?${text}` : "";
}

function isRawBody(body: unknown): body is BodyInit {
  return (
    body instanceof FormData ||
    body instanceof Blob ||
    body instanceof URLSearchParams ||
    body instanceof ArrayBuffer
  );
}

async function toApiError(response: Response): Promise<ApiError> {
  const contentType = response.headers.get("Content-Type") ?? "";
  let problem: ProblemDetails | undefined;

  if (contentType.includes("json")) {
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = undefined;
    }
  }

  const message =
    problem?.detail ||
    problem?.title ||
    DEFAULT_MESSAGES[response.status] ||
    `Yêu cầu thất bại (HTTP ${response.status}).`;

  return new ApiError(response.status, problem?.code ?? "HTTP_ERROR", message, problem);
}

async function readBody<T>(response: Response, responseType: RequestOptions["responseType"]): Promise<T> {
  if (response.status === 204 || response.status === 205) {
    return undefined as T;
  }
  if (responseType === "blob") return (await response.blob()) as T;
  if (responseType === "text") return (await response.text()) as T;

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

async function send(method: Method, path: string, body: unknown, options: RequestOptions): Promise<Response> {
  const { query, responseType, skipAuth, headers, ...init } = options;
  const finalHeaders = new Headers(headers);
  if (!finalHeaders.has("Accept")) {
    finalHeaders.set("Accept", responseType === "json" || !responseType ? "application/json" : "*/*");
  }

  let finalBody: BodyInit | undefined;
  if (body !== undefined) {
    if (isRawBody(body)) {
      finalBody = body; // trình duyệt tự đặt Content-Type (kèm boundary cho multipart)
    } else {
      finalBody = JSON.stringify(body);
      finalHeaders.set("Content-Type", "application/json");
    }
  }

  const token = skipAuth ? null : authSession.getAccessToken();
  if (token) {
    finalHeaders.set("Authorization", `Bearer ${token}`);
  }

  try {
    return await fetch(`${API_BASE_URL}${path}${buildQuery(query)}`, {
      credentials: "same-origin",
      ...init,
      method,
      headers: finalHeaders,
      body: finalBody,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw new ApiError(0, "NETWORK_ERROR", DEFAULT_MESSAGES[0]);
  }
}

async function request<T>(method: Method, path: string, body: unknown, options: RequestOptions = {}): Promise<T> {
  let response = await send(method, path, body, options);

  if (response.status === 401 && !options.skipAuth) {
    const newToken = await authSession.refresh();
    if (newToken) {
      response = await send(method, path, body, options);
    }
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  return readBody<T>(response, options.responseType);
}

export const httpClient = {
  get: <T>(path: string, options?: RequestOptions) => request<T>("GET", path, undefined, options),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) => request<T>("POST", path, body, options),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) => request<T>("PUT", path, body, options),
  patch: <T>(path: string, body?: unknown, options?: RequestOptions) => request<T>("PATCH", path, body, options),
  delete: <T>(path: string, options?: RequestOptions) => request<T>("DELETE", path, undefined, options),
};
