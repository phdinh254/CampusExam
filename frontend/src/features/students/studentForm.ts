import type { GioiTinh, HoSoThiSinh, TaoThiSinhRequest, ThiSinhChiTiet } from "../../types/api";

export type StudentFormState = {
  msts: string;
  hoTen: string;
  ngaySinh: string;
  gioiTinh: GioiTinh | "";
  lop: string;
  noiSinh: string;
  cccd: string;
};

export type StudentFormErrors = Partial<Record<keyof StudentFormState, string>>;

export const GIOI_TINH: Record<GioiTinh, string> = { nu: "Nữ", nam: "Nam" };

export function taoFormTrong(): StudentFormState {
  return { msts: "", hoTen: "", ngaySinh: "", gioiTinh: "", lop: "", noiSinh: "", cccd: "" };
}

export function formTuChiTiet(thiSinh: ThiSinhChiTiet): StudentFormState {
  return {
    msts: thiSinh.msts,
    hoTen: thiSinh.hoTen,
    ngaySinh: thiSinh.ngaySinh,
    gioiTinh: thiSinh.gioiTinh ?? "",
    lop: thiSinh.lop ?? "",
    noiSinh: thiSinh.noiSinh ?? "",
    cccd: thiSinh.cccd ?? "",
  };
}

/** `YYYY-MM-DD` theo giờ Việt Nam, để chặn ngày sinh trong tương lai. */
export function homNay(now = new Date()): string {
  return now.toLocaleDateString("en-CA", { timeZone: "Asia/Ho_Chi_Minh" });
}

/** Cùng quy tắc với `TaoThiSinhCommand`/`HoSoThiSinhCommand` ở backend. */
export function kiemTraForm(form: StudentFormState, taoMoi: boolean, now = new Date()): StudentFormErrors {
  const errors: StudentFormErrors = {};
  const msts = form.msts.trim();
  if (taoMoi) {
    if (!msts) errors.msts = "Nhập mã số thí sinh.";
    else if (msts.length > 20) errors.msts = "Mã số không được vượt quá 20 ký tự.";
    else if (!/^[A-Za-z0-9._-]+$/.test(msts)) errors.msts = "Chỉ gồm chữ không dấu, số và các ký tự . _ -";
  }
  if (!form.hoTen.trim()) errors.hoTen = "Nhập họ tên.";
  else if (form.hoTen.trim().length > 100) errors.hoTen = "Họ tên không được vượt quá 100 ký tự.";
  if (!form.ngaySinh) errors.ngaySinh = "Chọn ngày sinh.";
  else if (form.ngaySinh < "1900-01-01" || form.ngaySinh > homNay(now)) errors.ngaySinh = "Ngày sinh không hợp lệ.";
  if (form.lop.trim().length > 50) errors.lop = "Lớp không được vượt quá 50 ký tự.";
  if (form.noiSinh.trim().length > 200) errors.noiSinh = "Nơi sinh không được vượt quá 200 ký tự.";
  if (form.cccd.trim() && !/^\d{12}$/.test(form.cccd.trim())) errors.cccd = "CCCD gồm đúng 12 chữ số.";
  return errors;
}

export function hoSoGui(form: StudentFormState): HoSoThiSinh {
  const rong = (value: string) => value.trim() || null;
  return {
    hoTen: form.hoTen.trim(),
    ngaySinh: form.ngaySinh,
    gioiTinh: form.gioiTinh || null,
    lop: rong(form.lop),
    noiSinh: rong(form.noiSinh),
    cccd: rong(form.cccd),
  };
}

export function yeuCauTao(form: StudentFormState): TaoThiSinhRequest {
  return { ...hoSoGui(form), msts: form.msts.trim() };
}

export function loiTuServer(errors: Record<string, string[]>): StudentFormErrors {
  const keys: (keyof StudentFormState)[] = ["msts", "hoTen", "ngaySinh", "gioiTinh", "lop", "noiSinh", "cccd"];
  const result: StudentFormErrors = {};
  for (const key of keys) {
    if (errors[key]?.length) result[key] = errors[key].join(" ");
  }
  return result;
}

/** `2005-03-14` → `14/03/2005` (chuỗi ngày, không đổi múi giờ). */
export function dinhDangNgaySinh(ngay: string): string {
  const [nam, thang, ngayTrongThang] = ngay.split("-");
  return `${ngayTrongThang}/${thang}/${nam}`;
}

/** Hai chữ cái đầu của tên gọi và họ, ví dụ "Nguyễn Minh Long" → "LN". */
export function chuVietTat(hoTen: string): string {
  const tu = hoTen.trim().split(/\s+/).filter(Boolean);
  if (tu.length === 0) return "?";
  const ten = tu[tu.length - 1];
  const ho = tu.length > 1 ? tu[0] : "";
  return `${ten[0]}${ho[0] ?? ""}`.toLocaleUpperCase("vi");
}
