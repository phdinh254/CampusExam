import { describe, expect, it } from "vitest";

import { dinhDangNgay, kiemTraForm, loiTuServer, taoFormTrong, yeuCauTao } from "./questionForm";

const formHopLe = {
  ...taoFormTrong(3),
  noiDung: "  Khóa chính là gì?  ",
  phuongAnA: "A ",
  phuongAnB: "B",
  phuongAnC: "C",
  phuongAnD: "D",
  dapAnDung: "C" as const,
};

describe("kiemTraForm", () => {
  it("chấp nhận form đủ dữ liệu", () => {
    expect(kiemTraForm(formHopLe, true)).toEqual({});
  });

  it("bắt buộc KQHT chỉ khi thêm mới", () => {
    const form = { ...formHopLe, maKQHT: "" as const };
    expect(kiemTraForm(form, true).maKQHT).toBeDefined();
    expect(kiemTraForm(form, false).maKQHT).toBeUndefined();
  });

  it("báo lỗi nội dung và phương án chỉ có khoảng trắng hoặc quá dài", () => {
    const errors = kiemTraForm({ ...formHopLe, noiDung: "   ", phuongAnB: "x".repeat(1001) }, true);
    expect(errors.noiDung).toBe("Nhập nội dung câu hỏi.");
    expect(errors.phuongAnB).toContain("1000");
  });
});

describe("yeuCauTao", () => {
  it("cắt khoảng trắng và giữ KQHT, mức độ, đáp án", () => {
    expect(yeuCauTao(formHopLe)).toEqual({
      maKQHT: 3,
      maMD: 1,
      noiDung: "Khóa chính là gì?",
      phuongAnA: "A",
      phuongAnB: "B",
      phuongAnC: "C",
      phuongAnD: "D",
      dapAnDung: "C",
      coDinhDapAn: false,
    });
  });
});

describe("loiTuServer", () => {
  it("chỉ giữ lỗi của trường có trên form", () => {
    expect(loiTuServer({ maKQHT: ["KQHT không tồn tại."], phienBan: ["Phiên bản không hợp lệ."] }))
      .toEqual({ maKQHT: "KQHT không tồn tại." });
  });
});

describe("dinhDangNgay", () => {
  it("hiển thị theo giờ Việt Nam", () => {
    // 20:00 UTC ngày 8 là 03:00 ngày 9 ở Việt Nam.
    expect(dinhDangNgay("2026-11-08T20:00:00Z")).toBe("09/11/2026");
  });
});
