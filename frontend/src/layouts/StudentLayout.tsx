import { useEffect, useRef, useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";

import { Icon } from "../components/Icon";
import { useAuth, useTaiKhoan } from "../features/auth/AuthProvider";
import { chuCaiDau, tenGoi } from "../features/auth/authUtils";
import "../styles/student-portal.css";

/** Menu cổng thí sinh: Trang chủ · Thi trực tuyến (Tham gia thi, Kết quả thi) · Tài khoản cá nhân. */
const thiTrucTuyen = [
  { label: "Tham gia thi", to: "/candidate/thi/tham-gia" },
  { label: "Kết quả thi", to: "/candidate/thi/ket-qua" },
];

export default function StudentLayout() {
  const taiKhoan = useTaiKhoan();
  const { dangXuat } = useAuth();
  const location = useLocation();
  const [moMenuThi, setMoMenuThi] = useState(false);
  const menuThiRef = useRef<HTMLDivElement>(null);
  const dangOTrangThi = location.pathname.startsWith("/candidate/thi");

  // Đóng menu con khi chuyển trang, bấm ra ngoài hoặc nhấn Escape.
  useEffect(() => setMoMenuThi(false), [location.pathname]);
  useEffect(() => {
    if (!moMenuThi) return undefined;
    const dong = (event: MouseEvent | KeyboardEvent) => {
      if (event instanceof KeyboardEvent ? event.key === "Escape" : !menuThiRef.current?.contains(event.target as Node)) {
        setMoMenuThi(false);
      }
    };
    document.addEventListener("mousedown", dong);
    document.addEventListener("keydown", dong);
    return () => {
      document.removeEventListener("mousedown", dong);
      document.removeEventListener("keydown", dong);
    };
  }, [moMenuThi]);

  return (
    <div className="student-shell">
      <header className="student-header candidate-header">
        <Link className="brand-lockup dark" to="/candidate">
          <span className="brand-mark"><Icon name="book" size={22} /></span>
          <span><strong>Campus</strong>Exam</span>
        </Link>

        <nav aria-label="Điều hướng thí sinh" className="candidate-nav">
          <NavLink end to="/candidate"><Icon name="home" size={16} /> Trang chủ</NavLink>
          <div className="candidate-nav-group" ref={menuThiRef}>
            <button
              aria-controls="menu-thi-truc-tuyen"
              aria-expanded={moMenuThi}
              className={dangOTrangThi ? "active" : undefined}
              onClick={() => setMoMenuThi((value) => !value)}
              type="button"
            >
              <Icon name="file" size={16} /> Thi trực tuyến <span aria-hidden="true" className="candidate-caret">▾</span>
            </button>
            {moMenuThi && (
              <div className="candidate-submenu" id="menu-thi-truc-tuyen">
                {thiTrucTuyen.map((item) => <NavLink key={item.to} to={item.to}>{item.label}</NavLink>)}
              </div>
            )}
          </div>
          {/* Điện thoại: thanh menu cuộn ngang sẽ cắt dropdown, nên hiện thẳng hai mục con. */}
          {thiTrucTuyen.map((item) => <NavLink className="candidate-nav-flat" key={item.to} to={item.to}>{item.label}</NavLink>)}
          <NavLink to="/candidate/tai-khoan"><Icon name="users" size={16} /> Tài khoản cá nhân</NavLink>
        </nav>

        <div className="student-profile">
          <span className="avatar avatar-teal">{chuCaiDau(taiKhoan.hoTen)}</span>
          <span><strong>{tenGoi(taiKhoan.hoTen)}</strong><small>{taiKhoan.msts}</small></span>
          <button aria-label="Đăng xuất" className="icon-button" onClick={() => void dangXuat()} title="Đăng xuất" type="button"><Icon name="logout" size={18} /></button>
        </div>
      </header>
      <main className="student-content"><Outlet /></main>
    </div>
  );
}
