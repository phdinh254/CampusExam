import { httpClient } from "../../services/httpClient";
import type { KqhtDto, MucDoDto, SuaKqhtRequest, TaoKqhtRequest, TrangThaiSuDung } from "../../types/api";

export const kqhtApi = {
  danhSach: (signal?: AbortSignal) => httpClient.get<KqhtDto[]>("/kqht", { signal }),

  chiTiet: (maKQHT: number, signal?: AbortSignal) =>
    httpClient.get<KqhtDto>(`/kqht/${maKQHT}`, { signal }),

  tao: (body: TaoKqhtRequest) => httpClient.post<KqhtDto>("/kqht", body),

  sua: (maKQHT: number, body: SuaKqhtRequest) => httpClient.put<KqhtDto>(`/kqht/${maKQHT}`, body),

  doiTrangThai: (maKQHT: number, trangThai: TrangThaiSuDung) =>
    httpClient.patch<KqhtDto>(`/kqht/${maKQHT}/trang-thai`, { trangThai }),

  /** Chỉ xóa được KQHT chưa có câu hỏi/ma trận; ngược lại lỗi `KQHT_IN_USE`. */
  xoa: (maKQHT: number) => httpClient.delete<void>(`/kqht/${maKQHT}`),
};

export const mucDoApi = {
  danhSach: (signal?: AbortSignal) => httpClient.get<MucDoDto[]>("/muc-do", { signal }),
};
