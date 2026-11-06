import { Link } from "react-router-dom";

import { Icon, type IconName } from "../../components/Icon";
import { useTaiKhoan } from "../auth/AuthProvider";
import { tenGoi } from "../auth/authUtils";

const stats: { label: string; value: string; delta: string; icon: IconName; tone: string }[] = [
  { label: "Tổng câu hỏi", value: "2,480", delta: "+124 tháng này", icon: "book", tone: "indigo" },
  { label: "Kỳ thi đang mở", value: "08", delta: "3 kỳ thi hôm nay", icon: "calendar", tone: "teal" },
  { label: "Thí sinh", value: "1,284", delta: "+48 thí sinh mới", icon: "users", tone: "orange" },
  { label: "Lượt thi hoàn tất", value: "92.6%", delta: "+2.4% so với tháng trước", icon: "activity", tone: "purple" },
];

const activities = [
  { initials: "TH", tone: "indigo", title: "Trần Minh Hoàng đã tạo kỳ thi mới", meta: "Lập trình hướng đối tượng · 12 phút trước" },
  { initials: "NT", tone: "teal", title: "Đã nhập 45 câu hỏi từ tệp Word", meta: "Ngân hàng Cơ sở dữ liệu · 35 phút trước" },
  { initials: "HT", tone: "orange", title: "Kỳ thi Cấu trúc dữ liệu đã kết thúc", meta: "82/84 thí sinh hoàn thành · 1 giờ trước" },
  { initials: "LA", tone: "purple", title: "Lê Anh đã cập nhật ma trận đề thi", meta: "Mạng máy tính · 2 giờ trước" },
];

export default function AdminDashboard() {
  const taiKhoan = useTaiKhoan();

  return (
    <div className="page-stack">
      <section className="welcome-row">
        <div><h2>Chào {tenGoi(taiKhoan.hoTen)}!</h2></div>
        <div className="date-chip"><Icon name="calendar" size={18} /><span><small>HÔM NAY</small><strong>06 tháng 11, 2026</strong></span></div>
      </section>

      <section className="stats-grid">
        {stats.map((stat) => <article className="stat-card" key={stat.label}><div className={`stat-icon ${stat.tone}`}><Icon name={stat.icon} size={22} /></div><div className="stat-card-main"><span>{stat.label}</span><strong>{stat.value}</strong><small><span className="positive">↗</span> {stat.delta}</small></div></article>)}
      </section>

      <section className="dashboard-grid">
        <article className="panel performance-panel">
          <div className="panel-heading"><div><h3>Hiệu suất kỳ thi</h3><p>7 ngày qua</p></div></div>
          <div className="chart-summary"><strong>1,842</strong><span>lượt dự thi</span><small>+12.5%</small></div>
          <div className="bar-chart" aria-label="Biểu đồ lượt thi trong tuần">
            {[64, 82, 56, 93, 72, 88, 70].map((height, index) => <div className="bar-column" key={index}><div className={`bar ${index === 3 ? "peak" : ""}`} style={{ height: `${height}%` }}>{index === 3 && <span>328</span>}</div><small>{["T2", "T3", "T4", "T5", "T6", "T7", "CN"][index]}</small></div>)}
          </div>
        </article>

        <article className="panel upcoming-panel">
          <div className="panel-heading"><div><h3>Kỳ thi sắp diễn ra</h3></div><Link to="/admin/exams">Xem tất cả <Icon name="arrow-right" size={15} /></Link></div>
          <div className="exam-list">
            <div className="exam-list-item"><div className="date-tile"><strong>06</strong><small>THG 11</small></div><div className="exam-list-copy"><strong>Cơ sở dữ liệu nâng cao</strong><span><Icon name="clock" size={14} /> 09:00 – 10:00 · Phòng A203</span><div><span className="tag tag-indigo">120 thí sinh</span><span className="tag tag-success">Sẵn sàng</span></div></div></div>
            <div className="exam-list-item"><div className="date-tile teal"><strong>06</strong><small>THG 11</small></div><div className="exam-list-copy"><strong>Lập trình hướng đối tượng</strong><span><Icon name="clock" size={14} /> 14:00 – 15:30 · Trực tuyến</span><div><span className="tag tag-indigo">86 thí sinh</span><span className="tag tag-warning">Cần kiểm tra</span></div></div></div>
            <div className="exam-list-item"><div className="date-tile orange"><strong>07</strong><small>THG 11</small></div><div className="exam-list-copy"><strong>Mạng máy tính</strong><span><Icon name="clock" size={14} /> 08:00 – 09:00 · Phòng B105</span><div><span className="tag tag-indigo">64 thí sinh</span><span className="tag tag-success">Sẵn sàng</span></div></div></div>
          </div>
        </article>
      </section>

      <article className="panel activity-panel"><div className="panel-heading"><div><h3>Hoạt động gần đây</h3></div></div><div className="activity-list">{activities.map((item) => <div className="activity-item" key={item.title}><span className={`avatar avatar-${item.tone}`}>{item.initials}</span><div><strong>{item.title}</strong><small>{item.meta}</small></div></div>)}</div></article>
    </div>
  );
}
