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
  /** Số câu hỏi thuộc KQHT; lớn hơn 0 thì không đổi được thứ tự và không xóa được. */
  soCauHoi: number;
};

/** `PUT /kqht/{maKQHT}`: ký hiệu suy ra từ thứ tự; trạng thái đổi qua PATCH. */
export type SuaKqhtRequest = {
  tenKQHT: string;
  thuTu: number;
  moTa?: string | null;
};

export type DoiTrangThaiRequest = {
  trangThai: TrangThaiSuDung;
};

export type CauHoiTomTat = {
  maCH: number;
  maCauHoi: string;
  tomTat: string;
  kqht: { maKQHT: number; thuTu: number; kyHieu: string };
  maMD: MaMucDo;
  trangThai: TrangThaiSuDung;
  coHinhAnh: boolean;
  ngayCapNhat: string;
};

export type ViTriHinhAnh = "NOIDUNG" | "DAA" | "DAB" | "DAC" | "DAD";

export type DapAn = "A" | "B" | "C" | "D";

export type CauHoiChiTiet = {
  maCH: number;
  maCauHoi: string;
  maKQHT: number;
  maMD: MaMucDo;
  noiDung: string;
  phuongAnA: string;
  phuongAnB: string;
  phuongAnC: string;
  phuongAnD: string;
  dapAnDung: DapAn;
  coDinhDapAn: boolean;
  trangThai: TrangThaiSuDung;
  hinhAnh: { maTN: number; viTri: ViTriHinhAnh; thuTu: number; url: string; altText: string | null }[];
  /** Số đề gốc chứa câu hỏi; lớn hơn 0 thì không xóa được. */
  soDeDaDung: number;
  phienBan: string;
  ngayTao: string;
  ngayCapNhat: string | null;
};

export type NoiDungCauHoi = {
  noiDung: string;
  phuongAnA: string;
  phuongAnB: string;
  phuongAnC: string;
  phuongAnD: string;
  dapAnDung: DapAn;
  coDinhDapAn: boolean;
};

export type TaoCauHoiRequest = NoiDungCauHoi & { maKQHT: number; maMD: MaMucDo };

/** Không đổi được KQHT/mức độ vì chúng quyết định mã câu hỏi. */
export type SuaCauHoiRequest = NoiDungCauHoi & { phienBan: string };

export type TruyVanCauHoi = TruyVanTrang & {
  maKQHT?: number;
  maMD?: MaMucDo;
  trangThai?: TrangThaiSuDung;
  /** `maCauHoi`, `ngayCapNhat`, `ngayTao`; tiền tố `-` là giảm dần. */
  sapXep?: string;
};

export type ThongKeCauHoi = {
  tongSo: number;
  dangSuDung: number;
  ngungSuDung: number;
  /** Số KQHT đang sử dụng. */
  soKQHT: number;
};

export type GioiTinh = "nu" | "nam";

export type TrangThaiTaiKhoan = "khoa" | "hoat_dong";

export type ThiSinhTomTat = {
  msts: string;
  hoTen: string;
  /** `YYYY-MM-DD`. */
  ngaySinh: string;
  gioiTinh: GioiTinh | null;
  lop: string | null;
  trangThaiTaiKhoan: TrangThaiTaiKhoan;
  /** false khi thí sinh còn dùng mật khẩu mặc định (ngày sinh). */
  daDoiMatKhau: boolean;
  soLuotThi: number;
  ngayTao: string;
};

export type ThiSinhChiTiet = ThiSinhTomTat & {
  noiSinh: string | null;
  cccd: string | null;
  lanDNCuoi: string | null;
  /** Số kỳ thi được phân công; lớn hơn 0 thì không xóa được. */
  soLanPhanCong: number;
};

export type HoSoThiSinh = {
  hoTen: string;
  ngaySinh: string;
  gioiTinh: GioiTinh | null;
  lop: string | null;
  noiSinh: string | null;
  cccd: string | null;
};

/** Tạo kèm tài khoản: tên đăng nhập = MSTS, mật khẩu mặc định = ngày sinh ddMMyyyy. */
export type TaoThiSinhRequest = HoSoThiSinh & { msts: string };

export type TruyVanThiSinh = TruyVanTrang & {
  lop?: string;
  trangThai?: TrangThaiTaiKhoan;
  daDoiMatKhau?: boolean;
  /** `msts`, `hoTen`, `lop`, `ngayTao`; tiền tố `-` là giảm dần. */
  sapXep?: string;
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
