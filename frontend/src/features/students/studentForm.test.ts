import { describe, expect, it } from "vitest";

import { chuVietTat, dinhDangNgaySinh, homNay, hoSoGui, kiemTraForm, taoFormTrong, yeuCauTao } from "./studentForm";

const now = new Date("2026-11-09T03:00:00Z");
const hopLe = { ...taoFormTrong(), msts: " SV001 ", hoTen: " Nguyễn An ", ngaySinh: "2005-03-14", gioiTinh: "nam" as const };

describe("kiemTraForm", () => {
  it("chấp nhận form đủ dữ liệu", () => {
    expect(kiemTraForm(hopLe, true, now)).toEqual({});
  });

  it("chỉ kiểm tra mã số khi thêm mới", () => {
    const form = { ...hopLe, msts: "SV 01" };
    expect(kiemTraForm(form, true, now).msts).toBeDefined();
    expect(kiemTraForm(form, false, now).msts).toBeUndefined();
  });

  it("chặn ngày sinh tương lai, quá cũ và CCCD sai", () => {
    expect(kiemTraForm({ ...hopLe, ngaySinh: "2026-11-10" }, true, now).ngaySinh).toBe("Ngày sinh không hợp lệ.");
    expect(kiemTraForm({ ...hopLe, ngaySinh: "1899-12-31" }, true, now).ngaySinh).toBe("Ngày sinh không hợp lệ.");
    expect(kiemTraForm({ ...hopLe, cccd: "12345" }, true, now).cccd).toBeDefined();
  });
});

describe("chuyển dữ liệu", () => {
  it("cắt khoảng trắng và đổi chuỗi rỗng thành null", () => {
    expect(yeuCauTao({ ...hopLe, lop: "  " })).toEqual({
      msts: "SV001", hoTen: "Nguyễn An", ngaySinh: "2005-03-14", gioiTinh: "nam", lop: null, noiSinh: null, cccd: null,
    });
    expect(hoSoGui({ ...hopLe, gioiTinh: "" }).gioiTinh).toBeNull();
  });

  it("định dạng ngày sinh không lệch múi giờ", () => {
    expect(dinhDangNgaySinh("2005-03-14")).toBe("14/03/2005");
  });

  it("homNay theo giờ Việt Nam", () => {
    expect(homNay(new Date("2026-11-08T20:00:00Z"))).toBe("2026-11-09");
  });

  it("chữ viết tắt từ tên và họ", () => {
    expect(chuVietTat("Nguyễn Minh Long")).toBe("LN");
    expect(chuVietTat("An")).toBe("A");
  });
});
