import { httpClient } from "../../services/httpClient";
import type { DangNhapRequest, DoiMatKhauRequest, PhienDangNhap, TaiKhoanHienTai } from "../../types/api";

/** Các lệnh dùng cookie refresh phải gửi header này (chống CSRF, API_DESIGN.md mục 1.5). */
const CSRF_HEADERS = { "X-CSRF": "1" };

export const xacThucApi = {
  dangNhap: (body: DangNhapRequest) =>
    httpClient.post<PhienDangNhap>("/xac-thuc/dang-nhap", body, { skipAuth: true, credentials: "include" }),

  lamMoi: () =>
    httpClient.post<PhienDangNhap>("/xac-thuc/lam-moi", undefined, {
      skipAuth: true,
      credentials: "include",
      headers: CSRF_HEADERS,
    }),

  dangXuat: () =>
    httpClient.post<void>("/xac-thuc/dang-xuat", undefined, {
      skipAuth: true,
      credentials: "include",
      headers: CSRF_HEADERS,
    }),

  toi: (signal?: AbortSignal) => httpClient.get<TaiKhoanHienTai>("/xac-thuc/toi", { signal }),

  doiMatKhau: (body: DoiMatKhauRequest) =>
    httpClient.post<PhienDangNhap>("/xac-thuc/doi-mat-khau", body, { credentials: "include" }),
};
