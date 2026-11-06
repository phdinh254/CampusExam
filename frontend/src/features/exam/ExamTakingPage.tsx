import { useState } from "react";
import { useNavigate } from "react-router-dom";

import { Icon } from "../../components/Icon";

const questions = [
  { id: 1, text: "Trong mô hình cơ sở dữ liệu quan hệ, phát biểu nào sau đây mô tả đúng nhất về khóa chính (Primary Key)?", answers: ["Một thuộc tính bất kỳ có thể chứa giá trị trùng lặp", "Một hoặc nhiều thuộc tính xác định duy nhất mỗi bản ghi trong bảng", "Thuộc tính chỉ được sử dụng để liên kết với bảng khác", "Một thuộc tính luôn có kiểu dữ liệu là số nguyên"] },
  { id: 2, text: "Mệnh đề SQL nào được sử dụng để lọc các nhóm sau khi thực hiện GROUP BY?", answers: ["WHERE", "ORDER BY", "HAVING", "DISTINCT"] },
  { id: 3, text: "Dạng chuẩn thứ ba (3NF) yêu cầu điều kiện nào sau đây?", answers: ["Không có thuộc tính đa trị", "Mọi thuộc tính không khóa phụ thuộc đầy đủ vào khóa", "Không có phụ thuộc bắc cầu của thuộc tính không khóa vào khóa", "Mỗi bảng chỉ có một khóa ứng viên"] },
  { id: 4, text: "Transaction trong hệ quản trị cơ sở dữ liệu tuân theo nhóm thuộc tính nào?", answers: ["CRUD", "ACID", "BASE", "CAP"] },
  { id: 5, text: "Chỉ mục (Index) trong cơ sở dữ liệu chủ yếu được dùng để làm gì?", answers: ["Giảm kích thước bảng", "Tăng tốc độ truy vấn dữ liệu", "Mã hóa dữ liệu", "Ngăn chặn mọi cập nhật"] },
];

export default function ExamTakingPage() {
  const navigate = useNavigate();
  const [current, setCurrent] = useState(1);
  const [answers, setAnswers] = useState<Record<number, number>>({ 2: 2, 3: 2 });
  const [flagged, setFlagged] = useState<number[]>([3]);
  const [showSubmit, setShowSubmit] = useState(false);
  const question = questions[(current - 1) % questions.length];

  function toggleFlag() {
    setFlagged((items) => items.includes(current) ? items.filter((id) => id !== current) : [...items, current]);
  }

  return (
    <div className="exam-workspace">
      <section className="exam-question-area">
        <div className="exam-progress-row"><div><span>TIẾN ĐỘ LÀM BÀI</span><strong>{Object.keys(answers).length}/40 câu</strong></div><div className="exam-progress-track"><i style={{ width: `${(Object.keys(answers).length / 40) * 100}%` }} /></div><span>Đã tự động lưu <Icon name="check" size={14} /></span></div>
        <article className="question-card">
          <header><div><span className="question-number">CÂU {current}</span><span className="question-level">MỨC ĐỘ: {current % 3 === 0 ? "KHÓ" : current % 2 === 0 ? "TRUNG BÌNH" : "DỄ"}</span></div><button className={flagged.includes(current) ? "flagged" : ""} onClick={toggleFlag} type="button"><Icon name="flag" size={18} /> {flagged.includes(current) ? "Đã đánh dấu" : "Đánh dấu câu"}</button></header>
          <div className="question-content"><h1>{question.text}</h1><p>Chọn một phương án đúng nhất.</p><div className="answer-list">{question.answers.map((answer, index) => <label className={answers[current] === index ? "selected" : ""} key={answer}><input checked={answers[current] === index} name={`question-${current}`} onChange={() => setAnswers((items) => ({ ...items, [current]: index }))} type="radio" /><span className="answer-letter">{String.fromCharCode(65 + index)}</span><strong>{answer}</strong><span className="radio-mark"><Icon name="check" size={14} /></span></label>)}</div></div>
          <footer><button className="button button-secondary" disabled={current === 1} onClick={() => setCurrent((value) => Math.max(1, value - 1))}><Icon name="arrow-left" size={17} /> Câu trước</button><button className="button button-primary" onClick={() => setCurrent((value) => Math.min(40, value + 1))}>Câu tiếp theo <Icon name="arrow-right" size={17} /></button></footer>
        </article>
      </section>

      <aside className="exam-sidebar">
        <div className="timer-card"><span><Icon name="clock" size={17} /> THỜI GIAN CÒN LẠI</span><strong>34:28</strong><small>Kết thúc lúc 10:00</small></div>
        <div className="question-map"><div className="question-map-heading"><div><h2>Danh sách câu hỏi</h2><p>Chọn số câu để di chuyển nhanh</p></div><span>{Object.keys(answers).length}/40</span></div><div className="question-legend"><span><i className="answered" /> Đã trả lời</span><span><i className="flag-legend" /> Đánh dấu</span><span><i /> Chưa làm</span></div><div className="question-grid">{Array.from({ length: 40 }, (_, index) => index + 1).map((number) => <button className={`${current === number ? "current" : ""} ${answers[number] !== undefined ? "answered" : ""} ${flagged.includes(number) ? "flagged" : ""}`} key={number} onClick={() => setCurrent(number)}>{number}{flagged.includes(number) && <Icon name="flag" size={9} />}</button>)}</div></div>
        <div className="exam-save-note"><Icon name="check" size={16} /><span><strong>Bài làm đã được lưu</strong><small>Cập nhật vài giây trước</small></span></div>
        <button className="button submit-exam-button" onClick={() => setShowSubmit(true)}><Icon name="file" size={17} /> Nộp bài thi</button>
      </aside>

      {showSubmit && <div className="modal-backdrop" role="presentation"><div className="confirm-modal" role="dialog" aria-modal="true" aria-labelledby="submit-title"><span className="modal-icon"><Icon name="file" size={26} /></span><h2 id="submit-title">Xác nhận nộp bài?</h2><p>Đã trả lời <strong>{Object.keys(answers).length}/40 câu</strong>. Không thể chỉnh sửa sau khi nộp.</p><div><button className="button button-secondary" onClick={() => setShowSubmit(false)}>Tiếp tục làm</button><button className="button button-primary" onClick={() => navigate("/candidate")}>Nộp bài</button></div></div></div>}
    </div>
  );
}
