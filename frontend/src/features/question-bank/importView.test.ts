import { describe, expect, it } from "vitest";

import type { DongXemTruocCauHoi } from "../../types/api";
import { dinhDangDungLuong, locDong, soCanhBao, tomTatNoiDung } from "./importView";

const dong = (soDong: number, vanDe: DongXemTruocCauHoi["vanDe"]): DongXemTruocCauHoi => ({
  soDong, maCauHoi: `A1-${soDong}`, kyHieu: "A", maMD: 1, stt: soDong, noiDung: "x",
  phuongAnA: "a", phuongAnB: "b", phuongAnC: "c", phuongAnD: "d", dapAnDung: "A",
  danhDauCauDan: null, ghiChu: null, hinhAnh: [], vanDe, hopLe: !vanDe.some((v) => v.mucDo === "loi"),
});

describe("tomTatNoiDung", () => {
  it("thay mã hình và gộp khoảng trắng", () => {
    expect(tomTatNoiDung("Cho hình {{media:NOIDUNG:1}}\n\nxác định")).toBe("Cho hình [hình] xác định");
  });

  it("cắt nội dung dài", () => {
    expect(tomTatNoiDung("a".repeat(130), 120)).toBe(`${"a".repeat(120)}…`);
  });
});

describe("lọc dòng", () => {
  const danhSach = [
    dong(2, []),
    dong(3, [{ code: "DUPLICATE_IN_DATABASE", noiDung: "Trùng", mucDo: "loi" }]),
    dong(4, [{ code: "UNSUPPORTED_IMAGE_FORMAT", noiDung: "EMF", mucDo: "canh_bao" }]),
  ];

  it("chỉ giữ dòng có vấn đề khi bật lọc", () => {
    expect(locDong(danhSach, true).map((d) => d.soDong)).toEqual([3, 4]);
    expect(locDong(danhSach, false)).toHaveLength(3);
  });

  it("đếm cảnh báo", () => {
    expect(soCanhBao(danhSach)).toBe(1);
  });
});

describe("dinhDangDungLuong", () => {
  it("đổi đơn vị", () => {
    expect(dinhDangDungLuong(512)).toBe("512 B");
    expect(dinhDangDungLuong(1536)).toBe("1.5 KB");
    expect(dinhDangDungLuong(5 * 1024 * 1024)).toBe("5.0 MB");
  });
});
