import { Link, Outlet } from "react-router-dom";

import { Icon } from "../components/Icon";

export default function StudentLayout() {
  return (
    <div className="student-shell">
      <header className="student-header">
        <Link className="brand-lockup dark" to="/candidate">
          <span className="brand-mark"><Icon name="book" size={22} /></span>
          <span><strong>Campus</strong>Exam</span>
        </Link>
        <span className="student-area-label">Cổng thí sinh</span>
        <div className="student-profile">
          <span className="avatar avatar-teal">ML</span>
          <span><strong>Minh Long</strong><small>SV20230082</small></span>
        </div>
      </header>
      <main className="student-content"><Outlet /></main>
    </div>
  );
}
