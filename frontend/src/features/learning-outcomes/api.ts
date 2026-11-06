import { httpClient } from "../../services/httpClient";
import type { KqhtDto, MucDoDto, TaoKqhtRequest } from "../../types/api";

export const kqhtApi = {
  danhSach: (signal?: AbortSignal) => httpClient.get<KqhtDto[]>("/kqht", { signal }),

  chiTiet: (maKQHT: number, signal?: AbortSignal) =>
    httpClient.get<KqhtDto>(`/kqht/${maKQHT}`, { signal }),

  tao: (body: TaoKqhtRequest) => httpClient.post<KqhtDto>("/kqht", body),
};

export const mucDoApi = {
  danhSach: (signal?: AbortSignal) => httpClient.get<MucDoDto[]>("/muc-do", { signal }),
};
