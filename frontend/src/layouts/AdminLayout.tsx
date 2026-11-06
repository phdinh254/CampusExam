import { useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";

import { Icon, type IconName } from "../components/Icon";

const navItems: { label: string; to: string; icon: IconName }[] = [
  { label: "Tổng quan", to: "/admin", icon: "dashboard" },
  { label: "Ngân hàng câu hỏi", to: "/admin/questions", icon: "book" },
  { label: "Kỳ thi", to: "/admin/exams", icon: "calendar" },
  { label: "Thí sinh", to: "/admin/candidates", icon: "users" },
];

const pageTitles: Record<string, { title: string; eyebrow: string }> = {
  "/admin": { title: "Tổng quan", eyebrow: "Trung tâm điều hành" },
  "/admin/questions": { title: "Ngân hàng câu hỏi", eyebrow: "Học liệu" },
  "/admin/exams": { title: "Quản lý kỳ thi", eyebrow: "Tổ chức thi" },
  "/admin/candidates": { title: "Quản lý thí sinh", eyebrow: "Dự thi" },
};

export default function AdminLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();
  const heading = pageTitles[location.pathname] ?? {
    title: "CampusExam",
    eyebrow: "Quản trị hệ thống",
  };

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
            <button className="icon-button menu-button" onClick={() => setSidebarOpen(true)} type="button">
              <Icon name="menu" />
            </button>
            <div><span>{heading.eyebrow}</span><h1>{heading.title}</h1></div>
          </div>
          <div className="topbar-actions">
            <div className="profile-button" aria-label="Tài khoản quản trị viên">
              <span className="avatar avatar-indigo">NT</span>
              <span className="profile-copy"><strong>Nguyễn Thảo</strong><small>Quản trị viên</small></span>
            </div>
          </div>
        </header>
        <main className="admin-content"><Outlet /></main>
      </div>
    </div>
  );
}
