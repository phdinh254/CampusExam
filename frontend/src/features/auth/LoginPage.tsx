import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";

import { Icon } from "../../components/Icon";

export default function LoginPage() {
  const navigate = useNavigate();
  const [role, setRole] = useState<"admin" | "candidate">("admin");
  const [showPassword, setShowPassword] = useState(false);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    navigate(role === "admin" ? "/admin" : "/candidate");
  }

  return (
    <main className="login-page">
      <section className="login-showcase">
        <div className="login-orb orb-one" /><div className="login-orb orb-two" />
        <div className="showcase-brand">
          <span className="brand-mark light"><Icon name="book" size={23} /></span>
          <span><strong>Campus</strong>Exam</span>
        </div>
        <div className="showcase-copy">
          <span className="showcase-kicker"><Icon name="sparkles" size={16} /> HỆ THỐNG THI TRỰC TUYẾN</span>
          <h1>Quản lý kỳ thi<br /><em>tập trung, rõ ràng.</em></h1>
          <p>Một nền tảng cho quản trị viên và thí sinh.</p>
        </div>
        <p className="showcase-footer">© 2026 CampusExam · Đại học Duy Tân</p>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <div className="mobile-brand">
            <span className="brand-mark"><Icon name="book" size={21} /></span>
            <span><strong>Campus</strong>Exam</span>
          </div>
          <span className="login-eyebrow">CAMPUSEXAM</span>
          <h2>Đăng nhập</h2>
          <p className="login-subtitle">Nhập tài khoản do nhà trường cung cấp.</p>

          <div className="role-switcher" role="tablist" aria-label="Chọn vai trò">
            <button className={role === "admin" ? "active" : ""} onClick={() => setRole("admin")} type="button"><Icon name="dashboard" size={17} /> Quản trị viên</button>
            <button className={role === "candidate" ? "active" : ""} onClick={() => setRole("candidate")} type="button"><Icon name="users" size={17} /> Thí sinh</button>
          </div>

          <form onSubmit={handleSubmit}>
            <label className="field-label" htmlFor="username">Tên đăng nhập</label>
            <div className="input-wrap"><Icon name="users" size={18} /><input defaultValue={role === "admin" ? "admin" : "SV20230082"} id="username" key={role} autoComplete="username" /></div>
            <div className="password-label"><label className="field-label" htmlFor="password">Mật khẩu</label></div>
            <div className="input-wrap"><Icon name="lock" size={18} /><input defaultValue="campusexam123" id="password" type={showPassword ? "text" : "password"} autoComplete="current-password" /><button aria-label="Hiện mật khẩu" onClick={() => setShowPassword((value) => !value)} type="button">{showPassword ? "Ẩn" : "Hiện"}</button></div>
            <button className="button button-primary login-submit" type="submit">Đăng nhập <Icon name="arrow-right" size={18} /></button>
          </form>
        </div>
      </section>
    </main>
  );
}
