import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

import { Icon } from "../../components/Icon";
import { ApiError } from "../../services/httpClient";
import type { HoSoCaNhan } from "../../types/api";
import { useAuth } from "../auth/AuthProvider";
import { DUONG_DAN_DOI_MAT_KHAU } from "../auth/authUtils";
import { GIOI_TINH, dinhDangNgaySinh } from "../students/studentForm";
import { toiApi } from "./api";

/** Tài khoản cá nhân của thí sinh: xem hồ sơ (không tự sửa, D-THISINH-01), đổi mật khẩu. */
export default function AccountPage() {
  const { dangXuat } = useAuth();
  const [hoSo, setHoSo] = useState<HoSoCaNhan | null>(null);
  const [loi, setLoi] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setLoi(null);
    toiApi.hoSo(controller.signal)
      .then(setHoSo)
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        setLoi(error instanceof ApiError ? error.message : "Không thể kết nối máy chủ. Vui lòng thử lại.");
      });
    return () => controller.abort();
  }, [reloadKey]);

  const dong: [string, string | null | undefined][] = hoSo
    ? [
        ["Mã số thí sinh", hoSo.msts],
        ["Họ tên", hoSo.hoTen],
        ["Ngày sinh", dinhDangNgaySinh(hoSo.ngaySinh)],
        ["Giới tính", hoSo.gioiTinh ? GIOI_TINH[hoSo.gioiTinh] : null],
        ["Lớp", hoSo.lop],
        ["Nơi sinh", hoSo.noiSinh],
        ["Số CCCD", hoSo.cccd],
      ]
    : [];

  return (
    <div className="student-dashboard">
      <section className="student-welcome">
        <div><span className="eyebrow">TÀI KHOẢN CÁ NHÂN</span><h1>Thông tin của bạn</h1></div>
      </section>

      <section className="candidate-account-grid">
        <article className="panel candidate-profile-card">
          <div className="panel-heading"><div><h3>Hồ sơ thí sinh</h3></div></div>
          {loi ? (
            <div className="empty-state error-state"><Icon name="alert" size={28} /><strong>Chưa tải được hồ sơ</strong><span>{loi}</span><button className="button button-secondary" onClick={() => setReloadKey((value) => value + 1)} type="button">Thử lại</button></div>
          ) : !hoSo ? (
            <div className="outcomes-loading" aria-label="Đang tải hồ sơ">{[1, 2, 3].map((item) => <span key={item} />)}</div>
          ) : (
            <dl className="candidate-profile-list">
              {dong.map(([nhan, giaTri]) => (
                <div key={nhan}><dt>{nhan}</dt><dd>{giaTri || <span className="muted-cell">Chưa khai báo</span>}</dd></div>
              ))}
            </dl>
          )}
          <p className="candidate-profile-note"><Icon name="alert" size={14} /> Thông tin sai? Liên hệ quản trị viên để được chỉnh sửa.</p>
        </article>

        <aside className="candidate-account-side">
          <article className="panel candidate-security-card">
            <span className="candidate-shortcut-icon"><Icon name="lock" size={20} /></span>
            <h3>Mật khẩu</h3>
            <p>Tên đăng nhập là mã số thí sinh. Đổi mật khẩu định kỳ và không chia sẻ cho người khác.</p>
            <Link className="button button-primary" to={DUONG_DAN_DOI_MAT_KHAU}><Icon name="lock" size={16} /> Đổi mật khẩu</Link>
          </article>
          <article className="panel candidate-security-card">
            <span className="candidate-shortcut-icon"><Icon name="logout" size={20} /></span>
            <h3>Đăng xuất</h3>
            <p>Luôn đăng xuất khi dùng máy chung ở phòng máy.</p>
            <button className="button button-secondary" onClick={() => void dangXuat()} type="button"><Icon name="logout" size={16} /> Đăng xuất</button>
          </article>
        </aside>
      </section>
    </div>
  );
}
