import { Link, Outlet } from "react-router-dom";

import { Icon } from "../components/Icon";

export default function ExamLayout() {
  return (
    <div className="exam-shell">
      <header className="exam-header">
        <Link className="brand-lockup dark" to="/candidate">
          <span className="brand-mark"><Icon name="book" size={21} /></span>
          <span><strong>Campus</strong>Exam</span>
        </Link>
        <div className="exam-header-title"><span>BÀI THI ĐANG DIỄN RA</span><strong>Cơ sở dữ liệu nâng cao</strong></div>
        <div className="exam-candidate"><span className="avatar avatar-teal">ML</span><span><strong>Nguyễn Minh Long</strong><small>SV20230082</small></span></div>
      </header>
      <main className="exam-content"><Outlet /></main>
    </div>
  );
}
