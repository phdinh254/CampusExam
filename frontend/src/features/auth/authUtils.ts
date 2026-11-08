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

/** Khóa áp dụng cho cặp tài khoản + IP (D-AUTH-KHOA-01), nên chỉ máy này bị chặn. */
function thongBaoTamKhoa(error: ApiError): string {
  const khoaDen = error.problem?.khoaDen ? new Date(error.problem.khoaDen) : null;
  return khoaDen && !Number.isNaN(khoaDen.getTime())
    ? `Tạm khóa trên máy này đến ${dinhDangGio.format(khoaDen)} do nhập sai mật khẩu nhiều lần.`
    : "Tạm khóa trên máy này do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau.";
}

/** Lỗi chung (không theo trường) của màn hình đổi mật khẩu. */
export function thongBaoLoiDoiMatKhau(error: unknown): string {
  if (error instanceof ApiError && error.code === "AUTH_ACCOUNT_LOCKED") return thongBaoTamKhoa(error);
  return error instanceof Error ? error.message : "Đã có lỗi xảy ra. Vui lòng thử lại.";
}

export function thongBaoLoiDangNhap(error: unknown): string {
  if (!(error instanceof ApiError)) return "Đã có lỗi xảy ra. Vui lòng thử lại.";

  switch (error.code) {
    case "AUTH_INVALID_CREDENTIALS":
      return "Tên đăng nhập hoặc mật khẩu không đúng.";
    case "AUTH_ACCOUNT_LOCKED":
      return thongBaoTamKhoa(error);
    case "AUTH_ACCOUNT_DISABLED":
      return "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";
    case "VALIDATION_FAILED":
      return "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
    default:
      return error.message;
  }
}
