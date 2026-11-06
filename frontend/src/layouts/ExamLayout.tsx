import { Link, Outlet } from "react-router-dom";

import { Icon } from "../components/Icon";
import { useTaiKhoan } from "../features/auth/AuthProvider";
import { chuCaiDau } from "../features/auth/authUtils";

export default function ExamLayout() {
  const taiKhoan = useTaiKhoan();

  return (
    <div className="exam-shell">
      <header className="exam-header">
        <Link className="brand-lockup dark" to="/candidate">
          <span className="brand-mark"><Icon name="book" size={21} /></span>
          <span><strong>Campus</strong>Exam</span>
        </Link>
        <div className="exam-header-title"><span>BÀI THI ĐANG DIỄN RA</span><strong>Cơ sở dữ liệu nâng cao</strong></div>
        <div className="exam-candidate"><span className="avatar avatar-teal">{chuCaiDau(taiKhoan.hoTen)}</span><span><strong>{taiKhoan.hoTen}</strong><small>{taiKhoan.msts}</small></span></div>
      </header>
      <main className="exam-content"><Outlet /></main>
    </div>
  );
}
