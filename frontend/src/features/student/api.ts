import { httpClient } from "../../services/httpClient";
import type { HoSoCaNhan } from "../../types/api";

/** API của thí sinh đang đăng nhập (`/api/toi/*`, API_DESIGN.md mục 2.11). */
export const toiApi = {
  hoSo: (signal?: AbortSignal) => httpClient.get<HoSoCaNhan>("/toi/ho-so", { signal }),
};
