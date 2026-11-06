import { Navigate, Outlet, useLocation } from "react-router-dom";

import type { VaiTro } from "../../types/api";
import { useAuth } from "./AuthProvider";
import { DUONG_DAN_DOI_MAT_KHAU, trangChuTheoVaiTro } from "./authUtils";

type Props = {
  /** Bỏ trống: chỉ cần đăng nhập. */
  vaiTro?: VaiTro;
  /** Cho phép vào khi tài khoản còn bị bắt buộc đổi mật khẩu (chỉ trang đổi mật khẩu). */
  choPhepPhaiDoiMatKhau?: boolean;
};

export function ManHinhCho() {
  return (
    <div className="session-loading" role="status" aria-live="polite">
      <span className="session-spinner" aria-hidden="true" />
      <span>Đang kiểm tra phiên đăng nhập…</span>
    </div>
  );
}

export default function RequireRole({ vaiTro, choPhepPhaiDoiMatKhau = false }: Props) {
  const { trangThai, taiKhoan } = useAuth();
  const location = useLocation();

  if (trangThai === "dang_tai") return <ManHinhCho />;

  if (!taiKhoan) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }

  if (taiKhoan.phaiDoiMatKhau && !choPhepPhaiDoiMatKhau) {
    return <Navigate to={DUONG_DAN_DOI_MAT_KHAU} replace />;
  }

  if (vaiTro && taiKhoan.vaiTro !== vaiTro) {
    return <Navigate to={trangChuTheoVaiTro(taiKhoan.vaiTro)} replace />;
  }

  return <Outlet />;
}

/** "/" và đường dẫn lạ: về trang chủ của vai trò, hoặc trang đăng nhập. */
export function TrangChuRedirect() {
  const { trangThai, taiKhoan } = useAuth();
  if (trangThai === "dang_tai") return <ManHinhCho />;
  return <Navigate to={taiKhoan ? trangChuTheoVaiTro(taiKhoan.vaiTro) : "/login"} replace />;
}
