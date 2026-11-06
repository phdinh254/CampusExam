import { Link, Outlet } from "react-router-dom";

import { Icon } from "../components/Icon";
import { useAuth, useTaiKhoan } from "../features/auth/AuthProvider";
import { chuCaiDau, tenGoi } from "../features/auth/authUtils";

export default function StudentLayout() {
  const taiKhoan = useTaiKhoan();
  const { dangXuat } = useAuth();

  return (
    <div className="student-shell">
      <header className="student-header">
        <Link className="brand-lockup dark" to="/candidate">
          <span className="brand-mark"><Icon name="book" size={22} /></span>
          <span><strong>Campus</strong>Exam</span>
        </Link>
        <span className="student-area-label">Cổng thí sinh</span>
        <div className="student-profile">
          <span className="avatar avatar-teal">{chuCaiDau(taiKhoan.hoTen)}</span>
          <span><strong>{tenGoi(taiKhoan.hoTen)}</strong><small>{taiKhoan.msts}</small></span>
          <Link aria-label="Đổi mật khẩu" className="icon-button" title="Đổi mật khẩu" to="/doi-mat-khau"><Icon name="lock" size={18} /></Link>
          <button aria-label="Đăng xuất" className="icon-button" onClick={() => void dangXuat()} title="Đăng xuất" type="button"><Icon name="logout" size={18} /></button>
        </div>
      </header>
      <main className="student-content"><Outlet /></main>
    </div>
  );
}
