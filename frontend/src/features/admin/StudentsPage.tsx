import { useMemo, useState } from "react";

import { Icon } from "../../components/Icon";

const students = [
  { initials: "ML", name: "Nguyễn Minh Long", code: "SV20230082", program: "K29 - Công nghệ phần mềm", exams: 8, status: "Hoạt động", tone: "teal" },
  { initials: "TH", name: "Trần Gia Hân", code: "SV20230114", program: "K29 - Hệ thống thông tin", exams: 7, status: "Hoạt động", tone: "indigo" },
  { initials: "QP", name: "Lê Quốc Phong", code: "SV20220138", program: "K28 - Khoa học máy tính", exams: 12, status: "Hoạt động", tone: "orange" },
  { initials: "AN", name: "Phạm An Nhiên", code: "SV20230206", program: "K29 - An toàn thông tin", exams: 5, status: "Tạm khóa", tone: "purple" },
  { initials: "DK", name: "Đỗ Đăng Khoa", code: "SV20220091", program: "K28 - Công nghệ phần mềm", exams: 11, status: "Hoạt động", tone: "teal" },
];

function getEmail(name: string) {
  return `${name.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replaceAll(" ", ".")}@duytan.edu.vn`;
}

export default function StudentsPage() {
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("Tất cả trạng thái");
  const [cohort, setCohort] = useState("Tất cả khóa");

  const visibleStudents = useMemo(() => students.filter((student) => {
    const matchesQuery = `${student.name} ${student.code}`.toLowerCase().includes(query.toLowerCase());
    const matchesStatus = status === "Tất cả trạng thái" || student.status === status;
    const matchesCohort = cohort === "Tất cả khóa" || student.program.startsWith(cohort);
    return matchesQuery && matchesStatus && matchesCohort;
  }), [cohort, query, status]);

  return (
    <div className="page-stack">
      <section className="page-heading-row"><div><h2>Quản lý thí sinh</h2></div></section>
      <section className="panel table-panel">
        <div className="table-toolbar">
          <div className="search-box"><Icon name="search" size={18} /><input aria-label="Tìm thí sinh" onChange={(event) => setQuery(event.target.value)} placeholder="Tìm tên hoặc mã thí sinh" value={query} /></div>
          <select aria-label="Khóa học" onChange={(event) => setCohort(event.target.value)} value={cohort}><option>Tất cả khóa</option><option>K28</option><option>K29</option></select>
          <select aria-label="Trạng thái" onChange={(event) => setStatus(event.target.value)} value={status}><option>Tất cả trạng thái</option><option>Hoạt động</option><option>Tạm khóa</option></select>
        </div>
        <div className="table-scroll">
          <table className="data-table student-table">
            <thead><tr><th>THÍ SINH</th><th>MÃ THÍ SINH</th><th>LỚP / CHUYÊN NGÀNH</th><th>LƯỢT THI</th><th>TRẠNG THÁI</th></tr></thead>
            <tbody>{visibleStudents.map((student) => <tr key={student.code}><td><div className="student-name-cell"><span className={`avatar avatar-${student.tone}`}>{student.initials}</span><div><strong>{student.name}</strong><small>{getEmail(student.name)}</small></div></div></td><td><strong className="question-code">{student.code}</strong></td><td>{student.program}</td><td>{student.exams} kỳ thi</td><td><span className={`status-dot ${student.status === "Hoạt động" ? "status-active" : "status-disabled"}`}>{student.status}</span></td></tr>)}</tbody>
          </table>
        </div>
        {visibleStudents.length === 0 && <div className="empty-state"><Icon name="search" size={28} /><strong>Không tìm thấy thí sinh</strong></div>}
        <div className="table-footer"><span><strong>{visibleStudents.length}</strong> thí sinh đang hiển thị</span><span>Trang 1 / 257</span></div>
      </section>
    </div>
  );
}
