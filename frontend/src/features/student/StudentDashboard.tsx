import { Link } from "react-router-dom";

import { Icon, type IconName } from "../../components/Icon";
import { useTaiKhoan } from "../auth/AuthProvider";
import { tenGoi } from "../auth/authUtils";

const loiTat: { to: string; icon: IconName; title: string; text: string }[] = [
  { to: "/candidate/thi/tham-gia", icon: "file", title: "Tham gia thi", text: "Xem kỳ thi được phân công và vào phòng thi khi đến giờ." },
  { to: "/candidate/thi/ket-qua", icon: "check", title: "Kết quả thi", text: "Xem điểm các kỳ thi đã làm khi kỳ thi cho phép hiển thị." },
  { to: "/candidate/tai-khoan", icon: "users", title: "Tài khoản cá nhân", text: "Xem thông tin hồ sơ và đổi mật khẩu đăng nhập." },
];

/**
 * Bốn bước dự thi. Bước 1 luôn xong: vào được trang này nghĩa là đã đăng nhập và đổi mật
 * khẩu mặc định (tài khoản chưa đổi bị chuyển về trang đổi mật khẩu).
 */
const buocDuThi: { title: string; text: string; xong?: boolean; link?: { to: string; label: string } }[] = [
  { title: "Đăng nhập và đổi mật khẩu", text: "Bạn đã đăng nhập bằng mã số thí sinh và đổi mật khẩu mặc định.", xong: true },
  { title: "Kiểm tra lịch thi", text: "Xem kỳ thi được phân công, giờ bắt đầu và số máy. Có mặt trước giờ thi 10 phút.", link: { to: "/candidate/thi/tham-gia", label: "Tham gia thi" } },
  { title: "Làm bài", text: "Vào phòng thi khi đến giờ. Bài làm được tự lưu; không tải lại trang hay đóng trình duyệt. Gặp sự cố hãy báo ngay cho giám thị." },
  { title: "Nộp bài và xem kết quả", text: "Nộp bài trước khi hết giờ; hết giờ hệ thống tự thu bài. Điểm hiển thị sau khi nhà trường công bố kết quả.", link: { to: "/candidate/thi/ket-qua", label: "Kết quả thi" } },
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

      <section className="panel candidate-steps" aria-labelledby="huong-dan-du-thi">
        <div className="panel-heading"><div><h3 id="huong-dan-du-thi">Hướng dẫn dự thi</h3></div></div>
        <ol>
          {buocDuThi.map((buoc, index) => (
            <li className={buoc.xong ? "done" : undefined} key={buoc.title}>
              <span className="candidate-step-number">{buoc.xong ? <Icon name="check" size={15} /> : index + 1}</span>
              <div>
                <strong>{buoc.title}</strong>
                <p>{buoc.text}</p>
                {buoc.link && <Link to={buoc.link.to}>{buoc.link.label} <Icon name="arrow-right" size={13} /></Link>}
              </div>
            </li>
          ))}
        </ol>
      </section>
    </div>
  );
}
