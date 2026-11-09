import type { CauHoiChiTiet, DapAn, MaMucDo, NoiDungCauHoi, TaoCauHoiRequest } from "../../types/api";

export const DAP_AN: DapAn[] = ["A", "B", "C", "D"];

export const MUC_DO: Record<MaMucDo, string> = { 1: "Dễ", 2: "Trung bình", 3: "Khó" };

export type QuestionFormState = {
  maKQHT: number | "";
  maMD: MaMucDo;
  noiDung: string;
  phuongAnA: string;
  phuongAnB: string;
  phuongAnC: string;
  phuongAnD: string;
  dapAnDung: DapAn;
  coDinhDapAn: boolean;
};

export type QuestionFormErrors = Partial<Record<keyof QuestionFormState, string>>;

export const GIOI_HAN = { noiDung: 4000, phuongAn: 1000 } as const;

export function phuongAnKey(dapAn: DapAn): "phuongAnA" | "phuongAnB" | "phuongAnC" | "phuongAnD" {
  return `phuongAn${dapAn}`;
}

export function taoFormTrong(maKQHT: number | "" = ""): QuestionFormState {
  return { maKQHT, maMD: 1, noiDung: "", phuongAnA: "", phuongAnB: "", phuongAnC: "", phuongAnD: "", dapAnDung: "A", coDinhDapAn: false };
}

export function formTuChiTiet(cauHoi: CauHoiChiTiet): QuestionFormState {
  return {
    maKQHT: cauHoi.maKQHT,
    maMD: cauHoi.maMD,
    noiDung: cauHoi.noiDung,
    phuongAnA: cauHoi.phuongAnA,
    phuongAnB: cauHoi.phuongAnB,
    phuongAnC: cauHoi.phuongAnC,
    phuongAnD: cauHoi.phuongAnD,
    dapAnDung: cauHoi.dapAnDung,
    coDinhDapAn: cauHoi.coDinhDapAn,
  };
}

/** Cùng quy tắc với `NoiDungCauHoiCommand`/`TaoCauHoiCommand` ở backend. */
export function kiemTraForm(form: QuestionFormState, taoMoi: boolean): QuestionFormErrors {
  const errors: QuestionFormErrors = {};
  if (taoMoi && form.maKQHT === "") errors.maKQHT = "Chọn kết quả học tập.";
  if (!form.noiDung.trim()) errors.noiDung = "Nhập nội dung câu hỏi.";
  else if (form.noiDung.trim().length > GIOI_HAN.noiDung) errors.noiDung = `Nội dung không được vượt quá ${GIOI_HAN.noiDung} ký tự.`;
  for (const dapAn of DAP_AN) {
    const key = phuongAnKey(dapAn);
    const value = form[key].trim();
    if (!value) errors[key] = `Nhập phương án ${dapAn}.`;
    else if (value.length > GIOI_HAN.phuongAn) errors[key] = `Phương án không được vượt quá ${GIOI_HAN.phuongAn} ký tự.`;
  }
  return errors;
}

export function noiDungGui(form: QuestionFormState): NoiDungCauHoi {
  return {
    noiDung: form.noiDung.trim(),
    phuongAnA: form.phuongAnA.trim(),
    phuongAnB: form.phuongAnB.trim(),
    phuongAnC: form.phuongAnC.trim(),
    phuongAnD: form.phuongAnD.trim(),
    dapAnDung: form.dapAnDung,
    coDinhDapAn: form.coDinhDapAn,
  };
}

export function yeuCauTao(form: QuestionFormState): TaoCauHoiRequest {
  if (form.maKQHT === "") throw new Error("Thiếu KQHT.");
  return { ...noiDungGui(form), maKQHT: form.maKQHT, maMD: form.maMD };
}

/** Lỗi theo trường từ ProblemDetails → lỗi của form (chỉ giữ trường form có). */
export function loiTuServer(errors: Record<string, string[]>): QuestionFormErrors {
  const keys: (keyof QuestionFormState)[] = ["maKQHT", "maMD", "noiDung", "phuongAnA", "phuongAnB", "phuongAnC", "phuongAnD", "dapAnDung"];
  const result: QuestionFormErrors = {};
  for (const key of keys) {
    if (errors[key]?.length) result[key] = errors[key].join(" ");
  }
  return result;
}

export function dinhDangNgay(iso: string): string {
  return new Date(iso).toLocaleDateString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh", day: "2-digit", month: "2-digit", year: "numeric" });
}
