import { afterEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../services/httpClient";
import { authSession } from "../../services/authSession";
import type { TaiKhoanHienTai } from "../../types/api";
import { xacThucApi } from "./api";
import {
  chuCaiDau,
  duongDanSauDangNhap,
  tenGoi,
  thongBaoLoiDangNhap,
  thongBaoLoiDoiMatKhau,
  trangChuTheoVaiTro,
} from "./authUtils";

const admin: TaiKhoanHienTai = { mtk: 1, tenDN: "admin", hoTen: "Nguyễn Thảo", vaiTro: "ADMIN", msts: null, phaiDoiMatKhau: false };
const thiSinh: TaiKhoanHienTai = { mtk: 2, tenDN: "SV001", hoTen: "Nguyễn Minh Long", vaiTro: "THISINH", msts: "SV001", phaiDoiMatKhau: false };

describe("điều hướng sau đăng nhập", () => {
  it("trang chủ theo vai trò", () => {
    expect(trangChuTheoVaiTro("ADMIN")).toBe("/admin");
    expect(trangChuTheoVaiTro("THISINH")).toBe("/candidate");
  });

  it("bắt buộc đổi mật khẩu trước mọi trang khác", () => {
    expect(duongDanSauDangNhap({ ...thiSinh, phaiDoiMatKhau: true }, "/candidate")).toBe("/doi-mat-khau");
  });

  it("quay lại trang được yêu cầu nếu đúng vai trò", () => {
    expect(duongDanSauDangNhap(admin, "/admin/questions")).toBe("/admin/questions");
    expect(duongDanSauDangNhap(thiSinh, "/candidate")).toBe("/candidate");
  });

  it("không đưa thí sinh vào trang quản trị dù được yêu cầu", () => {
    expect(duongDanSauDangNhap(thiSinh, "/admin/questions")).toBe("/candidate");
    expect(duongDanSauDangNhap(admin, "/administrator")).toBe("/admin");
    expect(duongDanSauDangNhap(admin, null)).toBe("/admin");
  });
});

describe("hiển thị tên", () => {
  it("lấy chữ cái đầu của hai từ cuối", () => {
    expect(chuCaiDau("Nguyễn Minh Long")).toBe("ML");
    expect(chuCaiDau("Nguyễn Thảo")).toBe("NT");
    expect(chuCaiDau("admin")).toBe("A");
    expect(chuCaiDau("  ")).toBe("?");
  });

  it("tên gọi là hai từ cuối", () => {
    expect(tenGoi("Nguyễn Minh Long")).toBe("Minh Long");
    expect(tenGoi("Thảo")).toBe("Thảo");
  });
});

describe("thông báo lỗi đăng nhập", () => {
  it("dịch mã lỗi sang thông báo thân thiện", () => {
    expect(thongBaoLoiDangNhap(new ApiError(401, "AUTH_INVALID_CREDENTIALS", "x")))
      .toBe("Tên đăng nhập hoặc mật khẩu không đúng.");
    expect(thongBaoLoiDangNhap(new ApiError(403, "AUTH_ACCOUNT_DISABLED", "x")))
      .toContain("liên hệ quản trị viên");
  });

  it("hiện giờ hết tạm khóa theo giờ Việt Nam", () => {
    const error = new ApiError(423, "AUTH_ACCOUNT_LOCKED", "x", { khoaDen: "2026-11-06T02:15:00Z" });
    expect(thongBaoLoiDangNhap(error)).toBe("Tạm khóa trên máy này đến 09:15 do nhập sai mật khẩu nhiều lần.");
  });

  it("thông báo tạm khóa khi đổi mật khẩu dùng chung với đăng nhập", () => {
    const error = new ApiError(423, "AUTH_ACCOUNT_LOCKED", "x", { khoaDen: "2026-11-06T02:15:00Z" });
    expect(thongBaoLoiDoiMatKhau(error)).toBe("Tạm khóa trên máy này đến 09:15 do nhập sai mật khẩu nhiều lần.");
    expect(thongBaoLoiDoiMatKhau(new ApiError(500, "HTTP_ERROR", "Lỗi máy chủ"))).toBe("Lỗi máy chủ");
    expect(thongBaoLoiDoiMatKhau("không phải Error")).toBe("Đã có lỗi xảy ra. Vui lòng thử lại.");
  });

  it("dùng thông báo của ApiError cho lỗi mạng và rate limit", () => {
    expect(thongBaoLoiDangNhap(new ApiError(0, "NETWORK_ERROR", "Mất mạng"))).toBe("Mất mạng");
    expect(thongBaoLoiDangNhap(new Error("x"))).toBe("Đã có lỗi xảy ra. Vui lòng thử lại.");
  });
});

describe("xacThucApi", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    authSession.clear();
  });

  function stubFetch(status = 200, body: unknown = {}) {
    const fetchMock = vi.fn(async (_url: string, _init: RequestInit) =>
      status === 204
        ? new Response(null, { status })
        : new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);
    return fetchMock;
  }

  it("làm mới phiên gửi cookie và header chống CSRF, không gửi token cũ", async () => {
    authSession.setAccessToken("cu");
    const fetchMock = stubFetch();

    await xacThucApi.lamMoi();

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/xac-thuc/lam-moi");
    expect(init.method).toBe("POST");
    expect(init.credentials).toBe("include");
    const headers = new Headers(init.headers);
    expect(headers.get("X-CSRF")).toBe("1");
    expect(headers.get("Authorization")).toBeNull();
  });

  it("đăng xuất cũng gửi header chống CSRF", async () => {
    const fetchMock = stubFetch(204);

    await xacThucApi.dangXuat();

    expect(new Headers(fetchMock.mock.calls[0][1].headers).get("X-CSRF")).toBe("1");
  });

  it("đăng nhập sai không kích hoạt làm mới phiên", async () => {
    const refresh = vi.fn(async () => "moi");
    authSession.setRefreshHandler(refresh);
    stubFetch(401, { code: "AUTH_INVALID_CREDENTIALS" });

    await expect(xacThucApi.dangNhap({ tenDN: "a", matKhau: "b", ghiNho: false }))
      .rejects.toMatchObject({ code: "AUTH_INVALID_CREDENTIALS" });
    expect(refresh).not.toHaveBeenCalled();
    authSession.setRefreshHandler(null);
  });
});
