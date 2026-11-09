import { httpClient } from "../../services/httpClient";
import type {
  CauHoiChiTiet,
  CauHoiTomTat,
  SuaCauHoiRequest,
  TaoCauHoiRequest,
  ThongKeCauHoi,
  TrangDuLieu,
  TrangThaiSuDung,
  TruyVanCauHoi,
} from "../../types/api";

export const cauHoiApi = {
  danhSach: (truyVan: TruyVanCauHoi, signal?: AbortSignal) =>
    httpClient.get<TrangDuLieu<CauHoiTomTat>>("/cau-hoi", { query: truyVan, signal }),

  thongKe: (signal?: AbortSignal) => httpClient.get<ThongKeCauHoi>("/cau-hoi/thong-ke", { signal }),

  chiTiet: (maCH: number, signal?: AbortSignal) => httpClient.get<CauHoiChiTiet>(`/cau-hoi/${maCH}`, { signal }),

  tao: (body: TaoCauHoiRequest) => httpClient.post<CauHoiChiTiet>("/cau-hoi", body),

  /** Lệch `phienBan` trả lỗi `CONCURRENCY_CONFLICT`. */
  sua: (maCH: number, body: SuaCauHoiRequest) => httpClient.put<CauHoiChiTiet>(`/cau-hoi/${maCH}`, body),

  doiTrangThai: (maCH: number, trangThai: TrangThaiSuDung) =>
    httpClient.patch<CauHoiChiTiet>(`/cau-hoi/${maCH}/trang-thai`, { trangThai }),

  /** Chỉ xóa được câu chưa nằm trong đề; ngược lại lỗi `QUESTION_IN_USE`. */
  xoa: (maCH: number) => httpClient.delete<void>(`/cau-hoi/${maCH}`),
};
