import { Link } from "react-router-dom";

import { Icon } from "../../components/Icon";
import { useTaiKhoan } from "../auth/AuthProvider";
import { tenGoi } from "../auth/authUtils";

export default function StudentDashboard() {
  const taiKhoan = useTaiKhoan();

  return (
    <div className="student-dashboard">
      <section className="student-welcome">
        <div><span className="eyebrow">THỨ SÁU, 06 THÁNG 11</span><h1>Chào {tenGoi(taiKhoan.hoTen)}!</h1></div>
        <div className="student-semester"><small>HỌC KỲ HIỆN TẠI</small><strong>Học kỳ I · 2026–2027</strong></div>
      </section>

      <section className="next-exam-card">
        <div className="next-exam-date"><span>HÔM NAY</span><strong>06</strong><small>THÁNG 11</small></div>
        <div className="next-exam-main"><span className="live-pill"><i /> KỲ THI SẮP DIỄN RA</span><h2>Cơ sở dữ liệu nâng cao</h2><p>Mã học phần CSE304 · 60 phút · 40 câu hỏi</p><div className="exam-facts"><span><Icon name="clock" size={17} /> 09:00 – 10:00</span><span><Icon name="home" size={17} /> Phòng A203</span><span><Icon name="file" size={17} /> Trắc nghiệm</span></div></div>
        <div className="countdown-block"><small>BẮT ĐẦU SAU</small><div><span><strong>00</strong><small>GIỜ</small></span><b>:</b><span><strong>24</strong><small>PHÚT</small></span><b>:</b><span><strong>16</strong><small>GIÂY</small></span></div><Link className="button button-light" to="/exam/session">Vào phòng thi <Icon name="arrow-right" size={17} /></Link></div>
      </section>

      <section className="student-grid">
        <div className="student-main-column">
          <article className="panel" id="schedule">
            <div className="panel-heading"><div><h3>Lịch thi sắp tới</h3></div></div>
            <div className="student-exam-list">
              <div className="student-exam-item"><div className="compact-date"><strong>07</strong><small>THG 11</small></div><div><strong>Mạng máy tính</strong><span>CSE302 · 08:00 – 09:00</span></div><span className="location-pill"><Icon name="home" size={14} /> Phòng B105</span><span className="tag tag-success">Sẵn sàng</span></div>
              <div className="student-exam-item"><div className="compact-date purple"><strong>10</strong><small>THG 11</small></div><div><strong>Lập trình hướng đối tượng</strong><span>CSE201 · 14:00 – 15:30</span></div><span className="location-pill"><Icon name="home" size={14} /> Trực tuyến</span><span className="tag tag-indigo">Đã lên lịch</span></div>
              <div className="student-exam-item"><div className="compact-date orange"><strong>14</strong><small>THG 11</small></div><div><strong>Hệ điều hành</strong><span>CSE303 · 09:30 – 10:30</span></div><span className="location-pill"><Icon name="home" size={14} /> Phòng A302</span><span className="tag tag-indigo">Đã lên lịch</span></div>
            </div>
          </article>

          <article className="panel result-panel" id="results"><div className="panel-heading"><div><h3>Kết quả gần đây</h3></div></div><div className="recent-results"><div><span className="score-circle high">8.8</span><p><strong>Cấu trúc dữ liệu & giải thuật</strong><small>04/11/2026 · 35/40 câu đúng</small></p><span className="grade-badge">Giỏi</span></div><div><span className="score-circle">7.5</span><p><strong>Nhập môn trí tuệ nhân tạo</strong><small>30/10/2026 · 30/40 câu đúng</small></p><span className="grade-badge good">Khá</span></div></div></article>
        </div>

        <aside className="student-side-column">
          <article className="panel semester-card"><span className="eyebrow">TỔNG QUAN HỌC KỲ</span><div className="average-score"><span><strong>8.2</strong><small>Điểm trung bình</small></span><div className="score-ring">82%</div></div><div className="semester-stats"><div><strong>06</strong><span>Đã hoàn thành</span></div><div><strong>03</strong><span>Sắp diễn ra</span></div></div></article>
          <article className="panel notice-card"><div className="notice-icon"><Icon name="clock" /></div><div><h3>Trước giờ thi</h3><p>Kiểm tra mạng và có mặt trước 10 phút.</p></div></article>
        </aside>
      </section>
    </div>
  );
}
