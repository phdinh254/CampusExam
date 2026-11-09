import { Icon, type IconName } from "../../components/Icon";

type TrangChoProps = { kicker: string; title: string; icon: IconName; moTa: string };

/**
 * Khung trang Thi trực tuyến. Dữ liệu (`GET /api/toi/ky-thi`, `GET /api/toi/ket-qua`,
 * API_DESIGN.md mục 2.11) có sau khi làm phần kỳ thi, phân công và bài làm; trang không
 * hiển thị dữ liệu giả.
 */
function TrangThiTrucTuyen({ kicker, title, icon, moTa }: TrangChoProps) {
  return (
    <div className="student-dashboard">
      <section className="student-welcome">
        <div><span className="eyebrow">THI TRỰC TUYẾN · {kicker}</span><h1>{title}</h1></div>
      </section>
      <section className="panel candidate-pending">
        <Icon name={icon} size={30} />
        <strong>Chức năng đang được phát triển</strong>
        <span>{moTa}</span>
      </section>
    </div>
  );
}

export function JoinExamPage() {
  return (
    <TrangThiTrucTuyen
      icon="file"
      kicker="THAM GIA THI"
      moTa="Tại đây sẽ hiện các kỳ thi bạn được phân công, giờ mở phòng thi và nút vào thi khi đến giờ."
      title="Tham gia thi"
    />
  );
}

export function ExamResultsPage() {
  return (
    <TrangThiTrucTuyen
      icon="check"
      kicker="KẾT QUẢ THI"
      moTa="Tại đây sẽ hiện điểm và số câu đúng của các kỳ thi bạn đã làm, nếu kỳ thi cho phép hiển thị điểm."
      title="Kết quả thi"
    />
  );
}
