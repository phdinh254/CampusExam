import { Link } from "react-router-dom";

import { Icon, type IconName } from "../../components/Icon";
import { useTaiKhoan } from "../auth/AuthProvider";
import { tenGoi } from "../auth/authUtils";

const loiTat: { to: string; icon: IconName; title: string; text: string }[] = [
  { to: "/candidate/thi/tham-gia", icon: "file", title: "Tham gia thi", text: "Xem kỳ thi được phân công và vào phòng thi khi đến giờ." },
  { to: "/candidate/thi/ket-qua", icon: "check", title: "Kết quả thi", text: "Xem điểm các kỳ thi đã làm khi kỳ thi cho phép hiển thị." },
  { to: "/candidate/tai-khoan", icon: "users", title: "Tài khoản cá nhân", text: "Xem thông tin hồ sơ và đổi mật khẩu đăng nhập." },
];

/** "Thứ Hai, 09 tháng 11, 2026" theo giờ Việt Nam. */
function ngayHomNay() {
  const text = new Date().toLocaleDateString("vi-VN", {
    timeZone: "Asia/Ho_Chi_Minh",
    weekday: "long",
    day: "2-digit",
    month: "long",
    year: "numeric",
  });
  return text.charAt(0).toLocaleUpperCase("vi") + text.slice(1);
}

export default function StudentDashboard() {
  const taiKhoan = useTaiKhoan();

  return (
    <div className="student-dashboard">
      <section className="student-welcome">
        <div><span className="eyebrow">{ngayHomNay().toLocaleUpperCase("vi")}</span><h1>Chào {tenGoi(taiKhoan.hoTen)}!</h1></div>
      </section>

      <section className="candidate-shortcuts" aria-label="Lối tắt">
        {loiTat.map((item) => (
          <Link className="panel candidate-shortcut" key={item.to} to={item.to}>
            <span className="candidate-shortcut-icon"><Icon name={item.icon} size={20} /></span>
            <strong>{item.title}</strong>
            <span>{item.text}</span>
            <em>Mở <Icon name="arrow-right" size={14} /></em>
          </Link>
        ))}
      </section>

      <article className="panel notice-card">
        <div className="notice-icon"><Icon name="clock" /></div>
        <div><h3>Trước giờ thi</h3><p>Kiểm tra mạng, có mặt trước 10 phút và chỉ đăng nhập bằng tài khoản của mình.</p></div>
      </article>
    </div>
  );
}
