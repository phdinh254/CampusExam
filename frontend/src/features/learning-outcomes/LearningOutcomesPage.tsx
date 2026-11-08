import { useEffect, useMemo, useState, type FormEvent } from "react";

import { Icon } from "../../components/Icon";
import { ApiError } from "../../services/httpClient";
import type { KqhtDto, TaoKqhtRequest, TrangThaiSuDung } from "../../types/api";
import { kqhtApi } from "./api";

type FormState = {
  tenKQHT: string;
  thuTu: number;
  moTa: string;
  trangThai: TrangThaiSuDung;
  kyHieu: string;
};

type FormErrors = Partial<Record<keyof FormState, string>>;

const statusLabels: Record<TrangThaiSuDung, string> = {
  dang_su_dung: "Đang sử dụng",
  ngung_su_dung: "Ngừng sử dụng",
};

function kyHieuTheoThuTu(thuTu: number) {
  return thuTu >= 1 && thuTu <= 26 ? String.fromCharCode(64 + thuTu) : "";
}

function timThuTuTiepTheo(items: KqhtDto[]) {
  const daDung = new Set(items.map((item) => item.thuTu));
  for (let thuTu = 1; thuTu <= 26; thuTu += 1) {
    if (!daDung.has(thuTu)) return thuTu;
  }
  return 27;
}

function taoForm(thuTu: number): FormState {
  return {
    tenKQHT: "",
    thuTu,
    moTa: "",
    trangThai: "dang_su_dung",
    kyHieu: kyHieuTheoThuTu(thuTu),
  };
}

function thongBaoLoi(error: unknown) {
  if (error instanceof ApiError) return error.message;
  return "Không thể tải dữ liệu. Vui lòng thử lại.";
}

export default function LearningOutcomesPage() {
  const [items, setItems] = useState<KqhtDto[]>([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<"tat_ca" | TrangThaiSuDung>("tat_ca");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<FormState>(() => taoForm(1));
  const [formErrors, setFormErrors] = useState<FormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setLoadError(null);
    kqhtApi.danhSach(controller.signal)
      .then((data) => setItems([...data].sort((a, b) => a.thuTu - b.thuTu)))
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === "AbortError")) {
          setLoadError(thongBaoLoi(error));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [reloadKey]);

  useEffect(() => {
    if (!showForm) return undefined;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !submitting) setShowForm(false);
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [showForm, submitting]);

  const nextOrder = useMemo(() => timThuTuTiepTheo(items), [items]);
  const visibleItems = useMemo(() => {
    const keyword = query.trim().toLocaleLowerCase("vi");
    return items.filter((item) => {
      const matchesQuery = !keyword || `${item.kyHieu} ${item.tenKQHT} ${item.moTa ?? ""}`.toLocaleLowerCase("vi").includes(keyword);
      return matchesQuery && (status === "tat_ca" || item.trangThai === status);
    });
  }, [items, query, status]);

  const activeCount = items.filter((item) => item.trangThai === "dang_su_dung").length;
  const inactiveCount = items.length - activeCount;

  function openCreateForm() {
    setForm(taoForm(nextOrder));
    setFormErrors({});
    setSubmitError(null);
    setShowForm(true);
  }

  function changeOrder(value: string) {
    const thuTu = Number(value);
    setForm((current) => ({ ...current, thuTu, kyHieu: kyHieuTheoThuTu(thuTu) }));
    setFormErrors((current) => ({ ...current, thuTu: undefined, kyHieu: undefined }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const errors: FormErrors = {};
    if (!form.tenKQHT.trim()) errors.tenKQHT = "Nhập tên kết quả học tập.";
    else if (form.tenKQHT.trim().length > 100) errors.tenKQHT = "Tên không được vượt quá 100 ký tự.";
    if (!Number.isInteger(form.thuTu) || form.thuTu < 1 || form.thuTu > 26) errors.thuTu = "Thứ tự phải từ 1 đến 26.";
    else if (items.some((item) => item.thuTu === form.thuTu)) errors.thuTu = "Thứ tự này đã được sử dụng.";
    if (form.moTa.length > 1000) errors.moTa = "Mô tả không được vượt quá 1000 ký tự.";
    setFormErrors(errors);
    setSubmitError(null);
    if (Object.keys(errors).length > 0) return;

    const body: TaoKqhtRequest = {
      tenKQHT: form.tenKQHT.trim(),
      thuTu: form.thuTu,
      kyHieu: form.kyHieu,
      moTa: form.moTa.trim() || null,
      trangThai: form.trangThai,
    };

    setSubmitting(true);
    try {
      const created = await kqhtApi.tao(body);
      setItems((current) => [...current, created].sort((a, b) => a.thuTu - b.thuTu));
      setShowForm(false);
      setSuccess(`Đã thêm ${created.kyHieu} — ${created.tenKQHT}.`);
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.errors).length > 0) {
        setFormErrors({
          tenKQHT: error.errors.tenKQHT?.join(" "),
          thuTu: error.errors.thuTu?.join(" "),
          moTa: error.errors.moTa?.join(" "),
          trangThai: error.errors.trangThai?.join(" "),
          kyHieu: error.errors.kyHieu?.join(" "),
        });
      } else {
        setSubmitError(thongBaoLoi(error));
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="page-stack outcomes-page">
      <section className="page-heading-row">
        <div>
          <span className="page-kicker">CHUẨN ĐẦU RA MÔN HỌC</span>
          <h2>Kết quả học tập</h2>
          <p>Tổ chức mục tiêu đánh giá và quy ước mã cho ngân hàng câu hỏi.</p>
        </div>
        <div className="heading-actions">
          <button className="button button-primary" disabled={nextOrder > 26} onClick={openCreateForm} type="button">
            <Icon name="plus" size={17} /> Thêm KQHT
          </button>
        </div>
      </section>

      {success && (
        <div className="success-banner" role="status">
          <span><Icon name="check" size={17} /> {success}</span>
          <button aria-label="Đóng thông báo" onClick={() => setSuccess(null)} type="button"><Icon name="x" size={16} /></button>
        </div>
      )}

      <section className="outcome-summary" aria-label="Tóm tắt kết quả học tập">
        <article className="outcome-summary-main">
          <span className="summary-icon"><Icon name="layers" size={22} /></span>
          <div><small>TỔNG KẾT QUẢ HỌC TẬP</small><strong>{loading ? "—" : items.length.toString().padStart(2, "0")}</strong><p>Mỗi KQHT tạo một nhóm mã câu hỏi riêng.</p></div>
        </article>
        <article><span className="summary-dot active" /><div><small>ĐANG SỬ DỤNG</small><strong>{loading ? "—" : activeCount}</strong></div></article>
        <article><span className="summary-dot inactive" /><div><small>NGỪNG SỬ DỤNG</small><strong>{loading ? "—" : inactiveCount}</strong></div></article>
        <article><span className="summary-letter">{nextOrder <= 26 ? kyHieuTheoThuTu(nextOrder) : "✓"}</span><div><small>KÝ HIỆU TIẾP THEO</small><strong>{nextOrder <= 26 ? `KQHT ${nextOrder}` : "Đã đủ 26"}</strong></div></article>
      </section>

      <section className="panel table-panel outcomes-table-panel">
        <div className="table-toolbar">
          <div className="search-box"><Icon name="search" size={18} /><input aria-label="Tìm kết quả học tập" onChange={(event) => setQuery(event.target.value)} placeholder="Tìm theo ký hiệu, tên hoặc mô tả" value={query} /></div>
          <select aria-label="Lọc trạng thái" onChange={(event) => setStatus(event.target.value as typeof status)} value={status}>
            <option value="tat_ca">Tất cả trạng thái</option>
            <option value="dang_su_dung">Đang sử dụng</option>
            <option value="ngung_su_dung">Ngừng sử dụng</option>
          </select>
        </div>

        {loading ? (
          <div className="outcomes-loading" aria-live="polite" aria-label="Đang tải kết quả học tập">
            {[1, 2, 3, 4].map((item) => <span key={item} />)}
          </div>
        ) : loadError ? (
          <div className="empty-state error-state"><Icon name="alert" size={28} /><strong>Chưa tải được dữ liệu</strong><span>{loadError}</span><button className="button button-secondary" onClick={() => setReloadKey((value) => value + 1)} type="button">Thử lại</button></div>
        ) : (
          <>
            <div className="table-scroll">
              <table className="data-table outcomes-table">
                <thead><tr><th>KÝ HIỆU</th><th>KẾT QUẢ HỌC TẬP</th><th>QUY ƯỚC MÃ CÂU HỎI</th><th>THỨ TỰ</th><th>TRẠNG THÁI</th></tr></thead>
                <tbody>
                  {visibleItems.map((item) => (
                    <tr key={item.maKQHT}>
                      <td><span className="outcome-letter">{item.kyHieu}</span></td>
                      <td><div className="outcome-name"><strong>{item.tenKQHT}</strong><small>{item.moTa || "Chưa có mô tả"}</small></div></td>
                      <td><code>{item.kyHieu}1-001</code><small className="code-caption">Mức độ 1 · câu 001</small></td>
                      <td><span className="order-chip">{item.thuTu.toString().padStart(2, "0")}</span></td>
                      <td><span className={`status-dot ${item.trangThai === "dang_su_dung" ? "status-active" : "status-disabled"}`}>{statusLabels[item.trangThai]}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {visibleItems.length === 0 && <div className="empty-state"><Icon name="search" size={28} /><strong>Không có KQHT phù hợp</strong><span>Thử thay đổi từ khóa hoặc trạng thái.</span></div>}
            <div className="table-footer"><span><strong>{visibleItems.length}</strong> / {items.length} kết quả học tập</span><span>Ký hiệu A–Z theo thứ tự 1–26</span></div>
          </>
        )}
      </section>

      {showForm && (
        <div className="dialog-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !submitting) setShowForm(false); }}>
          <section aria-labelledby="create-outcome-title" aria-modal="true" className="form-dialog" role="dialog">
            <header><div><span className="dialog-icon"><Icon name="layers" size={20} /></span><div><small>THIẾT LẬP CHUẨN ĐẦU RA</small><h3 id="create-outcome-title">Thêm kết quả học tập</h3></div></div><button aria-label="Đóng" className="dialog-close" disabled={submitting} onClick={() => setShowForm(false)} type="button"><Icon name="x" size={19} /></button></header>
            <form noValidate onSubmit={handleSubmit}>
              <div className="dialog-body">
                {submitError && <div className="form-alert" role="alert"><Icon name="alert" size={16} /><span>{submitError}</span></div>}
                <label className="dialog-field full"><span>Tên kết quả học tập <em>*</em></span><input autoFocus maxLength={100} onChange={(event) => { setForm((current) => ({ ...current, tenKQHT: event.target.value })); setFormErrors((current) => ({ ...current, tenKQHT: undefined })); }} placeholder="Ví dụ: Vận dụng được mô hình dữ liệu quan hệ" value={form.tenKQHT} />{formErrors.tenKQHT && <small className="field-error">{formErrors.tenKQHT}</small>}<small className="field-counter">{form.tenKQHT.length}/100</small></label>
                <div className="dialog-field-grid">
                  <label className="dialog-field"><span>Thứ tự <em>*</em></span><input max={26} min={1} onChange={(event) => changeOrder(event.target.value)} type="number" value={form.thuTu} />{formErrors.thuTu && <small className="field-error">{formErrors.thuTu}</small>}</label>
                  <label className="dialog-field"><span>Ký hiệu</span><div className="readonly-code"><strong>{form.kyHieu || "—"}</strong><small>Tạo tự động theo thứ tự</small></div>{formErrors.kyHieu && <small className="field-error">{formErrors.kyHieu}</small>}</label>
                </div>
                <label className="dialog-field full"><span>Mô tả</span><textarea maxLength={1000} onChange={(event) => { setForm((current) => ({ ...current, moTa: event.target.value })); setFormErrors((current) => ({ ...current, moTa: undefined })); }} placeholder="Mô tả ngắn năng lực người học cần đạt…" rows={4} value={form.moTa} />{formErrors.moTa && <small className="field-error">{formErrors.moTa}</small>}<small className="field-counter">{form.moTa.length}/1000</small></label>
                <label className="dialog-field full"><span>Trạng thái</span><select onChange={(event) => setForm((current) => ({ ...current, trangThai: event.target.value as TrangThaiSuDung }))} value={form.trangThai}><option value="dang_su_dung">Đang sử dụng</option><option value="ngung_su_dung">Ngừng sử dụng</option></select><small className="field-help">KQHT đang sử dụng có thể được gắn vào câu hỏi mới.</small></label>
                <div className="code-preview"><span>MÃ CÂU HỎI MẪU</span><strong>{form.kyHieu || "?"}1-001</strong><p>Ký hiệu KQHT · mức độ · số thứ tự</p></div>
              </div>
              <footer><button className="button button-secondary" disabled={submitting} onClick={() => setShowForm(false)} type="button">Hủy</button><button className="button button-primary" disabled={submitting} type="submit">{submitting ? "Đang lưu…" : <><Icon name="plus" size={16} /> Thêm KQHT</>}</button></footer>
            </form>
          </section>
        </div>
      )}
    </div>
  );
}
