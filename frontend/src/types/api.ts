/**
 * Kiểu dữ liệu hợp đồng API (docs/API_DESIGN.md).
 * Khi backend chạy ở Development có thể đối chiếu với /openapi/v1.json.
 */

/** Lỗi theo trường: khóa là tên trường JSON, ví dụ `tenKQHT`. */
export type ValidationErrors = Record<string, string[]>;

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Mã lỗi ổn định, ví dụ `VALIDATION_FAILED`. */
  code?: string;
  errors?: ValidationErrors;
  traceId?: string;
  /** Có khi `code = AUTH_ACCOUNT_LOCKED`: thời điểm hết tạm khóa (UTC). */
  khoaDen?: string;
};

export type VaiTro = "ADMIN" | "THISINH";

export type TaiKhoanHienTai = {
  mtk: number;
  tenDN: string;
  hoTen: string;
  vaiTro: VaiTro;
  /** Mã số thí sinh; null với quản trị viên. */
  msts: string | null;
  phaiDoiMatKhau: boolean;
};

export type PhienDangNhap = {
  accessToken: string;
  hetHan: string;
  taiKhoan: TaiKhoanHienTai;
};

export type DangNhapRequest = {
  tenDN: string;
  matKhau: string;
  ghiNho: boolean;
};

export type DoiMatKhauRequest = {
  matKhauCu: string;
  matKhauMoi: string;
};

/** Một trang của API danh sách có phân trang (mục 1.3). */
export type TrangDuLieu<T> = {
  duLieu: T[];
  trang: number;
  kichThuocTrang: number;
  tongSoDong: number;
  tongSoTrang: number;
};

export type TruyVanTrang = {
  trang?: number;
  kichThuocTrang?: number;
  sapXep?: string;
  tuKhoa?: string;
};

/** KQHOCTAP.TRANGTHAI, CAUHOI.TRANGTHAI. */
export type TrangThaiSuDung = "dang_su_dung" | "ngung_su_dung";

/** 1 dễ, 2 trung bình, 3 khó. */
export type MaMucDo = 1 | 2 | 3;

export type MucDoDto = {
  maMD: MaMucDo;
  tenMD: string;
};

export type KqhtDto = {
  maKQHT: number;
  tenKQHT: string;
  thuTu: number;
  moTa: string | null;
  trangThai: TrangThaiSuDung;
  kyHieu: string;
};

export type TaoKqhtRequest = {
  tenKQHT: string;
  thuTu: number;
  moTa?: string | null;
  trangThai?: TrangThaiSuDung;
  kyHieu: string;
};

export type MucDoVanDe = "canh_bao" | "loi";

export type VanDeNhapCauHoi = {
  code: string;
  noiDung: string;
  mucDo: MucDoVanDe;
};

export type HinhAnhXemTruoc = {
  maDanhDau: string;
  viTri: "NOIDUNG" | "DAA" | "DAB" | "DAC" | "DAD";
  thuTu: number;
  tenFile: string;
  mimeType: string;
};

export type DongXemTruocCauHoi = {
  soDong: number;
  maCauHoi: string | null;
  kyHieu: string | null;
  maMD: MaMucDo | null;
  stt: number | null;
  noiDung: string;
  phuongAnA: string;
  phuongAnB: string;
  phuongAnC: string;
  phuongAnD: string;
  dapAnDung: string | null;
  danhDauCauDan: string | null;
  ghiChu: string | null;
  hinhAnh: HinhAnhXemTruoc[];
  vanDe: VanDeNhapCauHoi[];
  hopLe: boolean;
};

export type XemTruocNhapCauHoi = {
  tongDong: number;
  soDongHopLe: number;
  soDongLoi: number;
  tongHinhAnh: number;
  dong: DongXemTruocCauHoi[];
};
