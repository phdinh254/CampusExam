import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";

import { Icon } from "../../components/Icon";
import { useAuth } from "./AuthProvider";
import { duongDanSauDangNhap, thongBaoLoiDangNhap } from "./authUtils";
import { ManHinhCho } from "./RequireRole";

type CheDo = "admin" | "candidate";

export default function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { trangThai, taiKhoan, dangNhap } = useAuth();
  const [cheDo, setCheDo] = useState<CheDo>("admin");
  const [tenDN, setTenDN] = useState("");
  const [matKhau, setMatKhau] = useState("");
  const [ghiNho, setGhiNho] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [dangGui, setDangGui] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  const trangYeuCau = (location.state as { from?: string } | null)?.from ?? null;

  if (trangThai === "dang_tai") return <ManHinhCho />;
  if (taiKhoan) return <Navigate to={duongDanSauDangNhap(taiKhoan, trangYeuCau)} replace />;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!tenDN.trim() || !matKhau) {
      setLoi("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
      return;
    }

    setDangGui(true);
    setLoi(null);
    try {
      const daDangNhap = await dangNhap({ tenDN: tenDN.trim(), matKhau, ghiNho });
      navigate(duongDanSauDangNhap(daDangNhap, trangYeuCau), { replace: true });
    } catch (error) {
      setLoi(thongBaoLoiDangNhap(error));
      setMatKhau("");
    } finally {
      setDangGui(false);
    }
  }

  const laThiSinh = cheDo === "candidate";

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

          {/* Chỉ đổi gợi ý nhập liệu; vai trò thật do máy chủ trả về. */}
          <div className="role-switcher" role="group" aria-label="Bạn là">
            <button aria-pressed={!laThiSinh} className={!laThiSinh ? "active" : ""} onClick={() => setCheDo("admin")} type="button"><Icon name="dashboard" size={17} /> Quản trị viên</button>
            <button aria-pressed={laThiSinh} className={laThiSinh ? "active" : ""} onClick={() => setCheDo("candidate")} type="button"><Icon name="users" size={17} /> Thí sinh</button>
          </div>

          <form noValidate onSubmit={handleSubmit}>
            {loi && <div className="form-alert" role="alert"><Icon name="alert" size={16} /><span>{loi}</span></div>}

            <label className="field-label" htmlFor="username">{laThiSinh ? "Mã số thí sinh" : "Tên đăng nhập"}</label>
            <div className="input-wrap">
              <Icon name="users" size={18} />
              <input
                autoCapitalize="none"
                autoComplete="username"
                id="username"
                onChange={(event) => setTenDN(event.target.value)}
                placeholder={laThiSinh ? "VD: SV20230082" : "Tên đăng nhập quản trị"}
                spellCheck={false}
                value={tenDN}
              />
            </div>

            <div className="password-label"><label className="field-label" htmlFor="password">Mật khẩu</label></div>
            <div className="input-wrap">
              <Icon name="lock" size={18} />
              <input
                autoComplete="current-password"
                id="password"
                onChange={(event) => setMatKhau(event.target.value)}
                type={showPassword ? "text" : "password"}
                value={matKhau}
              />
              <button aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"} onClick={() => setShowPassword((value) => !value)} type="button">{showPassword ? "Ẩn" : "Hiện"}</button>
            </div>
            {laThiSinh && (
              <p className="field-hint">Lần đầu đăng nhập, mật khẩu là ngày sinh dạng ngày-tháng-năm viết liền, ví dụ 14032005.</p>
            )}

            <label className="remember-row">
              <input checked={ghiNho} onChange={(event) => setGhiNho(event.target.checked)} type="checkbox" />
              <span>Ghi nhớ đăng nhập trên thiết bị này (7 ngày)</span>
            </label>

            <button className="button button-primary login-submit" disabled={dangGui} type="submit">
              {dangGui ? "Đang đăng nhập…" : <>Đăng nhập <Icon name="arrow-right" size={18} /></>}
            </button>
          </form>
          <p className="login-help">Quên mật khẩu? Liên hệ quản trị viên để được cấp lại.</p>
        </div>
      </section>
    </main>
  );
}
