import { useCallback, useEffect, useState, type FormEvent } from "react";

import { ConfirmDialog } from "../../components/common/ConfirmDialog";
import { Icon } from "../../components/Icon";
import { ApiError } from "../../services/httpClient";
import type { GioiTinh, ThiSinhChiTiet, ThiSinhTomTat, TrangDuLieu, TrangThaiTaiKhoan } from "../../types/api";
import { thiSinhApi } from "../students/api";
import {
  GIOI_TINH,
  chuVietTat,
  dinhDangNgaySinh,
  formTuChiTiet,
  homNay,
  hoSoGui,
  kiemTraForm,
  loiTuServer,
  taoFormTrong,
  yeuCauTao,
  type StudentFormErrors,
  type StudentFormState,
} from "../students/studentForm";

const KICH_THUOC_TRANG = 20;
const TONES = ["teal", "indigo", "orange", "purple"] as const;

const statusLabels: Record<TrangThaiTaiKhoan, string> = { hoat_dong: "Hoạt động", khoa: "Đã khóa" };

function thongBaoLoi(error: unknown) {
  return error instanceof ApiError ? error.message : "Không thể kết nối máy chủ. Vui lòng thử lại.";
}

function laHuy(error: unknown) {
  return error instanceof DOMException && error.name === "AbortError";
}

/** Màu ảnh đại diện ổn định theo mã số. */
function tone(msts: string) {
  let tong = 0;
  for (const kyTu of msts) tong += kyTu.charCodeAt(0);
  return TONES[tong % TONES.length];
}

type Editor = { mode: "create" } | { mode: "edit"; msts: string; chiTiet: ThiSinhChiTiet | null };
type ConfirmKind = "toggle" | "reset" | "delete";

export default function StudentsPage() {
  const [query, setQuery] = useState("");
  const [tuKhoa, setTuKhoa] = useState("");
  const [lop, setLop] = useState("");
  const [trangThai, setTrangThai] = useState<TrangThaiTaiKhoan | "">("");
  const [chuaDoiMatKhau, setChuaDoiMatKhau] = useState(false);
  const [trang, setTrang] = useState(1);
  const [danhSachLop, setDanhSachLop] = useState<string[]>([]);
  const [data, setData] = useState<TrangDuLieu<ThiSinhTomTat> | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [success, setSuccess] = useState<string | null>(null);

  const [editor, setEditor] = useState<Editor | null>(null);
  const [form, setForm] = useState<StudentFormState>(taoFormTrong);
  const [formErrors, setFormErrors] = useState<StudentFormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [confirm, setConfirm] = useState<{ kind: ConfirmKind; item: ThiSinhTomTat } | null>(null);
  const [confirmBusy, setConfirmBusy] = useState(false);
  const [confirmError, setConfirmError] = useState<string | null>(null);

  const lamMoi = useCallback(() => setReloadKey((value) => value + 1), []);

  useEffect(() => {
    const timer = window.setTimeout(() => { setTuKhoa(query.trim()); setTrang(1); }, 300);
    return () => window.clearTimeout(timer);
  }, [query]);

  useEffect(() => {
    const controller = new AbortController();
    thiSinhApi.lop(controller.signal).then(setDanhSachLop).catch(() => undefined);
    return () => controller.abort();
  }, [reloadKey]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setLoadError(null);
    thiSinhApi.danhSach({
      tuKhoa,
      lop: lop || undefined,
      trangThai: trangThai || undefined,
      daDoiMatKhau: chuaDoiMatKhau ? false : undefined,
      trang,
      kichThuocTrang: KICH_THUOC_TRANG,
    }, controller.signal)
      .then((result) => {
        if (result.duLieu.length === 0 && trang > 1 && result.tongSoTrang > 0) setTrang(result.tongSoTrang);
        else setData(result);
      })
      .catch((error: unknown) => { if (!laHuy(error)) setLoadError(thongBaoLoi(error)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [tuKhoa, lop, trangThai, chuaDoiMatKhau, trang, reloadKey]);

  useEffect(() => {
    if (!editor) return undefined;
    const handleKeyDown = (event: KeyboardEvent) => { if (event.key === "Escape" && !submitting) setEditor(null); };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [editor, submitting]);

  function doiLoc<T>(setter: (value: T) => void) {
    return (value: T) => { setter(value); setTrang(1); };
  }

  function moThem() {
    setForm(taoFormTrong());
    setFormErrors({});
    setSubmitError(null);
    setEditor({ mode: "create" });
  }

  async function moSua(msts: string) {
    setFormErrors({});
    setSubmitError(null);
    setEditor({ mode: "edit", msts, chiTiet: null });
    try {
      const chiTiet = await thiSinhApi.chiTiet(msts);
      setForm(formTuChiTiet(chiTiet));
      setEditor({ mode: "edit", msts, chiTiet });
    } catch (error) {
      setSubmitError(thongBaoLoi(error));
    }
  }

  function capNhatForm<K extends keyof StudentFormState>(key: K, value: StudentFormState[K]) {
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
        const created = await thiSinhApi.tao(yeuCauTao(form));
        setSuccess(`Đã thêm ${created.hoTen} (${created.msts}). Mật khẩu ban đầu là ngày sinh dạng ddMMyyyy.`);
      } else {
        const updated = await thiSinhApi.sua(editor.msts, hoSoGui(form));
        setSuccess(`Đã cập nhật hồ sơ ${updated.hoTen} (${updated.msts}).`);
      }
      setEditor(null);
      lamMoi();
    } catch (error) {
      if (error instanceof ApiError && error.code === "STUDENT_MSTS_CONFLICT") {
        setFormErrors({ msts: "Mã số này đã được dùng cho thí sinh hoặc tài khoản khác." });
      } else if (error instanceof ApiError && error.code === "STUDENT_CCCD_CONFLICT") {
        setFormErrors({ cccd: "Số CCCD này đã gắn với thí sinh khác." });
      } else if (error instanceof ApiError && Object.keys(error.errors).length > 0) {
        setFormErrors(loiTuServer(error.errors));
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
        await thiSinhApi.xoa(item.msts);
        setSuccess(`Đã xóa thí sinh ${item.hoTen} (${item.msts}).`);
      } else if (kind === "reset") {
        await thiSinhApi.capLaiMatKhau(item.msts);
        setSuccess(`Đã đặt lại mật khẩu của ${item.msts} về ngày sinh; thí sinh phải đổi khi đăng nhập.`);
      } else {
        const moi: TrangThaiTaiKhoan = item.trangThaiTaiKhoan === "hoat_dong" ? "khoa" : "hoat_dong";
        await thiSinhApi.doiTrangThaiTaiKhoan(item.msts, moi);
        setSuccess(moi === "khoa" ? `Đã khóa tài khoản ${item.msts}.` : `Đã mở khóa tài khoản ${item.msts}.`);
      }
      setConfirm(null);
      lamMoi();
    } catch (error) {
      setConfirmError(error instanceof ApiError && error.code === "STUDENT_IN_USE"
        ? "Thí sinh đã được phân công thi nên không xóa được. Hãy khóa tài khoản thay vì xóa."
        : thongBaoLoi(error));
    } finally {
      setConfirmBusy(false);
    }
  }

  const editingDetail = editor?.mode === "edit" ? editor.chiTiet : null;
  const editorLoading = editor?.mode === "edit" && !editor.chiTiet && !submitError;
  const coBoLoc = Boolean(tuKhoa || lop || trangThai || chuaDoiMatKhau);

  const confirmText: Record<ConfirmKind, (item: ThiSinhTomTat) => { title: string; label: string; body: string }> = {
    toggle: (item) => item.trangThaiTaiKhoan === "hoat_dong"
      ? { title: `Khóa tài khoản ${item.msts}?`, label: "Khóa tài khoản", body: "Thí sinh bị đăng xuất khỏi mọi thiết bị ngay và không đăng nhập được cho tới khi mở khóa." }
      : { title: `Mở khóa ${item.msts}?`, label: "Mở khóa", body: "Thí sinh đăng nhập lại được bằng mật khẩu hiện tại." },
    reset: (item) => ({ title: `Cấp lại mật khẩu cho ${item.msts}?`, label: "Cấp lại mật khẩu", body: `Mật khẩu được đặt lại về ngày sinh dạng ddMMyyyy, mọi phiên đăng nhập bị thu hồi và thí sinh phải đổi mật khẩu ở lần đăng nhập tới. Hãy xác minh danh tính thí sinh trước khi cấp lại.` }),
    delete: (item) => ({ title: `Xóa thí sinh ${item.msts}?`, label: "Xóa thí sinh", body: `Xóa vĩnh viễn hồ sơ và tài khoản của ${item.hoTen}. Chỉ xóa được thí sinh chưa được phân công thi; thao tác không hoàn tác được.` }),
  };
  const confirmContent = confirm ? confirmText[confirm.kind](confirm.item) : null;

  return (
    <div className="page-stack">
      <section className="page-heading-row">
        <div><h2>Quản lý thí sinh</h2></div>
        <div className="heading-actions">
          <button className="button button-primary" onClick={moThem} type="button"><Icon name="plus" size={17} /> Thêm thí sinh</button>
        </div>
      </section>

      {success && (
        <div className="success-banner" role="status">
          <span><Icon name="check" size={17} /> {success}</span>
          <button aria-label="Đóng thông báo" onClick={() => setSuccess(null)} type="button"><Icon name="x" size={16} /></button>
        </div>
      )}

      <section className="panel table-panel">
        <div className="table-toolbar">
          <div className="search-box"><Icon name="search" size={18} /><input aria-label="Tìm thí sinh" maxLength={100} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm tên hoặc mã số thí sinh" value={query} /></div>
          <select aria-label="Lớp" onChange={(event) => doiLoc(setLop)(event.target.value)} value={lop}>
            <option value="">Tất cả lớp</option>
            {danhSachLop.map((item) => <option key={item} value={item}>{item}</option>)}
          </select>
          <select aria-label="Trạng thái tài khoản" onChange={(event) => doiLoc(setTrangThai)(event.target.value as TrangThaiTaiKhoan | "")} value={trangThai}>
            <option value="">Tất cả trạng thái</option>
            <option value="hoat_dong">Hoạt động</option>
            <option value="khoa">Đã khóa</option>
          </select>
          <label className="checkbox-field toolbar-check"><input checked={chuaDoiMatKhau} onChange={(event) => doiLoc(setChuaDoiMatKhau)(event.target.checked)} type="checkbox" /><span>Chưa đổi mật khẩu mặc định</span></label>
        </div>

        {loadError ? (
          <div className="empty-state error-state"><Icon name="alert" size={28} /><strong>Chưa tải được thí sinh</strong><span>{loadError}</span><button className="button button-secondary" onClick={lamMoi} type="button">Thử lại</button></div>
        ) : (
          <>
            <div className="table-scroll" aria-busy={loading}>
              <table className="data-table student-table">
                <thead><tr><th>THÍ SINH</th><th>MÃ SỐ</th><th>LỚP</th><th>LƯỢT THI</th><th>TRẠNG THÁI</th><th><span className="sr-only">THAO TÁC</span></th></tr></thead>
                <tbody>
                  {(data?.duLieu ?? []).map((item) => (
                    <tr key={item.msts}>
                      <td>
                        <div className="student-name-cell">
                          <span className={`avatar avatar-${tone(item.msts)}`}>{chuVietTat(item.hoTen)}</span>
                          <div><strong>{item.hoTen}</strong><small>Sinh ngày {dinhDangNgaySinh(item.ngaySinh)}{item.gioiTinh && ` · ${GIOI_TINH[item.gioiTinh]}`}</small></div>
                        </div>
                      </td>
                      <td><strong className="question-code">{item.msts}</strong></td>
                      <td>{item.lop ?? <span className="muted-cell">—</span>}</td>
                      <td>{item.soLuotThi}</td>
                      <td>
                        <span className={`status-dot ${item.trangThaiTaiKhoan === "hoat_dong" ? "status-active" : "status-disabled"}`}>{statusLabels[item.trangThaiTaiKhoan]}</span>
                        {!item.daDoiMatKhau && <small className="password-hint">Chưa đổi mật khẩu</small>}
                      </td>
                      <td>
                        <div className="row-actions">
                          <button aria-label={`Sửa ${item.msts}`} className="row-action" onClick={() => void moSua(item.msts)} title="Sửa hồ sơ" type="button"><Icon name="edit" size={16} /></button>
                          <button aria-label={`Cấp lại mật khẩu ${item.msts}`} className="row-action" onClick={() => { setConfirmError(null); setConfirm({ kind: "reset", item }); }} title="Cấp lại mật khẩu" type="button"><Icon name="sparkles" size={16} /></button>
                          <button aria-label={item.trangThaiTaiKhoan === "hoat_dong" ? `Khóa ${item.msts}` : `Mở khóa ${item.msts}`} className="row-action" onClick={() => { setConfirmError(null); setConfirm({ kind: "toggle", item }); }} title={item.trangThaiTaiKhoan === "hoat_dong" ? "Khóa tài khoản" : "Mở khóa"} type="button"><Icon name={item.trangThaiTaiKhoan === "hoat_dong" ? "lock" : "check"} size={16} /></button>
                          <button aria-label={`Xóa ${item.msts}`} className="row-action danger" onClick={() => { setConfirmError(null); setConfirm({ kind: "delete", item }); }} title="Xóa" type="button"><Icon name="x" size={16} /></button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {!loading && data?.duLieu.length === 0 && (
              <div className="empty-state"><Icon name={coBoLoc ? "search" : "users"} size={28} />
                {coBoLoc
                  ? <><strong>Không tìm thấy thí sinh</strong><span>Thử thay đổi từ khóa hoặc bộ lọc.</span></>
                  : <><strong>Chưa có thí sinh</strong><span>Bấm “Thêm thí sinh” để tạo hồ sơ và tài khoản đầu tiên.</span></>}
              </div>
            )}
            <div className="table-footer">
              <span>{loading && !data ? "Đang tải…" : <><strong>{data?.tongSoDong.toLocaleString("vi-VN") ?? 0}</strong> thí sinh</>}</span>
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
          <section aria-labelledby="student-dialog-title" aria-modal="true" className="form-dialog" role="dialog">
            <header>
              <div>
                <span className="dialog-icon"><Icon name="users" size={20} /></span>
                <div><small>HỒ SƠ THÍ SINH</small><h3 id="student-dialog-title">{editor.mode === "create" ? "Thêm thí sinh" : `Sửa hồ sơ ${editor.msts}`}</h3></div>
              </div>
              <button aria-label="Đóng" className="dialog-close" disabled={submitting} onClick={() => setEditor(null)} type="button"><Icon name="x" size={19} /></button>
            </header>
            <form noValidate onSubmit={handleSubmit}>
              <div className="dialog-body">
                {submitError && <div className="form-alert" role="alert"><Icon name="alert" size={16} /><span>{submitError}</span></div>}
                {editorLoading ? (
                  <div className="outcomes-loading" aria-label="Đang tải hồ sơ">{[1, 2, 3].map((item) => <span key={item} />)}</div>
                ) : (editor.mode === "create" || editingDetail) && (
                  <>
                    <div className="dialog-field-grid">
                      <label className="dialog-field"><span>Mã số thí sinh <em>*</em></span>
                        <input autoFocus={editor.mode === "create"} disabled={editor.mode === "edit"} maxLength={20} onChange={(event) => capNhatForm("msts", event.target.value)} placeholder="Ví dụ: SV20230082" value={form.msts} />
                        {formErrors.msts && <small className="field-error">{formErrors.msts}</small>}
                        <small className="field-help">{editor.mode === "create" ? "Dùng làm tên đăng nhập; không đổi được sau khi tạo." : "Mã số là tên đăng nhập nên không đổi được."}</small>
                      </label>
                      <label className="dialog-field"><span>Ngày sinh <em>*</em></span>
                        <input max={homNay()} min="1900-01-01" onChange={(event) => capNhatForm("ngaySinh", event.target.value)} type="date" value={form.ngaySinh} />
                        {formErrors.ngaySinh && <small className="field-error">{formErrors.ngaySinh}</small>}
                        {editor.mode === "create"
                          ? <small className="field-help">Mật khẩu ban đầu là ngày sinh dạng ddMMyyyy.</small>
                          : <small className="field-help">Đổi ngày sinh không đổi mật khẩu hiện tại.</small>}
                      </label>
                    </div>
                    <label className="dialog-field full"><span>Họ tên <em>*</em></span>
                      <input autoFocus={editor.mode === "edit"} maxLength={100} onChange={(event) => capNhatForm("hoTen", event.target.value)} value={form.hoTen} />
                      {formErrors.hoTen && <small className="field-error">{formErrors.hoTen}</small>}
                    </label>
                    <div className="dialog-field-grid">
                      <label className="dialog-field"><span>Giới tính</span>
                        <select onChange={(event) => capNhatForm("gioiTinh", event.target.value as GioiTinh | "")} value={form.gioiTinh}>
                          <option value="">Không khai báo</option>
                          <option value="nu">Nữ</option>
                          <option value="nam">Nam</option>
                        </select>
                      </label>
                      <label className="dialog-field"><span>Lớp</span>
                        <input list="student-class-list" maxLength={50} onChange={(event) => capNhatForm("lop", event.target.value)} value={form.lop} />
                        <datalist id="student-class-list">{danhSachLop.map((item) => <option key={item} value={item} />)}</datalist>
                        {formErrors.lop && <small className="field-error">{formErrors.lop}</small>}
                      </label>
                    </div>
                    <div className="dialog-field-grid">
                      <label className="dialog-field"><span>Nơi sinh</span>
                        <input maxLength={200} onChange={(event) => capNhatForm("noiSinh", event.target.value)} value={form.noiSinh} />
                        {formErrors.noiSinh && <small className="field-error">{formErrors.noiSinh}</small>}
                      </label>
                      <label className="dialog-field"><span>Số CCCD</span>
                        <input inputMode="numeric" maxLength={12} onChange={(event) => capNhatForm("cccd", event.target.value)} value={form.cccd} />
                        {formErrors.cccd && <small className="field-error">{formErrors.cccd}</small>}
                      </label>
                    </div>
                  </>
                )}
              </div>
              <footer>
                <button className="button button-secondary" disabled={submitting} onClick={() => setEditor(null)} type="button">Hủy</button>
                <button className="button button-primary" disabled={submitting || editorLoading || (editor.mode === "edit" && !editingDetail)} type="submit">
                  {submitting ? "Đang lưu…" : editor.mode === "create" ? <><Icon name="plus" size={16} /> Thêm thí sinh</> : <><Icon name="check" size={16} /> Lưu thay đổi</>}
                </button>
              </footer>
            </form>
          </section>
        </div>
      )}

      {confirm && confirmContent && (
        <ConfirmDialog
          busy={confirmBusy}
          confirmLabel={confirmContent.label}
          danger={confirm.kind === "delete" || (confirm.kind === "toggle" && confirm.item.trangThaiTaiKhoan === "hoat_dong")}
          error={confirmError}
          onCancel={() => setConfirm(null)}
          onConfirm={handleConfirm}
          title={confirmContent.title}
        >
          <p>{confirmContent.body}</p>
        </ConfirmDialog>
      )}
    </div>
  );
}
