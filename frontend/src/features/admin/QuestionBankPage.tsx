import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";

import { ConfirmDialog } from "../../components/common/ConfirmDialog";
import { Icon } from "../../components/Icon";
import { ApiError } from "../../services/httpClient";
import type { CauHoiChiTiet, CauHoiTomTat, KqhtDto, MaMucDo, ThongKeCauHoi, TrangDuLieu, TrangThaiSuDung } from "../../types/api";
import { kqhtApi } from "../learning-outcomes/api";
import { cauHoiApi } from "../question-bank/api";
import { HinhCauHoi } from "../question-bank/HinhCauHoi";
import { TEN_VI_TRI } from "../question-bank/importView";
import {
  DAP_AN,
  GIOI_HAN,
  MUC_DO,
  dinhDangNgay,
  formTuChiTiet,
  kiemTraForm,
  loiTuServer,
  noiDungGui,
  phuongAnKey,
  taoFormTrong,
  yeuCauTao,
  type QuestionFormErrors,
  type QuestionFormState,
} from "../question-bank/questionForm";

const KICH_THUOC_TRANG = 20;

const statusLabels: Record<TrangThaiSuDung, string> = { dang_su_dung: "Đang dùng", ngung_su_dung: "Ngừng dùng" };
const levelClass: Record<MaMucDo, string> = { 1: "level-easy", 2: "level-medium", 3: "level-hard" };

function thongBaoLoi(error: unknown) {
  return error instanceof ApiError ? error.message : "Không thể kết nối máy chủ. Vui lòng thử lại.";
}

function laHuy(error: unknown) {
  return error instanceof DOMException && error.name === "AbortError";
}

type Editor =
  | { mode: "create" }
  | { mode: "edit"; maCH: number; chiTiet: CauHoiChiTiet | null };

export default function QuestionBankPage() {
  const [kqhtList, setKqhtList] = useState<KqhtDto[]>([]);
  const [thongKe, setThongKe] = useState<ThongKeCauHoi | null>(null);
  const [query, setQuery] = useState("");
  const [tuKhoa, setTuKhoa] = useState("");
  const [maKQHT, setMaKQHT] = useState<number | "">("");
  const [maMD, setMaMD] = useState<MaMucDo | "">("");
  const [trangThai, setTrangThai] = useState<TrangThaiSuDung | "">("");
  const [sapXep, setSapXep] = useState("-ngayCapNhat");
  const [trang, setTrang] = useState(1);
  const [data, setData] = useState<TrangDuLieu<CauHoiTomTat> | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [success, setSuccess] = useState<string | null>(null);

  const [editor, setEditor] = useState<Editor | null>(null);
  const [form, setForm] = useState<QuestionFormState>(() => taoFormTrong());
  const [formErrors, setFormErrors] = useState<QuestionFormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [confirm, setConfirm] = useState<{ kind: "toggle" | "delete"; item: CauHoiTomTat } | null>(null);
  const [confirmBusy, setConfirmBusy] = useState(false);
  const [confirmError, setConfirmError] = useState<string | null>(null);

  const lamMoi = useCallback(() => setReloadKey((value) => value + 1), []);

  // Gõ tìm kiếm: chờ người dùng ngừng gõ 300 ms mới gọi API.
  useEffect(() => {
    const timer = window.setTimeout(() => { setTuKhoa(query.trim()); setTrang(1); }, 300);
    return () => window.clearTimeout(timer);
  }, [query]);

  useEffect(() => {
    const controller = new AbortController();
    kqhtApi.danhSach(controller.signal).then(setKqhtList).catch(() => undefined);
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    cauHoiApi.thongKe(controller.signal).then(setThongKe).catch(() => undefined);
    return () => controller.abort();
  }, [reloadKey]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setLoadError(null);
    cauHoiApi.danhSach({
      tuKhoa,
      maKQHT: maKQHT === "" ? undefined : maKQHT,
      maMD: maMD === "" ? undefined : maMD,
      trangThai: trangThai === "" ? undefined : trangThai,
      sapXep,
      trang,
      kichThuocTrang: KICH_THUOC_TRANG,
    }, controller.signal)
      .then((result) => {
        // Trang hiện tại trống sau khi xóa câu cuối: lùi về trang cuối còn dữ liệu.
        if (result.duLieu.length === 0 && trang > 1 && result.tongSoTrang > 0) setTrang(result.tongSoTrang);
        else setData(result);
      })
      .catch((error: unknown) => { if (!laHuy(error)) setLoadError(thongBaoLoi(error)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [tuKhoa, maKQHT, maMD, trangThai, sapXep, trang, reloadKey]);

  useEffect(() => {
    if (!editor) return undefined;
    const handleKeyDown = (event: KeyboardEvent) => { if (event.key === "Escape" && !submitting) setEditor(null); };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [editor, submitting]);

  const kqhtDangDung = kqhtList.filter((item) => item.trangThai === "dang_su_dung");

  function doiLoc<T>(setter: (value: T) => void) {
    return (value: T) => { setter(value); setTrang(1); };
  }

  function moThem() {
    setForm(taoFormTrong(maKQHT !== "" && kqhtDangDung.some((item) => item.maKQHT === maKQHT) ? maKQHT : ""));
    setFormErrors({});
    setSubmitError(null);
    setEditor({ mode: "create" });
  }

  async function moSua(maCH: number) {
    setFormErrors({});
    setSubmitError(null);
    setEditor({ mode: "edit", maCH, chiTiet: null });
    try {
      const chiTiet = await cauHoiApi.chiTiet(maCH);
      setForm(formTuChiTiet(chiTiet));
      setEditor({ mode: "edit", maCH, chiTiet });
    } catch (error) {
      setSubmitError(thongBaoLoi(error));
    }
  }

  function capNhatForm<K extends keyof QuestionFormState>(key: K, value: QuestionFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
    setFormErrors((current) => ({ ...current, [key]: undefined }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!editor) return;
    const errors = kiemTraForm(form, editor.mode === "create");
    setFormErrors(errors);
    setSubmitError(null);
    if (Object.keys(errors).length > 0) return;

    setSubmitting(true);
    try {
      if (editor.mode === "create") {
        const created = await cauHoiApi.tao(yeuCauTao(form));
        setSuccess(`Đã thêm câu hỏi ${created.maCauHoi}.`);
      } else if (editor.chiTiet) {
        const updated = await cauHoiApi.sua(editor.maCH, { ...noiDungGui(form), phienBan: editor.chiTiet.phienBan });
        setSuccess(`Đã cập nhật câu hỏi ${updated.maCauHoi}.`);
      }
      setEditor(null);
      lamMoi();
    } catch (error) {
      if (error instanceof ApiError && error.code === "CONCURRENCY_CONFLICT") {
        setSubmitError("Câu hỏi vừa được sửa ở nơi khác. Đóng hộp thoại và mở lại để xem bản mới nhất.");
      } else if (error instanceof ApiError && Object.keys(error.errors).length > 0) {
        setFormErrors(loiTuServer(error.errors));
        setSubmitError(error.errors.phienBan?.join(" ") ?? null);
      } else {
        setSubmitError(thongBaoLoi(error));
      }
    } finally {
      setSubmitting(false);
    }
  }

  async function handleConfirm() {
    if (!confirm) return;
    const { kind, item } = confirm;
    setConfirmBusy(true);
    setConfirmError(null);
    try {
      if (kind === "delete") {
        await cauHoiApi.xoa(item.maCH);
        setSuccess(`Đã xóa câu hỏi ${item.maCauHoi}.`);
      } else {
        const moi: TrangThaiSuDung = item.trangThai === "dang_su_dung" ? "ngung_su_dung" : "dang_su_dung";
        await cauHoiApi.doiTrangThai(item.maCH, moi);
        setSuccess(`Câu hỏi ${item.maCauHoi}: ${statusLabels[moi].toLocaleLowerCase("vi")}.`);
      }
      setConfirm(null);
      lamMoi();
    } catch (error) {
      setConfirmError(error instanceof ApiError && error.code === "QUESTION_IN_USE"
        ? "Câu hỏi đã nằm trong đề thi nên không xóa được. Hãy chuyển sang ngừng sử dụng."
        : thongBaoLoi(error));
    } finally {
      setConfirmBusy(false);
    }
  }

  const tyLe = (value: number) => (thongKe && thongKe.tongSo > 0 ? `${((value / thongKe.tongSo) * 100).toFixed(1)}%` : "—");
  const editingDetail = editor?.mode === "edit" ? editor.chiTiet : null;
  const editorLoading = editor?.mode === "edit" && !editor.chiTiet && !submitError;

  return (
    <div className="page-stack">
      <section className="page-heading-row">
        <div><h2>Ngân hàng câu hỏi</h2></div>
        <div className="heading-actions">
          <Link className="button button-secondary" to="/admin/questions/import"><Icon name="file" size={17} /> Nhập từ Word</Link>
          <button className="button button-primary" disabled={kqhtDangDung.length === 0} onClick={moThem} title={kqhtDangDung.length === 0 ? "Cần ít nhất một KQHT đang sử dụng" : undefined} type="button">
            <Icon name="plus" size={17} /> Thêm câu hỏi
          </button>
        </div>
      </section>

      {success && (
        <div className="success-banner" role="status">
          <span><Icon name="check" size={17} /> {success}</span>
          <button aria-label="Đóng thông báo" onClick={() => setSuccess(null)} type="button"><Icon name="x" size={16} /></button>
        </div>
      )}

      <section className="mini-stats-grid">
        <div><span className="mini-stat-icon indigo"><Icon name="book" /></span><p><small>TỔNG CÂU HỎI</small><strong>{thongKe ? thongKe.tongSo.toLocaleString("vi-VN") : "—"}</strong></p></div>
        <div><span className="mini-stat-icon teal"><Icon name="check" /></span><p><small>ĐANG SỬ DỤNG</small><strong>{thongKe ? thongKe.dangSuDung.toLocaleString("vi-VN") : "—"}</strong></p><em>{thongKe ? tyLe(thongKe.dangSuDung) : ""}</em></div>
        <div><span className="mini-stat-icon orange"><Icon name="lock" /></span><p><small>NGỪNG SỬ DỤNG</small><strong>{thongKe ? thongKe.ngungSuDung.toLocaleString("vi-VN") : "—"}</strong></p><em>{thongKe ? tyLe(thongKe.ngungSuDung) : ""}</em></div>
        <div><span className="mini-stat-icon purple"><Icon name="layers" /></span><p><small>KQHT ĐANG DÙNG</small><strong>{thongKe ? thongKe.soKQHT.toString().padStart(2, "0") : "—"}</strong></p></div>
      </section>

      <section className="panel table-panel">
        <div className="table-toolbar">
          <div className="search-box"><Icon name="search" size={18} /><input aria-label="Tìm câu hỏi" maxLength={200} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm mã hoặc nội dung" value={query} /></div>
          <select aria-label="Kết quả học tập" onChange={(event) => doiLoc(setMaKQHT)(event.target.value === "" ? "" : Number(event.target.value))} value={maKQHT}>
            <option value="">Tất cả KQHT</option>
            {kqhtList.map((item) => <option key={item.maKQHT} value={item.maKQHT}>{item.kyHieu} — KQHT {item.thuTu}</option>)}
          </select>
          <select aria-label="Mức độ" onChange={(event) => doiLoc(setMaMD)(event.target.value === "" ? "" : (Number(event.target.value) as MaMucDo))} value={maMD}>
            <option value="">Tất cả mức độ</option>
            {([1, 2, 3] as MaMucDo[]).map((md) => <option key={md} value={md}>{MUC_DO[md]}</option>)}
          </select>
          <select aria-label="Trạng thái" onChange={(event) => doiLoc(setTrangThai)(event.target.value as TrangThaiSuDung | "")} value={trangThai}>
            <option value="">Tất cả trạng thái</option>
            <option value="dang_su_dung">Đang dùng</option>
            <option value="ngung_su_dung">Ngừng dùng</option>
          </select>
          <select aria-label="Sắp xếp" onChange={(event) => doiLoc(setSapXep)(event.target.value)} value={sapXep}>
            <option value="-ngayCapNhat">Mới cập nhật</option>
            <option value="maCauHoi">Mã câu hỏi</option>
            <option value="-ngayTao">Mới thêm</option>
          </select>
        </div>

        {loadError ? (
          <div className="empty-state error-state"><Icon name="alert" size={28} /><strong>Chưa tải được câu hỏi</strong><span>{loadError}</span><button className="button button-secondary" onClick={lamMoi} type="button">Thử lại</button></div>
        ) : (
          <>
            <div className="table-scroll" aria-busy={loading}>
              <table className="data-table question-table">
                <thead><tr><th>MÃ CÂU HỎI</th><th>NỘI DUNG CÂU HỎI</th><th>KQHT</th><th>MỨC ĐỘ</th><th>TRẠNG THÁI</th><th>CẬP NHẬT</th><th><span className="sr-only">THAO TÁC</span></th></tr></thead>
                <tbody>
                  {(data?.duLieu ?? []).map((item) => (
                    <tr className={item.trangThai === "ngung_su_dung" ? "question-row-muted" : undefined} key={item.maCH}>
                      <td><strong className="question-code">{item.maCauHoi}</strong></td>
                      <td><div className="question-cell"><strong>{item.tomTat}</strong><small>Trắc nghiệm · 4 phương án{item.coHinhAnh && <> · <span className="has-image">Có hình</span></>}</small></div></td>
                      <td><span className="outcome-badge">KQHT {item.kqht.thuTu}</span></td>
                      <td><span className={`level-badge ${levelClass[item.maMD]}`}>{MUC_DO[item.maMD]}</span></td>
                      <td><span className={`status-dot ${item.trangThai === "dang_su_dung" ? "status-active" : "status-disabled"}`}>{statusLabels[item.trangThai]}</span></td>
                      <td><span className="muted-cell">{dinhDangNgay(item.ngayCapNhat)}</span></td>
                      <td>
                        <div className="row-actions">
                          <button aria-label={`Sửa ${item.maCauHoi}`} className="row-action" onClick={() => void moSua(item.maCH)} title="Sửa" type="button"><Icon name="edit" size={16} /></button>
                          <button aria-label={item.trangThai === "dang_su_dung" ? `Ngừng dùng ${item.maCauHoi}` : `Khôi phục ${item.maCauHoi}`} className="row-action" onClick={() => { setConfirmError(null); setConfirm({ kind: "toggle", item }); }} title={item.trangThai === "dang_su_dung" ? "Ngừng sử dụng" : "Khôi phục"} type="button"><Icon name={item.trangThai === "dang_su_dung" ? "lock" : "check"} size={16} /></button>
                          <button aria-label={`Xóa ${item.maCauHoi}`} className="row-action danger" onClick={() => { setConfirmError(null); setConfirm({ kind: "delete", item }); }} title="Xóa" type="button"><Icon name="x" size={16} /></button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {!loading && data?.duLieu.length === 0 && (
              <div className="empty-state"><Icon name="search" size={28} />
                {data.tongSoDong === 0 && !tuKhoa && maKQHT === "" && maMD === "" && trangThai === ""
                  ? <><strong>Chưa có câu hỏi</strong><span>Bấm “Thêm câu hỏi” để tạo câu đầu tiên.</span></>
                  : <><strong>Không tìm thấy câu hỏi</strong><span>Thử thay đổi từ khóa hoặc bộ lọc.</span></>}
              </div>
            )}
            <div className="table-footer">
              <span>{loading && !data ? "Đang tải…" : <><strong>{data?.tongSoDong.toLocaleString("vi-VN") ?? 0}</strong> câu hỏi</>}</span>
              <div className="pagination">
                <button aria-label="Trang trước" disabled={loading || trang <= 1} onClick={() => setTrang((value) => value - 1)} type="button"><Icon name="arrow-left" size={15} /></button>
                <span>Trang {data && data.tongSoTrang > 0 ? data.trang : 0} / {data?.tongSoTrang ?? 0}</span>
                <button aria-label="Trang sau" disabled={loading || !data || trang >= data.tongSoTrang} onClick={() => setTrang((value) => value + 1)} type="button"><Icon name="arrow-right" size={15} /></button>
              </div>
            </div>
          </>
        )}
      </section>

      {editor && (
        <div className="dialog-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !submitting) setEditor(null); }}>
          <section aria-labelledby="question-dialog-title" aria-modal="true" className="form-dialog question-dialog" role="dialog">
            <header>
              <div>
                <span className="dialog-icon"><Icon name="book" size={20} /></span>
                <div><small>NGÂN HÀNG CÂU HỎI</small><h3 id="question-dialog-title">{editor.mode === "create" ? "Thêm câu hỏi" : `Sửa câu hỏi ${editingDetail?.maCauHoi ?? ""}`}</h3></div>
              </div>
              <button aria-label="Đóng" className="dialog-close" disabled={submitting} onClick={() => setEditor(null)} type="button"><Icon name="x" size={19} /></button>
            </header>
            <form noValidate onSubmit={handleSubmit}>
              <div className="dialog-body">
                {submitError && <div className="form-alert" role="alert"><Icon name="alert" size={16} /><span>{submitError}</span></div>}
                {editorLoading ? (
                  <div className="outcomes-loading" aria-label="Đang tải câu hỏi">{[1, 2, 3].map((item) => <span key={item} />)}</div>
                ) : (editor.mode === "create" || editingDetail) && (
                  <>
                    {editingDetail && editingDetail.soDeDaDung > 0 && (
                      <div className="form-warning"><Icon name="alert" size={16} /><span>Câu hỏi đã nằm trong {editingDetail.soDeDaDung} đề. Đề đã sinh giữ bản sao cũ; nội dung mới chỉ áp dụng cho đề sinh sau.</span></div>
                    )}
                    <div className="dialog-field-grid">
                      <label className="dialog-field"><span>Kết quả học tập <em>*</em></span>
                        <select disabled={editor.mode === "edit"} onChange={(event) => capNhatForm("maKQHT", event.target.value === "" ? "" : Number(event.target.value))} value={form.maKQHT}>
                          <option value="">Chọn KQHT</option>
                          {(editor.mode === "create" ? kqhtDangDung : kqhtList).map((item) => <option key={item.maKQHT} value={item.maKQHT}>{item.kyHieu} — {item.tenKQHT}</option>)}
                        </select>
                        {formErrors.maKQHT && <small className="field-error">{formErrors.maKQHT}</small>}
                      </label>
                      <label className="dialog-field"><span>Mức độ <em>*</em></span>
                        <select disabled={editor.mode === "edit"} onChange={(event) => capNhatForm("maMD", Number(event.target.value) as MaMucDo)} value={form.maMD}>
                          {([1, 2, 3] as MaMucDo[]).map((md) => <option key={md} value={md}>{md} — {MUC_DO[md]}</option>)}
                        </select>
                        {formErrors.maMD && <small className="field-error">{formErrors.maMD}</small>}
                      </label>
                    </div>
                    {editor.mode === "edit"
                      ? <small className="field-help">KQHT và mức độ quyết định mã câu hỏi nên không đổi được. Muốn phân loại lại, hãy thêm câu mới và ngừng dùng câu này.</small>
                      : <small className="field-help">Mã câu hỏi được cấp tự động theo KQHT và mức độ, ví dụ A1-1.</small>}
                    <label className="dialog-field full" style={{ marginTop: 14 }}><span>Nội dung câu hỏi <em>*</em></span>
                      <textarea autoFocus maxLength={GIOI_HAN.noiDung} onChange={(event) => capNhatForm("noiDung", event.target.value)} rows={4} value={form.noiDung} />
                      {formErrors.noiDung && <small className="field-error">{formErrors.noiDung}</small>}
                      <small className="field-counter">{form.noiDung.length}/{GIOI_HAN.noiDung}</small>
                    </label>
                    <div className="answer-grid" role="radiogroup" aria-label="Đáp án đúng">
                      {DAP_AN.map((dapAn) => {
                        const key = phuongAnKey(dapAn);
                        return (
                          <div className={`dialog-field answer-field${form.dapAnDung === dapAn ? " is-correct" : ""}`} key={dapAn}>
                            <span>
                              <label htmlFor={`phuong-an-${dapAn}`}>Phương án {dapAn} <em>*</em></label>
                              <label className="correct-toggle"><input checked={form.dapAnDung === dapAn} name="dapAnDung" onChange={() => capNhatForm("dapAnDung", dapAn)} type="radio" /> Đáp án đúng</label>
                            </span>
                            <textarea id={`phuong-an-${dapAn}`} maxLength={GIOI_HAN.phuongAn} onChange={(event) => capNhatForm(key, event.target.value)} rows={2} value={form[key]} />
                            {formErrors[key] && <small className="field-error">{formErrors[key]}</small>}
                          </div>
                        );
                      })}
                    </div>
                    {formErrors.dapAnDung && <small className="field-error">{formErrors.dapAnDung}</small>}
                    {editingDetail && editingDetail.hinhAnh.length > 0 && (
                      <div className="question-images">
                        <span>Hình trong câu hỏi</span>
                        <small className="field-help">Mã như <code>{"{{media:NOIDUNG:1}}"}</code> trong nội dung đánh dấu vị trí hình; giữ nguyên mã khi sửa để hình hiển thị đúng chỗ.</small>
                        <div className="question-image-grid">
                          {editingDetail.hinhAnh.map((hinh) => (
                            <figure key={hinh.maTN}>
                              <HinhCauHoi alt={hinh.altText ?? `${TEN_VI_TRI[hinh.viTri]} – hình ${hinh.thuTu}`} url={hinh.url} />
                              <figcaption><code>{`{{media:${hinh.viTri}:${hinh.thuTu}}}`}</code></figcaption>
                            </figure>
                          ))}
                        </div>
                      </div>
                    )}
                    <label className="checkbox-field"><input checked={form.coDinhDapAn} onChange={(event) => capNhatForm("coDinhDapAn", event.target.checked)} type="checkbox" /><span>Giữ nguyên thứ tự phương án khi sinh đề (dùng cho phương án kiểu “Cả A và B đều đúng”).</span></label>
                  </>
                )}
              </div>
              <footer>
                <button className="button button-secondary" disabled={submitting} onClick={() => setEditor(null)} type="button">Hủy</button>
                <button className="button button-primary" disabled={submitting || editorLoading || (editor.mode === "edit" && !editingDetail)} type="submit">
                  {submitting ? "Đang lưu…" : editor.mode === "create" ? <><Icon name="plus" size={16} /> Thêm câu hỏi</> : <><Icon name="check" size={16} /> Lưu thay đổi</>}
                </button>
              </footer>
            </form>
          </section>
        </div>
      )}

      {confirm && (
        <ConfirmDialog
          busy={confirmBusy}
          confirmLabel={confirm.kind === "delete" ? "Xóa câu hỏi" : confirm.item.trangThai === "dang_su_dung" ? "Ngừng sử dụng" : "Khôi phục"}
          danger={confirm.kind === "delete"}
          error={confirmError}
          onCancel={() => setConfirm(null)}
          onConfirm={handleConfirm}
          title={confirm.kind === "delete" ? `Xóa câu hỏi ${confirm.item.maCauHoi}?` : confirm.item.trangThai === "dang_su_dung" ? `Ngừng dùng ${confirm.item.maCauHoi}?` : `Khôi phục ${confirm.item.maCauHoi}?`}
        >
          {confirm.kind === "delete"
            ? <p>Xóa vĩnh viễn câu hỏi này. Chỉ xóa được câu chưa nằm trong đề thi nào; thao tác không hoàn tác được.</p>
            : confirm.item.trangThai === "dang_su_dung"
              ? <p>Câu hỏi sẽ không được chọn khi sinh đề mới. Các đề đã sinh không bị ảnh hưởng.</p>
              : <p>Câu hỏi sẽ được dùng lại khi sinh đề mới.</p>}
        </ConfirmDialog>
      )}
    </div>
  );
}
