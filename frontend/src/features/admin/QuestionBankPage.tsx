import { useMemo, useState } from "react";

import { Icon } from "../../components/Icon";

const questions = [
  { code: "A1-001", text: "Trong mô hình cơ sở dữ liệu quan hệ, khóa chính có đặc điểm nào sau đây?", outcome: "KQHT 1", level: "Dễ", status: "Đang dùng", updated: "06/11/2026" },
  { code: "A2-014", text: "Chuẩn hóa dữ liệu đến dạng chuẩn 3NF nhằm loại bỏ loại phụ thuộc nào?", outcome: "KQHT 1", level: "Trung bình", status: "Đang dùng", updated: "05/11/2026" },
  { code: "B3-008", text: "Cho lược đồ R(A,B,C,D). Hãy xác định tập khóa ứng viên phù hợp.", outcome: "KQHT 2", level: "Khó", status: "Đang dùng", updated: "04/11/2026" },
  { code: "C2-021", text: "Câu lệnh SQL nào được sử dụng để thay đổi cấu trúc của một bảng?", outcome: "KQHT 3", level: "Trung bình", status: "Bản nháp", updated: "03/11/2026" },
  { code: "B1-032", text: "Phép toán nào trong đại số quan hệ dùng để chọn các hàng thỏa điều kiện?", outcome: "KQHT 2", level: "Dễ", status: "Đang dùng", updated: "02/11/2026" },
  { code: "D3-004", text: "Phân tích hiện tượng deadlock trong một lịch biểu thực thi đồng thời.", outcome: "KQHT 4", level: "Khó", status: "Ngừng dùng", updated: "30/10/2026" },
];

export default function QuestionBankPage() {
  const [query, setQuery] = useState("");
  const [level, setLevel] = useState("Tất cả mức độ");
  const [outcome, setOutcome] = useState("Tất cả KQHT");
  const [status, setStatus] = useState("Tất cả trạng thái");
  const visibleQuestions = useMemo(() => questions.filter((question) => {
    const matchesQuery = `${question.code} ${question.text}`.toLowerCase().includes(query.toLowerCase());
    const matchesLevel = level === "Tất cả mức độ" || question.level === level;
    const matchesOutcome = outcome === "Tất cả KQHT" || question.outcome === outcome;
    const matchesStatus = status === "Tất cả trạng thái" || question.status === status;
    return matchesQuery && matchesLevel && matchesOutcome && matchesStatus;
  }), [query, level, outcome, status]);

  return (
    <div className="page-stack">
      <section className="page-heading-row"><div><h2>Ngân hàng câu hỏi</h2></div></section>
      <section className="mini-stats-grid"><div><span className="mini-stat-icon indigo"><Icon name="book" /></span><p><small>TỔNG CÂU HỎI</small><strong>2,480</strong></p><em>+124</em></div><div><span className="mini-stat-icon teal"><Icon name="check" /></span><p><small>ĐANG SỬ DỤNG</small><strong>2,284</strong></p><em>92.1%</em></div><div><span className="mini-stat-icon orange"><Icon name="edit" /></span><p><small>BẢN NHÁP</small><strong>148</strong></p><em>6.0%</em></div><div><span className="mini-stat-icon purple"><Icon name="layers" /></span><p><small>KẾT QUẢ HỌC TẬP</small><strong>08</strong></p><em>Đã thiết lập</em></div></section>
      <section className="panel table-panel">
        <div className="table-toolbar"><div className="search-box"><Icon name="search" size={18} /><input aria-label="Tìm câu hỏi" onChange={(event) => setQuery(event.target.value)} placeholder="Tìm mã hoặc nội dung" value={query} /></div><select aria-label="Kết quả học tập" onChange={(event) => setOutcome(event.target.value)} value={outcome}><option>Tất cả KQHT</option><option>KQHT 1</option><option>KQHT 2</option><option>KQHT 3</option><option>KQHT 4</option></select><select aria-label="Mức độ" onChange={(event) => setLevel(event.target.value)} value={level}><option>Tất cả mức độ</option><option>Dễ</option><option>Trung bình</option><option>Khó</option></select><select aria-label="Trạng thái" onChange={(event) => setStatus(event.target.value)} value={status}><option>Tất cả trạng thái</option><option>Đang dùng</option><option>Bản nháp</option><option>Ngừng dùng</option></select></div>
        <div className="table-scroll"><table className="data-table question-table"><thead><tr><th>MÃ CÂU HỎI</th><th>NỘI DUNG CÂU HỎI</th><th>KQHT</th><th>MỨC ĐỘ</th><th>TRẠNG THÁI</th><th>CẬP NHẬT</th></tr></thead><tbody>{visibleQuestions.map((question) => <tr key={question.code}><td><strong className="question-code">{question.code}</strong></td><td><div className="question-cell"><strong>{question.text}</strong><small>Trắc nghiệm · 4 phương án</small></div></td><td><span className="outcome-badge">{question.outcome}</span></td><td><span className={`level-badge level-${question.level === "Dễ" ? "easy" : question.level === "Khó" ? "hard" : "medium"}`}>{question.level}</span></td><td><span className={`status-dot status-${question.status === "Đang dùng" ? "active" : question.status === "Bản nháp" ? "draft" : "disabled"}`}>{question.status}</span></td><td><span className="muted-cell">{question.updated}</span></td></tr>)}</tbody></table></div>
        {visibleQuestions.length === 0 && <div className="empty-state"><Icon name="search" size={28} /><strong>Không tìm thấy câu hỏi</strong><span>Thử thay đổi từ khóa hoặc bộ lọc.</span></div>}
        <div className="table-footer"><span><strong>{visibleQuestions.length}</strong> kết quả đang hiển thị</span><span>Trang 1 / 124</span></div>
      </section>
    </div>
  );
}
