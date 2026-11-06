import { httpClient } from "../../services/httpClient";
import type { XemTruocNhapCauHoi } from "../../types/api";

/** Giới hạn khớp backend (QuestionImportsController); kiểm tra sớm để không upload vô ích. */
export const KICH_THUOC_TEP_TOI_DA = 20 * 1024 * 1024;

export const nhapCauHoiApi = {
  xemTruoc: (file: File, signal?: AbortSignal) => {
    const form = new FormData();
    form.append("file", file, file.name);
    return httpClient.post<XemTruocNhapCauHoi>("/nhap-cau-hoi/xem-truoc", form, { signal });
  },
};
