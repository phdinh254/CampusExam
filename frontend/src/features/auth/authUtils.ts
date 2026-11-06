import { ApiError } from "../../services/httpClient";
import type { TaiKhoanHienTai, VaiTro } from "../../types/api";

export const DUONG_DAN_DOI_MAT_KHAU = "/doi-mat-khau";

const TRANG_CHU: Record<VaiTro, string> = {
  ADMIN: "/admin",
  THISINH: "/candidate",
};

export function trangChuTheoVaiTro(vaiTro: VaiTro): string {
  return TRANG_CHU[vaiTro];
}

/** Nơi chuyển tới sau đăng nhập: bắt buộc đổi mật khẩu trước, rồi trang được yêu cầu nếu đúng vai trò. */
export function duongDanSauDangNhap(taiKhoan: TaiKhoanHienTai, trangYeuCau?: string | null): string {
  if (taiKhoan.phaiDoiMatKhau) return DUONG_DAN_DOI_MAT_KHAU;

  const trangChu = trangChuTheoVaiTro(taiKhoan.vaiTro);
  if (trangYeuCau && (trangYeuCau === trangChu || trangYeuCau.startsWith(`${trangChu}/`))) {
    return trangYeuCau;
  }
  return trangChu;
}

/** "Nguyễn Minh Long" → "ML"; "admin" → "A". */
export function chuCaiDau(hoTen: string): string {
  const tu = hoTen.trim().split(/\s+/).filter(Boolean);
  if (tu.length === 0) return "?";
  const chon = tu.length === 1 ? [tu[0]] : tu.slice(-2);
  return chon.map((t) => t[0].toLocaleUpperCase("vi-VN")).join("");
}

/** Tên gọi trong lời chào: hai từ cuối của họ tên, ví dụ "Minh Long". */
export function tenGoi(hoTen: string): string {
  const tu = hoTen.trim().split(/\s+/).filter(Boolean);
  return tu.slice(-2).join(" ") || hoTen;
}

const dinhDangGio = new Intl.DateTimeFormat("vi-VN", {
  hour: "2-digit",
  minute: "2-digit",
  timeZone: "Asia/Ho_Chi_Minh",
});

export function thongBaoLoiDangNhap(error: unknown): string {
  if (!(error instanceof ApiError)) return "Đã có lỗi xảy ra. Vui lòng thử lại.";

  switch (error.code) {
    case "AUTH_INVALID_CREDENTIALS":
      return "Tên đăng nhập hoặc mật khẩu không đúng.";
    case "AUTH_ACCOUNT_LOCKED": {
      const khoaDen = error.problem?.khoaDen ? new Date(error.problem.khoaDen) : null;
      return khoaDen && !Number.isNaN(khoaDen.getTime())
        ? `Tài khoản tạm khóa đến ${dinhDangGio.format(khoaDen)} do đăng nhập sai nhiều lần.`
        : "Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau.";
    }
    case "AUTH_ACCOUNT_DISABLED":
      return "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";
    case "VALIDATION_FAILED":
      return "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
    default:
      return error.message;
  }
}
