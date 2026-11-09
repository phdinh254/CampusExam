import { useEffect, useRef, useState, type ChangeEvent } from "react";
import { Link } from "react-router-dom";

import { Icon } from "../../components/Icon";
import { ApiError } from "../../services/httpClient";
import type { LanImport, TrangDuLieu, XemTruocNhapCauHoi } from "../../types/api";
import { KICH_THUOC_TEP_TOI_DA, nhapCauHoiApi } from "./importApi";
import { dinhDangDungLuong, dinhDangGio, locDong, soCanhBao, tomTatNoiDung } from "./importView";

const trangThaiLanImport: Record<LanImport["trangThai"], string> = {
  dang_xu_ly: "Đang xử lý",
  hoan_tat: "Hoàn tất",
  that_bai: "Thất bại",
  hoan_tat_co_loi: "Hoàn tất có lỗi",
};

function thongBaoLoi(error: unknown) {
  if (error instanceof ApiError) {
    switch (error.code) {
      case "IMPORT_PREVIEW_EXPIRED": return "Bản xem trước đã quá 30 phút. Hãy chọn lại tệp.";
      case "IMPORT_PREVIEW_NOT_FOUND": return "Bản xem trước không còn (đã xác nhận hoặc hết hạn). Hãy chọn lại tệp.";
      default: return error.problem?.detail ?? error.message;
    }
  }
  return "Không thể kết nối máy chủ. Vui lòng thử lại.";
}

export default function QuestionImportPage() {
  const inputRef = useRef<HTMLInputElement>(null);
  const [tep, setTep] = useState<File | null>(null);
  const [xemTruoc, setXemTruoc] = useState<XemTruocNhapCauHoi | null>(null);
  const [dangDoc, setDangDoc] = useState(false);
  const [dangGhi, setDangGhi] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);
  const [thanhCong, setThanhCong] = useState<LanImport | null>(null);
  const [chiCoVanDe, setChiCoVanDe] = useState(false);
  const [lichSu, setLichSu] = useState<TrangDuLieu<LanImport> | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    nhapCauHoiApi.lichSu({ trang: 1, kichThuocTrang: 10 }, controller.signal).then(setLichSu).catch(() => undefined);
    return () => controller.abort();
  }, [reloadKey]);

  async function chonTep(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    setLoi(null);
    setThanhCong(null);
    setXemTruoc(null);
    setChiCoVanDe(false);
    if (!file.name.toLowerCase().endsWith(".docx")) {
      setLoi("Chỉ hỗ trợ tệp .docx. Tệp .doc cũ cần mở bằng Word và lưu lại dạng .docx.");
      return;
    }
    if (file.size > KICH_THUOC_TEP_TOI_DA) {
      setLoi(`Tệp ${dinhDangDungLuong(file.size)} vượt giới hạn 20 MB.`);
      return;
    }

    setTep(file);
    setDangDoc(true);
    try {
      const ketQua = await nhapCauHoiApi.xemTruoc(file);
      setXemTruoc(ketQua);
      setChiCoVanDe(ketQua.soDongLoi > 0);
    } catch (error) {
      setTep(null);
      setLoi(thongBaoLoi(error));
    } finally {
      setDangDoc(false);
    }
  }

  async function xacNhan() {
    if (!xemTruoc?.maXemTruoc) return;
    setDangGhi(true);
    setLoi(null);
    try {
      const lan = await nhapCauHoiApi.xacNhan(xemTruoc.maXemTruoc);
      setThanhCong(lan);
      setXemTruoc(null);
      setTep(null);
      setReloadKey((value) => value + 1);
    } catch (error) {
      setLoi(thongBaoLoi(error));
      // Bản xem trước không dùng lại được sau các lỗi này; phải tải tệp lên lại.
      if (error instanceof ApiError && ["IMPORT_PREVIEW_EXPIRED", "IMPORT_PREVIEW_NOT_FOUND", "QUESTION_IMPORT_HAS_ERRORS"].includes(error.code)) {
        setXemTruoc((current) => (current ? { ...current, maXemTruoc: null } : current));
      }
    } finally {
      setDangGhi(false);
    }
  }

  const dongHienThi = xemTruoc ? locDong(xemTruoc.dong, chiCoVanDe) : [];
  const canhBao = xemTruoc ? soCanhBao(xemTruoc.dong) : 0;

  return (
    <div className="page-stack">
      <section className="page-heading-row">
        <div>
          <span className="page-kicker"><Link to="/admin/questions">NGÂN HÀNG CÂU HỎI</Link></span>
          <h2>Nhập câu hỏi từ Word</h2>
          <p>Tệp .docx theo mẫu bảng IU03: mã câu hỏi, nội dung kèm 4 phương án, đáp án đúng.</p>
        </div>
        <div className="heading-actions">
          <input accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document" className="sr-only" onChange={(event) => void chonTep(event)} ref={inputRef} tabIndex={-1} type="file" />
          <button className="button button-primary" disabled={dangDoc || dangGhi} onClick={() => inputRef.current?.click()} type="button">
            <Icon name="file" size={17} /> {dangDoc ? "Đang đọc tệp…" : xemTruoc ? "Chọn tệp khác" : "Chọn tệp .docx"}
          </button>
        </div>
      </section>

      {loi && <div className="form-alert" role="alert"><Icon name="alert" size={16} /><span>{loi}</span></div>}

      {thanhCong && (
        <div className="success-banner" role="status">
          <span><Icon name="check" size={17} /> Đã nhập {thanhCong.soThanhCong} câu hỏi từ {thanhCong.tenFile}. <Link to="/admin/questions">Xem ngân hàng câu hỏi</Link></span>
          <button aria-label="Đóng thông báo" onClick={() => setThanhCong(null)} type="button"><Icon name="x" size={16} /></button>
        </div>
      )}

      {!xemTruoc && !dangDoc && (
        <section className="panel import-empty">
          <Icon name="file" size={30} />
          <strong>Chưa chọn tệp</strong>
          <span>Hệ thống đọc tệp và hiển thị từng dòng để bạn kiểm tra. Chỉ khi mọi dòng hợp lệ mới ghi vào ngân hàng câu hỏi, và ghi tất cả cùng lúc.</span>
        </section>
      )}

      {xemTruoc && (
        <>
          <section className="mini-stats-grid">
            <div><span className="mini-stat-icon indigo"><Icon name="file" /></span><p><small>TỔNG DÒNG</small><strong>{xemTruoc.tongDong}</strong></p><em>{tep ? dinhDangDungLuong(tep.size) : ""}</em></div>
            <div><span className="mini-stat-icon teal"><Icon name="check" /></span><p><small>HỢP LỆ</small><strong>{xemTruoc.soDongHopLe}</strong></p></div>
            <div><span className="mini-stat-icon orange"><Icon name="alert" /></span><p><small>CÓ LỖI</small><strong>{xemTruoc.soDongLoi}</strong></p>{canhBao > 0 && <em>{canhBao} cảnh báo</em>}</div>
            <div><span className="mini-stat-icon purple"><Icon name="layers" /></span><p><small>HÌNH ẢNH</small><strong>{xemTruoc.tongHinhAnh}</strong></p></div>
          </section>

          <section className="panel import-confirm">
            {xemTruoc.maXemTruoc ? (
              <>
                <div>
                  <strong>Sẵn sàng nhập {xemTruoc.tongDong} câu hỏi{tep ? ` từ ${tep.name}` : ""}.</strong>
                  <span>Bản xem trước giữ tới {xemTruoc.hetHan ? dinhDangGio(xemTruoc.hetHan) : "—"}. Hệ thống kiểm tra lại trước khi ghi.</span>
                </div>
                <button className="button button-primary" disabled={dangGhi} onClick={() => void xacNhan()} type="button">
                  {dangGhi ? "Đang ghi…" : <><Icon name="check" size={16} /> Xác nhận nhập</>}
                </button>
              </>
            ) : (
              <div>
                <strong>{xemTruoc.tongDong === 0 ? "Tệp không có dòng câu hỏi nào." : "Chưa thể nhập."}</strong>
                <span>{xemTruoc.tongDong === 0 ? "Kiểm tra lại bảng câu hỏi trong tệp." : "Sửa các dòng lỗi trong tệp Word rồi chọn lại tệp. Không có dòng nào được ghi."}</span>
              </div>
            )}
          </section>

          <section className="panel table-panel">
            <div className="table-toolbar">
              <label className="checkbox-field toolbar-check"><input checked={chiCoVanDe} onChange={(event) => setChiCoVanDe(event.target.checked)} type="checkbox" /><span>Chỉ hiện dòng có lỗi hoặc cảnh báo</span></label>
            </div>
            <div className="table-scroll">
              <table className="data-table import-table">
                <thead><tr><th>DÒNG</th><th>MÃ</th><th>NỘI DUNG</th><th>ĐÁP ÁN</th><th>KIỂM TRA</th></tr></thead>
                <tbody>
                  {dongHienThi.map((dong) => (
                    <tr className={dong.hopLe ? undefined : "import-row-error"} key={dong.soDong}>
                      <td><span className="muted-cell">{dong.soDong}</span></td>
                      <td><strong className="question-code">{dong.maCauHoi ?? "—"}</strong></td>
                      <td><div className="question-cell"><strong>{tomTatNoiDung(dong.noiDung) || <span className="muted-cell">(trống)</span>}</strong>{dong.hinhAnh.length > 0 && <small>{dong.hinhAnh.length} hình</small>}</div></td>
                      <td>{dong.dapAnDung ?? "—"}</td>
                      <td>
                        {dong.vanDe.length === 0
                          ? <span className="status-dot status-active">Hợp lệ</span>
                          : <ul className="issue-list">{dong.vanDe.map((vanDe, index) => <li className={vanDe.mucDo === "loi" ? "issue-error" : "issue-warning"} key={`${vanDe.code}-${index}`}>{vanDe.noiDung}</li>)}</ul>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {dongHienThi.length === 0 && <div className="empty-state"><Icon name="check" size={28} /><strong>Không có dòng nào có vấn đề</strong></div>}
          </section>
        </>
      )}

      <section className="panel table-panel">
        <div className="table-toolbar"><strong>Lịch sử nhập</strong></div>
        <div className="table-scroll">
          <table className="data-table">
            <thead><tr><th>THỜI GIAN</th><th>TỆP</th><th>SỐ CÂU</th><th>NGƯỜI NHẬP</th><th>TRẠNG THÁI</th></tr></thead>
            <tbody>
              {(lichSu?.duLieu ?? []).map((lan) => (
                <tr key={lan.maLanImport}>
                  <td><span className="muted-cell">{dinhDangGio(lan.thoiGian)}</span></td>
                  <td>{lan.tenFile}</td>
                  <td>{lan.soThanhCong}/{lan.tongDong}</td>
                  <td>{lan.nguoiThucHien}</td>
                  <td><span className={`status-dot ${lan.trangThai === "hoan_tat" ? "status-active" : "status-disabled"}`}>{trangThaiLanImport[lan.trangThai]}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {lichSu?.duLieu.length === 0 && <div className="empty-state"><Icon name="clock" size={28} /><strong>Chưa có lần nhập nào</strong></div>}
      </section>
    </div>
  );
}
