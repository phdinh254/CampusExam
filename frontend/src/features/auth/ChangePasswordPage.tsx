import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";

import { Icon } from "../../components/Icon";
import { ApiError } from "../../services/httpClient";
import { useAuth, useTaiKhoan } from "./AuthProvider";
import { thongBaoLoiDoiMatKhau, trangChuTheoVaiTro } from "./authUtils";

type LoiTruong = Partial<Record<"matKhauCu" | "matKhauMoi" | "xacNhan", string>>;

export default function ChangePasswordPage() {
  const navigate = useNavigate();
  const { doiMatKhau, dangXuat } = useAuth();
  const taiKhoan = useTaiKhoan();
  const [matKhauCu, setMatKhauCu] = useState("");
  const [matKhauMoi, setMatKhauMoi] = useState("");
  const [xacNhan, setXacNhan] = useState("");
  const [loiTruong, setLoiTruong] = useState<LoiTruong>({});
  const [loiChung, setLoiChung] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);

  const batBuoc = taiKhoan.phaiDoiMatKhau;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const loi: LoiTruong = {};
    if (!matKhauCu) loi.matKhauCu = "Nhập mật khẩu hiện tại.";
    if (!matKhauMoi) loi.matKhauMoi = "Nhập mật khẩu mới.";
    if (matKhauMoi && xacNhan !== matKhauMoi) loi.xacNhan = "Mật khẩu nhập lại không khớp.";
    setLoiTruong(loi);
    setLoiChung(null);
    if (Object.keys(loi).length > 0) return;

    setDangGui(true);
    try {
      const moi = await doiMatKhau({ matKhauCu, matKhauMoi });
      navigate(trangChuTheoVaiTro(moi.vaiTro), { replace: true });
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.errors).length > 0) {
        setLoiTruong({
          matKhauCu: error.errors.matKhauCu?.join(" "),
          matKhauMoi: error.errors.matKhauMoi?.join(" "),
        });
      } else {
        setLoiChung(thongBaoLoiDoiMatKhau(error));
      }
    } finally {
      setDangGui(false);
    }
  }

  return (
    <main className="login-page single">
      <section className="login-panel">
        <div className="login-card">
          <div className="mobile-brand visible">
            <span className="brand-mark"><Icon name="book" size={21} /></span>
            <span><strong>Campus</strong>Exam</span>
          </div>
          <span className="login-eyebrow">{taiKhoan.msts ?? taiKhoan.tenDN}</span>
          <h2>Đổi mật khẩu</h2>
          <p className="login-subtitle">
            {batBuoc
              ? "Đây là lần đăng nhập đầu tiên hoặc mật khẩu vừa được cấp lại. Bạn cần đặt mật khẩu mới trước khi tiếp tục."
              : "Đặt mật khẩu mới cho tài khoản của bạn. Mọi thiết bị khác sẽ bị đăng xuất."}
          </p>

          <form noValidate onSubmit={handleSubmit}>
            {loiChung && <div className="form-alert" role="alert"><Icon name="alert" size={16} /><span>{loiChung}</span></div>}

            <TruongMatKhau autoComplete="current-password" id="matKhauCu" label={batBuoc && taiKhoan.msts ? "Mật khẩu hiện tại (ngày sinh)" : "Mật khẩu hiện tại"} loi={loiTruong.matKhauCu} onChange={setMatKhauCu} value={matKhauCu} />
            <TruongMatKhau autoComplete="new-password" id="matKhauMoi" label="Mật khẩu mới" loi={loiTruong.matKhauMoi} onChange={setMatKhauMoi} value={matKhauMoi} />
            <TruongMatKhau autoComplete="new-password" id="xacNhan" label="Nhập lại mật khẩu mới" loi={loiTruong.xacNhan} onChange={setXacNhan} value={xacNhan} />

            <ul className="password-rules">
              <li>Từ 8 đến 128 ký tự, có cả chữ và số.</li>
              <li>Không chứa tên đăng nhập{taiKhoan.msts ? " hoặc ngày sinh" : ""}.</li>
              <li>Khác mật khẩu hiện tại.</li>
            </ul>

            <button className="button button-primary login-submit" disabled={dangGui} type="submit">
              {dangGui ? "Đang lưu…" : "Đổi mật khẩu"}
            </button>
          </form>

          <div className="login-secondary-actions">
            {batBuoc
              ? <button className="link-button" onClick={() => void dangXuat()} type="button"><Icon name="logout" size={15} /> Đăng xuất</button>
              : <button className="link-button" onClick={() => navigate(trangChuTheoVaiTro(taiKhoan.vaiTro))} type="button"><Icon name="arrow-left" size={15} /> Về trang chủ</button>}
          </div>
        </div>
      </section>
    </main>
  );
}

type TruongMatKhauProps = {
  id: string;
  label: string;
  value: string;
  loi?: string;
  autoComplete: string;
  onChange: (value: string) => void;
};

function TruongMatKhau({ id, label, value, loi, autoComplete, onChange }: TruongMatKhauProps) {
  return (
    <div className="field-group">
      <label className="field-label" htmlFor={id}>{label}</label>
      <div className={`input-wrap ${loi ? "has-error" : ""}`}>
        <Icon name="lock" size={18} />
        <input
          aria-describedby={loi ? `${id}-loi` : undefined}
          aria-invalid={loi ? true : undefined}
          autoComplete={autoComplete}
          id={id}
          onChange={(event) => onChange(event.target.value)}
          type="password"
          value={value}
        />
      </div>
      {loi && <p className="field-error" id={`${id}-loi`}>{loi}</p>}
    </div>
  );
}
