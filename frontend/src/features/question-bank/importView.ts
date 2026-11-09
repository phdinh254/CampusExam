import type { DongXemTruocCauHoi, ViTriHinhAnh } from "../../types/api";

export const TEN_VI_TRI: Record<ViTriHinhAnh, string> = {
  NOIDUNG: "Nội dung",
  DAA: "Phương án A",
  DAB: "Phương án B",
  DAC: "Phương án C",
  DAD: "Phương án D",
};

/** `{{media:NOIDUNG:1}}` → `[hình]`, gộp khoảng trắng, cắt còn `doDai` ký tự. */
export function tomTatNoiDung(noiDung: string, doDai = 120): string {
  const gon = noiDung.replace(/\{\{media:[A-Z]+:\d+\}\}/g, "[hình]").replace(/\s+/g, " ").trim();
  return gon.length > doDai ? `${gon.slice(0, doDai).trimEnd()}…` : gon;
}

/** Các dòng cần hiển thị: tất cả, hoặc chỉ dòng có lỗi/cảnh báo. */
export function locDong(dong: DongXemTruocCauHoi[], chiCoVanDe: boolean): DongXemTruocCauHoi[] {
  return chiCoVanDe ? dong.filter((item) => item.vanDe.length > 0) : dong;
}

export function soCanhBao(dong: DongXemTruocCauHoi[]): number {
  return dong.reduce((tong, item) => tong + item.vanDe.filter((v) => v.mucDo === "canh_bao").length, 0);
}

/** Dung lượng tệp dễ đọc: 1536 → "1.5 KB". */
export function dinhDangDungLuong(byte: number): string {
  if (byte < 1024) return `${byte} B`;
  if (byte < 1024 * 1024) return `${(byte / 1024).toFixed(1)} KB`;
  return `${(byte / (1024 * 1024)).toFixed(1)} MB`;
}

export function dinhDangGio(iso: string): string {
  return new Date(iso).toLocaleString("vi-VN", {
    timeZone: "Asia/Ho_Chi_Minh",
    hour: "2-digit",
    minute: "2-digit",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}
