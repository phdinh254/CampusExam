import { useMemo, useState } from "react";

import { Icon } from "../../components/Icon";

const exams = [
  { title: "Cơ sở dữ liệu nâng cao", code: "CSDL-2026-01", date: "06/11/2026", time: "09:00 – 10:00", candidates: "120", progress: 76, status: "Đang diễn ra", tone: "success" },
  { title: "Lập trình hướng đối tượng", code: "OOP-2026-03", date: "06/11/2026", time: "14:00 – 15:30", candidates: "86", progress: 100, status: "Sắp diễn ra", tone: "indigo" },
  { title: "Mạng máy tính", code: "NET-2026-02", date: "07/11/2026", time: "08:00 – 09:00", candidates: "64", progress: 92, status: "Đã phát hành", tone: "purple" },
  { title: "Cấu trúc dữ liệu & giải thuật", code: "DSA-2026-05", date: "04/11/2026", time: "13:30 – 15:00", candidates: "84", progress: 98, status: "Đã kết thúc", tone: "neutral" },
];

export default function ExamsPage() {
  const [query, setQuery] = useState("");
  const visibleExams = useMemo(() => exams.filter((exam) =>
    `${exam.title} ${exam.code}`.toLowerCase().includes(query.toLowerCase()),
  ), [query]);

  return <div className="page-stack">
    <section className="page-heading-row"><div><h2>Quản lý kỳ thi</h2></div></section>
    <section className="exam-overview"><div className="exam-overview-main"><span className="eyebrow">ĐANG DIỄN RA</span><h3>Cơ sở dữ liệu nâng cao</h3><p>CSDL-2026-01 · Phòng A203</p><div className="live-stats"><div><strong>91</strong><span>Đang làm</span></div><div><strong>18</strong><span>Đã nộp</span></div><div><strong>11</strong><span>Chưa vào</span></div></div></div><div className="live-clock"><span>CÒN LẠI</span><strong>00:34:28</strong><small><i /> Đang hoạt động</small></div></section>
    <section className="panel table-panel"><div className="panel-heading exam-table-heading"><div><h3>Danh sách kỳ thi</h3></div><div className="heading-actions"><div className="search-box small"><Icon name="search" size={17} /><input aria-label="Tìm kỳ thi" onChange={(event) => setQuery(event.target.value)} placeholder="Tìm tên hoặc mã kỳ thi" value={query} /></div></div></div><div className="table-scroll"><table className="data-table exam-table"><thead><tr><th>KỲ THI</th><th>THỜI GIAN</th><th>THÍ SINH</th><th>HOÀN THÀNH</th><th>TRẠNG THÁI</th></tr></thead><tbody>{visibleExams.map((exam) => <tr key={exam.code}><td><div className="exam-name-cell"><span className={`exam-subject-icon ${exam.tone}`}><Icon name="file" size={19} /></span><div><strong>{exam.title}</strong><small>{exam.code}</small></div></div></td><td><div className="date-cell"><strong>{exam.date}</strong><small>{exam.time}</small></div></td><td><span className="candidate-count"><Icon name="users" size={16} /> {exam.candidates}</span></td><td><div className="progress-cell"><span><i style={{ width: `${exam.progress}%` }} /></span><strong>{exam.progress}%</strong></div></td><td><span className={`exam-status ${exam.tone}`}>{exam.status}</span></td></tr>)}</tbody></table></div>{visibleExams.length === 0 && <div className="empty-state"><Icon name="search" size={28} /><strong>Không tìm thấy kỳ thi</strong></div>}<div className="table-footer"><span><strong>{visibleExams.length}</strong> kỳ thi đang hiển thị</span><span>Trang 1 / 5</span></div></section>
  </div>;
}
