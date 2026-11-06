import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";

import { Icon, type IconName } from "../components/Icon";
import { useAuth, useTaiKhoan } from "../features/auth/AuthProvider";
import { chuCaiDau } from "../features/auth/authUtils";

const navItems: { label: string; to: string; icon: IconName }[] = [
  { label: "Tổng quan", to: "/admin", icon: "dashboard" },
  { label: "Ngân hàng câu hỏi", to: "/admin/questions", icon: "book" },
  { label: "Kỳ thi", to: "/admin/exams", icon: "calendar" },
  { label: "Thí sinh", to: "/admin/candidates", icon: "users" },
];

export default function AdminLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const taiKhoan = useTaiKhoan();
  const { dangXuat } = useAuth();

  return (
    <div className="admin-shell">
      <button
        aria-label="Đóng menu"
        className={`sidebar-backdrop ${sidebarOpen ? "is-open" : ""}`}
        onClick={() => setSidebarOpen(false)}
        type="button"
      />
      <aside className={`admin-sidebar ${sidebarOpen ? "is-open" : ""}`}>
        <NavLink className="brand-lockup" to="/admin" onClick={() => setSidebarOpen(false)}>
          <span className="brand-mark"><Icon name="book" size={22} /></span>
          <span><strong>Campus</strong>Exam</span>
        </NavLink>

        <nav className="sidebar-nav" aria-label="Điều hướng quản trị">
          {navItems.map((item) => (
            <NavLink
              className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}
              end={item.to === "/admin"}
              key={item.to}
              onClick={() => setSidebarOpen(false)}
              to={item.to}
            >
              <Icon name={item.icon} size={19} />
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

      </aside>

      <div className="admin-main">
        <header className="topbar">
          <div className="topbar-title">
            <button aria-label="Mở menu" className="icon-button menu-button" onClick={() => setSidebarOpen(true)} type="button">
              <Icon name="menu" />
            </button>
          </div>
          <div className="topbar-actions">
            <div className="profile-button" aria-label="Tài khoản quản trị viên">
              <span className="avatar avatar-indigo">{chuCaiDau(taiKhoan.hoTen)}</span>
              <span className="profile-copy"><strong>{taiKhoan.hoTen}</strong><small>Quản trị viên</small></span>
            </div>
            <NavLink aria-label="Đổi mật khẩu" className="icon-button" title="Đổi mật khẩu" to="/doi-mat-khau"><Icon name="lock" size={18} /></NavLink>
            <button aria-label="Đăng xuất" className="icon-button" onClick={() => void dangXuat()} title="Đăng xuất" type="button"><Icon name="logout" size={18} /></button>
          </div>
        </header>
        <main className="admin-content"><Outlet /></main>
      </div>
    </div>
  );
}
