import { httpClient } from "../../services/httpClient";
import type {
  HoSoThiSinh,
  TaoThiSinhRequest,
  ThiSinhChiTiet,
  ThiSinhTomTat,
  TrangDuLieu,
  TrangThaiTaiKhoan,
  TruyVanThiSinh,
} from "../../types/api";

const duongDan = (msts: string) => `/thi-sinh/${encodeURIComponent(msts)}`;

export const thiSinhApi = {
  danhSach: (truyVan: TruyVanThiSinh, signal?: AbortSignal) =>
    httpClient.get<TrangDuLieu<ThiSinhTomTat>>("/thi-sinh", { query: truyVan, signal }),

  lop: (signal?: AbortSignal) => httpClient.get<string[]>("/thi-sinh/lop", { signal }),

  chiTiet: (msts: string, signal?: AbortSignal) => httpClient.get<ThiSinhChiTiet>(duongDan(msts), { signal }),

  tao: (body: TaoThiSinhRequest) => httpClient.post<ThiSinhChiTiet>("/thi-sinh", body),

  sua: (msts: string, body: HoSoThiSinh) => httpClient.put<ThiSinhChiTiet>(duongDan(msts), body),

  doiTrangThaiTaiKhoan: (msts: string, trangThai: TrangThaiTaiKhoan) =>
    httpClient.patch<ThiSinhChiTiet>(`${duongDan(msts)}/trang-thai-tai-khoan`, { trangThai }),

  /** Đặt lại về ngày sinh; API không trả mật khẩu. */
  capLaiMatKhau: (msts: string) => httpClient.post<ThiSinhChiTiet>(`${duongDan(msts)}/cap-lai-mat-khau`),

  /** Chỉ xóa được thí sinh chưa được phân công thi; ngược lại lỗi `STUDENT_IN_USE`. */
  xoa: (msts: string) => httpClient.delete<void>(duongDan(msts)),
};
