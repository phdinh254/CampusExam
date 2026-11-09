# Thiết kế API kết nối Frontend ↔ Backend

Ngày soạn: 06/11/2026 · Trạng thái: **Đề xuất**; D-API-01 đã chốt (06/11/2026).

Tài liệu này là hợp đồng giữa `frontend/` (React + Vite) và `backend/` (ASP.NET
Core .NET 10). Mỗi endpoint ghi rõ vai trò được gọi, màn hình dùng nó và bảng
database liên quan. Quyết định nghiệp vụ tham chiếu `BUSINESS_RULE_DECISIONS.md`.

---

## 1. Quy ước chung

### 1.1 Đường dẫn và đặt tên (D-API-01 — đã chốt: tiếng Việt)

| Hạng mục | Quy ước | Ví dụ |
|---|---|---|
| Tiền tố | `/api` (dev đi qua proxy Vite, cùng origin) | `/api/cau-hoi` |
| Tài nguyên | tiếng Việt **không dấu**, chữ thường, kebab-case, dạng danh từ (không thêm "danh sách") | `/api/ma-tran`, `/api/ky-thi` |
| Viết tắt đã quen trong đồ án | giữ nguyên dạng viết tắt | `/api/kqht`, `/api/muc-do` |
| Hành động không phải CRUD | `POST /{tai-nguyen}/{id}/{dong-tu}` | `POST /api/ky-thi/12/phat-hanh` |
| Không gian thí sinh | `/api/toi/...` — luôn lấy thí sinh từ token, không nhận `msts` trên URL | `/api/toi/ky-thi` |
| Tham số query | camelCase không dấu | `?tuKhoa=&maKQHT=&trang=1` |
| Trường JSON | camelCase không dấu, bám tên cột DB khi có — giống `KqhtDto` hiện tại | `maKT`, `tenKT`, `tgBatDau` |
| Mã lỗi `code` | giữ UPPER_SNAKE tiếng Anh như các mã đang có (`KQHT_NOT_FOUND`) — đây là hằng số máy đọc, không hiển thị cho người dùng | `CONCURRENCY_CONFLICT` |

Các endpoint kỹ thuật `/api/health`, `/api/openapi` giữ nguyên tên chuẩn.

Hệ quả với code đang có:

- ✅ `/api/kqht`, `/api/muc-do` và `KqhtDto` đã đúng quy ước.
- 🔁 `/api/question-imports/preview` đổi thành `/api/nhap-cau-hoi/xem-truoc`;
  DTO `QuestionImportPreview` đổi tên trường sang tiếng Việt (mục 2.5) và cập nhật
  test `QuestionImportsControllerTests`.
- Tên lớp C# (controller, DTO, namespace) không bắt buộc đổi; chỉ đường dẫn và
  tên trường JSON là hợp đồng.

### 1.2 Kiểu dữ liệu

- **Thời gian:** ISO 8601 UTC có hậu tố `Z` (`2026-11-06T02:00:00Z`). Backend lưu
  UTC; frontend tự đổi sang `Asia/Ho_Chi_Minh` khi hiển thị.
- **Ngày không giờ** (ngày sinh): `YYYY-MM-DD`.
- **Trạng thái:** trả **chuỗi** ổn định thay vì số trong DB, backend ánh xạ hai
  chiều. Frontend không bao giờ phải biết `TRANGTHAIKYTHI = 2`.

| DB | Giá trị API |
|---|---|
| `KQHOCTAP.TRANGTHAI`, `CAUHOI.TRANGTHAI` 0/1 | `ngung_su_dung` / `dang_su_dung` |
| `TAIKHOAN.TRANGTHAI` 0/1 | `khoa` / `hoat_dong` |
| `MATRAN.TRANGTHAIMATRAN` 0/1/2 | `dang_soan` / `su_dung` / `ngung_su_dung` |
| `KYTHI.TRANGTHAIKYTHI` 0..4 | `nhap` / `da_phat_hanh` / `dang_thi` / `da_ket_thuc` / `da_huy` |
| `KYTHI.CACHPHANDE` 0/1 | `ngau_nhien` / `can_bang` |
| `BAILAM.TRANGTHAIBAILAM` 0/1/2 | `dang_lam` / `da_nop` / `he_thong_thu` |
| `LANIMPORT.TRANGTHAILANIMPORT` 0..3 | `dang_xu_ly` / `hoan_tat` / `that_bai` / `hoan_tat_co_loi` |
| `MUCDO.MAMD` 1/2/3 | giữ số `1/2/3` (đã là một phần mã câu hỏi) |

> `KqhtDto.trangThai` hiện trả số `0/1`. Đổi sang chuỗi ở bước 1 (mục 6) cùng lúc
> với các endpoint mới, trước khi frontend bắt đầu dùng.

- **Phiên bản chống ghi đè:** cột `rowversion` (`PHIENBAN`) trả về dạng chuỗi
  base64 trong trường `phienBan`. Request sửa phải gửi lại `phienBan`; lệch trả
  `409` mã `CONCURRENCY_CONFLICT`.

### 1.3 Phân trang, lọc, sắp xếp

Request: `?trang=1&kichThuocTrang=20&sapXep=-ngayCapNhat,maCauHoi&tuKhoa=...`
(`kichThuocTrang` tối đa 100; dấu `-` là giảm dần; trường sắp xếp nằm trong
allowlist của từng endpoint).

Response danh sách có phân trang:

```json
{
  "duLieu": [ ... ],
  "trang": 1,
  "kichThuocTrang": 20,
  "tongSoDong": 2480,
  "tongSoTrang": 124
}
```

Danh mục nhỏ (mức độ, KQHT) trả mảng thẳng, không phân trang.

### 1.4 Lỗi

Mọi lỗi dùng `ProblemDetails` (RFC 9457) như các endpoint hiện có, luôn có
`code` ổn định để frontend rẽ nhánh và `traceId` để tra log. Trường của
`ProblemDetails` (`title`, `status`, `errors`) giữ tên chuẩn vì do framework sinh.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Dữ liệu không hợp lệ",
  "status": 400,
  "code": "VALIDATION_FAILED",
  "errors": { "tenKQHT": ["Tên KQHT là bắt buộc."] },
  "traceId": "00-4bf9..."
}
```

| HTTP | Khi nào | `code` ví dụ |
|---|---|---|
| 400 | Sai định dạng/validation | `VALIDATION_FAILED`, `QUESTION_IMPORT_INVALID_FILE` |
| 401 | Thiếu/hết hạn access token, sai mật khẩu | `AUTH_TOKEN_EXPIRED`, `AUTH_INVALID_CREDENTIALS` |
| 403 | Sai vai trò, phải đổi mật khẩu | `FORBIDDEN`, `AUTH_PASSWORD_CHANGE_REQUIRED` |
| 404 | Không tồn tại **hoặc** không thuộc quyền xem | `QUESTION_NOT_FOUND` |
| 409 | Trùng, sai trạng thái, ghi đè | `KQHT_KY_HIEU_CONFLICT`, `EXAM_INVALID_STATE`, `CONCURRENCY_CONFLICT` |
| 410 | Bản xem trước import hết hạn | `IMPORT_PREVIEW_EXPIRED` |
| 413 | Tệp quá lớn | `QUESTION_IMPORT_FILE_TOO_LARGE` |
| 423 | Tài khoản tạm khóa | `AUTH_ACCOUNT_LOCKED` (kèm `khoaDen`) |
| 429 | Vượt rate limit | `RATE_LIMITED` (kèm header `Retry-After`) |

Thí sinh truy cập bài làm của người khác nhận **404**, không phải 403, để không
lộ sự tồn tại của tài nguyên.

### 1.5 Xác thực và phiên

- **Access token:** JWT ký HS256, sống 15 phút, chứa `sub` (MTK), `role`
  (`ADMIN`/`THISINH`), `msts` (nếu thí sinh), `sstamp` (= `MABAOMAT`). Frontend
  giữ **trong bộ nhớ** (biến module), không lưu `localStorage`.
- **Refresh token:** chuỗi ngẫu nhiên 256 bit trong cookie
  `ce_refresh` — `HttpOnly; Secure; SameSite=Strict; Path=/api/xac-thuc`. DB chỉ
  lưu SHA-256 ở `PHIENDANGNHAP.TOKENHASH`. Mỗi lần làm mới xoay vòng
  (`MAPHIENTHAY`); dùng lại token cũ đã bị thay ⇒ thu hồi cả chuỗi phiên.
- "Ghi nhớ đăng nhập": bỏ chọn ⇒ cookie phiên (không `Max-Age`), refresh hết hạn
  sau 12 giờ; chọn ⇒ `Max-Age` 7 ngày.
- **Chống CSRF:** `/api/xac-thuc/lam-moi` và `/api/xac-thuc/dang-xuat` dùng cookie
  nên bắt buộc header `X-CSRF: 1` và kiểm tra `Origin` nằm trong allowlist.
- Đổi mật khẩu / khóa tài khoản đổi `MABAOMAT` ⇒ mọi access token cũ bị từ chối ở
  lần kiểm tra `sstamp` kế tiếp, mọi refresh token bị thu hồi.
- Tài khoản `PHAIDOIMATKHAU = 1`: mọi API trừ `/xac-thuc/*` trả `403
  AUTH_PASSWORD_CHANGE_REQUIRED`.

### 1.6 Policy

| Policy | Điều kiện |
|---|---|
| `AdminOnly` | `role = ADMIN` |
| `CandidateOnly` | `role = THISINH` và có `msts` |
| `Authenticated` | đã đăng nhập (dùng cho `/xac-thuc/toi`, `/xac-thuc/doi-mat-khau`) |

Mặc định mọi controller yêu cầu đăng nhập (fallback policy); chỉ
`/xac-thuc/dang-nhap`, `/xac-thuc/lam-moi`, `/health` được `[AllowAnonymous]`.

---

## 2. Danh mục endpoint

Cột **Màn hình** chỉ nơi frontend gọi. Ký hiệu: ✅ đã có · 🔁 đã có, cần đổi ·
🆕 chưa có. Mọi đường dẫn dưới đây đều có tiền tố `/api`.

### 2.1 Xác thực — `/xac-thuc`

| | Method & path | Vai trò | Màn hình |
|---|---|---|---|
| 🆕 | `POST /xac-thuc/dang-nhap` | ẩn danh | Đăng nhập |
| 🆕 | `POST /xac-thuc/lam-moi` | ẩn danh (cookie) | tự động khi 401 / khi tải lại trang |
| 🆕 | `POST /xac-thuc/dang-xuat` | đã đăng nhập | Menu hồ sơ |
| 🆕 | `GET /xac-thuc/toi` | đã đăng nhập | Layout (tên, avatar, vai trò) |
| 🆕 | `POST /xac-thuc/doi-mat-khau` | đã đăng nhập | Đổi mật khẩu / lần đầu |

```http
POST /api/xac-thuc/dang-nhap
{ "tenDN": "SV20230082", "matKhau": "••••••••", "ghiNho": true }
```
```json
200 OK  (Set-Cookie: ce_refresh=...)
{
  "accessToken": "eyJ...",
  "hetHan": "2026-11-06T02:15:00Z",
  "taiKhoan": {
    "mtk": 15,
    "tenDN": "SV20230082",
    "hoTen": "Nguyễn Minh Long",
    "vaiTro": "THISINH",
    "msts": "SV20230082",
    "phaiDoiMatKhau": false
  }
}
```

- Sai tên đăng nhập hoặc mật khẩu đều trả cùng `401 AUTH_INVALID_CREDENTIALS`.
- Sai 5 lần liên tiếp từ cùng IP ⇒ khóa cặp tên đăng nhập + IP 15 phút, trả `423`
  (cả với tên không tồn tại; D-AUTH-KHOA-01). Sai mật khẩu hiện tại khi đổi mật
  khẩu dùng chung bộ đếm.
- Rate limit `Auth:LoginPermitsPerMinute` (mặc định 60) lần/phút/IP. Mọi lần đăng
  nhập ghi `NHATKYHD`.
- **Request không có trường vai trò.** Backend trả `vaiTro`; frontend tự điều
  hướng `/admin` hoặc `/student` (xem mục 4.3 về nút chọn vai trò).

`/xac-thuc/lam-moi` trả cùng dạng body như đăng nhập. `/xac-thuc/toi` trả object
`taiKhoan`. `/xac-thuc/doi-mat-khau` nhận `{ "matKhauCu": "...", "matKhauMoi": "..." }`.

### 2.2 Tổng quan quản trị — `/quan-tri/tong-quan`

| | Method & path | Màn hình — khối |
|---|---|---|
| 🆕 | `GET /quan-tri/tong-quan` | 4 thẻ thống kê |
| 🆕 | `GET /quan-tri/tong-quan/luot-thi?soNgay=7` | Biểu đồ "Hiệu suất kỳ thi" |
| 🆕 | `GET /ky-thi?trangThai=da_phat_hanh,dang_thi&batDauTu=..&batDauDen=..&kichThuocTrang=5&sapXep=tgBatDau` | "Kỳ thi sắp diễn ra" (dùng lại API kỳ thi) |
| 🆕 | `GET /nhat-ky?kichThuocTrang=5&sapXep=-thoiGian` | "Hoạt động gần đây" |

```json
GET /api/quan-tri/tong-quan
{
  "cauHoi":     { "tong": 2480, "taoTrongThang": 124 },
  "kyThiDangMo": { "tong": 8, "batDauHomNay": 3 },
  "thiSinh":    { "tong": 1284, "taoTrongThang": 48 },
  "hoanThanh":  { "tyLe": 0.926, "tyLeKyTruoc": 0.902 },
  "thoiDiemTinh": "2026-11-06T01:30:00Z"
}
```

`hoanThanh.tyLe` = số `BAILAM` đã nộp/hệ thống thu chia số `PHANCONGTHI` của các
kỳ thi đã kết thúc trong 30 ngày (D-API-04). Frontend tự tính chuỗi "+2.4% so
với tháng trước".

```json
GET /api/quan-tri/tong-quan/luot-thi?soNgay=7
{
  "tongLuotThi": 1842,
  "tyLeThayDoi": 0.125,
  "theoNgay": [ { "ngay": "2026-10-31", "luotThi": 210, "hoanThanh": 198 }, ... ]
}
```

Ngày được gom theo múi giờ `Asia/Ho_Chi_Minh` ở backend.

### 2.3 Danh mục — mức độ, KQHT

| | Method & path | Vai trò | Ghi chú |
|---|---|---|---|
| ✅ | `GET /muc-do` | admin | `[{ "maMD": 1, "tenMD": "Dễ" }]` |
| ✅ | `GET /kqht` | admin | có `soCauHoi` để UI biết có được đổi thứ tự/xóa |
| ✅ | `GET /kqht/{maKQHT}` | admin | |
| ✅ | `POST /kqht` | admin | về sau có thể bỏ `kyHieu` khỏi request vì suy ra từ `thuTu` |
| ✅ | `PUT /kqht/{maKQHT}` | admin | `{ tenKQHT, thuTu, moTa }`; ký hiệu suy ra từ `thuTu`; đã có câu hỏi mà đổi `thuTu` ⇒ 409 `KQHT_IN_USE`; trùng ⇒ 409 `KQHT_KY_HIEU_CONFLICT` |
| ✅ | `PATCH /kqht/{maKQHT}/trang-thai` | admin | `{ "trangThai": "ngung_su_dung" }`, chuyển tự do (D-KQHT-01) |
| ✅ | `DELETE /kqht/{maKQHT}` | admin | 204; đã có câu hỏi hoặc nằm trong ma trận ⇒ 409 `KQHT_IN_USE` |

```json
{ "maKQHT": 1, "thuTu": 1, "kyHieu": "A", "tenKQHT": "Hiểu mô hình quan hệ",
  "moTa": null, "trangThai": "dang_su_dung", "soCauHoi": 312 }
```

### 2.4 Ngân hàng câu hỏi — `/cau-hoi`

| | Method & path | Màn hình |
|---|---|---|
| ✅ | `GET /cau-hoi?tuKhoa&maKQHT&maMD&trangThai&trang&kichThuocTrang&sapXep` | Bảng câu hỏi + bộ lọc |
| ✅ | `GET /cau-hoi/thong-ke` | 4 thẻ mini (tổng, đang dùng, ngừng dùng, số KQHT đang dùng) |
| ✅ | `GET /cau-hoi/{maCH}` | Xem/sửa chi tiết |
| ✅ | `POST /cau-hoi` | Thêm câu hỏi (JSON; thêm ảnh làm ở bước 4) |
| ✅ | `PUT /cau-hoi/{maCH}` | Sửa nội dung (bắt buộc `phienBan`) |
| ✅ | `PATCH /cau-hoi/{maCH}/trang-thai` | Ngừng/khôi phục, chuyển tự do |
| ✅ | `DELETE /cau-hoi/{maCH}` | Xóa cứng khi chưa nằm trong đề, ngược lại 409 `QUESTION_IN_USE` |
| 🆕 | `GET /cau-hoi/{maCH}/hinh-anh/{maTN}` | Ảnh trong nội dung/phương án (stream, `Cache-Control: private`) — bước 4, cùng lưu file |

Query danh sách: `sapXep` nhận **một** trường `maCauHoi` | `ngayCapNhat` | `ngayTao`
(tiền tố `-` giảm dần, mặc định `-ngayCapNhat`); `maCauHoi` sắp theo (thứ tự KQHT,
mức độ, STT) nên `A1-2` đứng trước `A1-10`. `trangThai` nhận chuỗi như JSON.

Phần tử danh sách (không trả phương án để nhẹ bảng):

```json
{
  "maCH": 102, "maCauHoi": "A2-14", "tomTat": "Chuẩn hóa dữ liệu đến dạng chuẩn 3NF...",
  "kqht": { "maKQHT": 1, "thuTu": 1, "kyHieu": "A" },
  "maMD": 2, "trangThai": "dang_su_dung", "coHinhAnh": false,
  "ngayCapNhat": "2026-11-05T03:12:00Z"
}
```

Chi tiết:

```json
{
  "maCH": 102, "maCauHoi": "A2-14", "maKQHT": 1, "maMD": 2,
  "noiDung": "Chuẩn hóa dữ liệu đến dạng chuẩn 3NF nhằm loại bỏ ...",
  "phuongAnA": "...", "phuongAnB": "...", "phuongAnC": "...", "phuongAnD": "...",
  "dapAnDung": "C", "coDinhDapAn": false, "trangThai": "dang_su_dung",
  "hinhAnh": [ { "maTN": 7, "viTri": "NOIDUNG", "thuTu": 1,
                 "url": "/api/cau-hoi/102/hinh-anh/7", "altText": null } ],
  "soDeDaDung": 3, "phienBan": "AAAAAAAAB9E=",
  "ngayTao": "...", "ngayCapNhat": "..."
}
```

- Phương án là bốn trường phẳng `phuongAnA..D` (cùng dạng với preview nhập câu
  hỏi mục 2.5), dùng cho cả response và request.
- `POST` nhận `{ maKQHT, maMD, noiDung, phuongAnA..D, dapAnDung, coDinhDapAn }`, không
  nhận `maCauHoi`/`stt`: backend cấp `STT` kế tiếp theo (KQHT, mức độ) trong
  transaction và trả mã mới. KQHT không tồn tại ⇒ 400 (lỗi trường `maKQHT`); KQHT
  ngừng sử dụng ⇒ 409 `KQHT_INACTIVE`.
- `PUT` nhận nội dung + `phienBan`, **không** nhận `maKQHT`/`maMD` (D-CAUHOI-TT-01).
- Câu đã nằm trong `GOM_CAU` (`soDeDaDung > 0`) vẫn sửa được vì đề dùng bản sao,
  nhưng UI cần cảnh báo; không xóa cứng được.
- `dapAnDung` **chỉ** có ở API quản trị.

### 2.5 Nhập câu hỏi từ Word — `/nhap-cau-hoi`

Luồng hai bước, file chỉ upload một lần:

| | Method & path | Ghi chú |
|---|---|---|
| ✅ | `POST /nhap-cau-hoi/xem-truoc` (multipart `file`) | đối chiếu database; hết lỗi thì có `maXemTruoc` + `hetHan`, backend giữ tệp tạm 30 phút; còn lỗi thì hai trường này `null` |
| ✅ | `POST /nhap-cau-hoi/xem-truoc/{maXemTruoc}/xac-nhan` | 201; ghi `LANIMPORT`, `CAUHOI`, `TAINGUYEN_CAUHOI` + file hình trong một lần (tất cả hoặc không) |
| ✅ | `GET /nhap-cau-hoi?trang&kichThuocTrang` | Lịch sử các lần nhập câu hỏi (`LANIMPORT`), mới nhất trước |
| ✅ | `GET /nhap-cau-hoi/{maLanImport}` | Tổng kết một lần nhập |
| — | `GET /nhap-cau-hoi/{maLanImport}/loi?dinhDang=xlsx` | Bỏ ở P0: không nhập từng phần nên lần nhập thành công không có dòng lỗi (D-IMPORT-TRUNG-01); lỗi xem ngay ở bản xem trước |

Phần tử lịch sử: `{ maLanImport, tenFile, tongDong, soThanhCong, soDongLoi, thoiGian,
trangThai: "hoan_tat", nguoiThucHien }`. Lỗi xác nhận: 404 `IMPORT_PREVIEW_NOT_FOUND`
(không có, đã dùng, hoặc của tài khoản khác), 410 `IMPORT_PREVIEW_EXPIRED`, 409
`QUESTION_IMPORT_HAS_ERRORS` (database đổi từ lúc xem trước). Mã lỗi theo dòng thêm
`DUPLICATE_IN_DATABASE`, `KQHT_NOT_FOUND`, `KQHT_INACTIVE`, `CONTENT_TOO_LONG`,
`ANSWER_TOO_LONG`; cảnh báo `UNSUPPORTED_IMAGE_FORMAT`.

Đổi tên trường của response xem trước:

| Hiện tại | Mới |
|---|---|
| `totalRows`, `validRows`, `errorRows`, `totalMedia`, `rows` | `tongDong`, `soDongHopLe`, `soDongLoi`, `tongHinhAnh`, `dong` |
| `rowNumber`, `questionCode`, `kqhtSymbol`, `difficulty`, `sequenceNumber` | `soDong`, `maCauHoi`, `kyHieu`, `maMD`, `stt` |
| `questionText`, `answerA..D`, `correctAnswer` | `noiDung`, `phuongAnA..D`, `dapAnDung` |
| `stemMarker`, `note`, `media`, `issues`, `isValid` | `danhDauCauDan`, `ghiChu`, `hinhAnh`, `vanDe`, `hopLe` |
| `media[]`: `marker`, `position`, `order`, `fileName`, `contentType` | `maDanhDau`, `viTri`, `thuTu`, `tenFile`, `mimeType` |
| `issues[]`: `code`, `message`, `severity` (`Warning`/`Error`) | `code`, `noiDung`, `mucDo` (`canh_bao`/`loi`) |

- Xác nhận trả `409 QUESTION_IMPORT_HAS_ERRORS` nếu bản xem trước còn dòng lỗi
  (D-IMPORT-01: chỉ ghi khi mọi lỗi chặn đã xử lý), `410 IMPORT_PREVIEW_EXPIRED`
  khi quá hạn.

### 2.6 Thí sinh — `/thi-sinh`

| | Method & path | Màn hình |
|---|---|---|
| ✅ | `GET /thi-sinh?tuKhoa&lop&trangThai&daDoiMatKhau&trang&kichThuocTrang&sapXep` | Bảng thí sinh |
| ✅ | `GET /thi-sinh/lop` | Danh sách lớp cho bộ lọc |
| ✅ | `GET /thi-sinh/{msts}` | Chi tiết (`soLuotThi`, `soLanPhanCong`; danh sách lịch sử thi làm cùng kỳ thi) |
| ✅ | `POST /thi-sinh` | Thêm thí sinh (tạo kèm `TAIKHOAN`: tên đăng nhập = `msts`, mật khẩu mặc định = ngày sinh `ddMMyyyy`) |
| ✅ | `PUT /thi-sinh/{msts}` | Sửa hồ sơ (`msts` không đổi được) |
| ✅ | `PATCH /thi-sinh/{msts}/trang-thai-tai-khoan` | Khóa / mở khóa |
| ✅ | `POST /thi-sinh/{msts}/cap-lai-mat-khau` | Đặt lại về ngày sinh, bật `PHAIDOIMATKHAU`, thu hồi phiên |
| ✅ | `DELETE /thi-sinh/{msts}` | Xóa hồ sơ + tài khoản khi chưa được phân công thi, ngược lại 409 `STUDENT_IN_USE` |
| 🆕 | `GET /nhap-thi-sinh/tep-mau` | Tải tệp mẫu `.xlsx` |
| 🆕 | `POST /nhap-thi-sinh/xem-truoc`, `POST /nhap-thi-sinh/xem-truoc/{maXemTruoc}/xac-nhan`, `GET /nhap-thi-sinh` | Như nhập câu hỏi |

```json
{
  "msts": "SV20230082", "hoTen": "Nguyễn Minh Long",
  "ngaySinh": "2005-03-14", "gioiTinh": "nam", "lop": "K29 - CNPM",
  "noiSinh": null, "cccd": null,
  "trangThaiTaiKhoan": "hoat_dong", "daDoiMatKhau": true, "soLuotThi": 8, "ngayTao": "..."
}
```

`cccd` chỉ trả ở API chi tiết, không trả ở danh sách. `gioiTinh`: `nu` / `nam` / `null`.
Phần tử còn có `daDoiMatKhau` (false khi `PHAIDOIMATKHAU = 1`); danh sách lọc được
bằng `?daDoiMatKhau=false` để nhắc thí sinh chưa đổi mật khẩu mặc định.

Tài khoản thí sinh theo `D-THISINH-01` (`BUSINESS_RULE_DECISIONS.md`). Không API nào
trả mật khẩu, kể cả mật khẩu mặc định; thí sinh tự biết ngày sinh của mình.

### 2.7 Ma trận đề — `/ma-tran`

| | Method & path | Ghi chú |
|---|---|---|
| 🆕 | `GET /ma-tran?trangThai&trang` | |
| 🆕 | `GET /ma-tran/{maMT}` | gồm `cauTruc[]` (KQHT × mức độ) |
| 🆕 | `POST /ma-tran` / `PUT /ma-tran/{maMT}` | ghi `MATRAN` + `CAU_TRUC` cùng transaction, có `phienBan` |
| 🆕 | `GET /ma-tran/{maMT}/do-phu` | số câu khả dụng/yêu cầu từng ô |
| 🆕 | `POST /ma-tran/{maMT}/kich-hoat` · `/ngung-su-dung` | kích hoạt trả 409 `MATRIX_INSUFFICIENT_QUESTIONS` kèm danh sách ô thiếu |

```json
{
  "maMT": 3, "tenMT": "CSDL giữa kỳ", "tongSoCau": 40, "thoiGianLam": 60,
  "trangThai": "dang_soan", "phienBan": "...",
  "cauTruc": [ { "maKQHT": 1, "maMD": 1, "soCau": 6, "tyLe": 15.00 } ]
}
```

### 2.8 Đề gốc, đề con — `/de-goc`

| | Method & path | Ghi chú |
|---|---|---|
| 🆕 | `POST /ma-tran/{maMT}/de-goc` | sinh `DEGOC` + `GOM_CAU` (+ ảnh); body `{ "maDeGocHienThi": "CSDL-GK-01", "hatGiong": null }` |
| 🆕 | `GET /de-goc?maMT&chuaGanKyThi=true` | `chuaGanKyThi` để chọn đề cho kỳ thi |
| 🆕 | `GET /de-goc/{maDe}` | xem đề gốc kèm đáp án (admin) |
| 🆕 | `POST /de-goc/{maDe}/de-con` | `{ "soLuong": 4 }` sinh `DECON` + `HOAN_VI` |
| 🆕 | `GET /de-goc/{maDe}/de-con/{maDeCon}` | xem/in thử đề con |

### 2.9 Kỳ thi — `/ky-thi`

| | Method & path | Màn hình |
|---|---|---|
| 🆕 | `GET /ky-thi?tuKhoa&trangThai&batDauTu&batDauDen&trang&kichThuocTrang&sapXep` | Bảng kỳ thi, "sắp diễn ra" ở dashboard |
| 🆕 | `GET /ky-thi/{maKT}` | Chi tiết |
| 🆕 | `POST /ky-thi` / `PUT /ky-thi/{maKT}` | Tạo/sửa nháp, có `phienBan` |
| 🆕 | `PUT /ky-thi/{maKT}/de-goc` | `{ "maDe": 9 }` — chỉ khi chưa phân công |
| 🆕 | `GET /ky-thi/{maKT}/thi-sinh?tuKhoa&trangThaiBaiLam&trang` | Danh sách thí sinh của kỳ thi |
| 🆕 | `POST /ky-thi/{maKT}/thi-sinh` | `{ "dsMsts": [...] }` — gán đề con theo `cachPhanDe` |
| 🆕 | `DELETE /ky-thi/{maKT}/thi-sinh/{msts}` | chỉ khi `nhap` |
| 🆕 | `GET /ky-thi/{maKT}/kiem-tra-phat-hanh` | danh sách điều kiện phát hành còn thiếu |
| 🆕 | `POST /ky-thi/{maKT}/phat-hanh` · `/huy` | 409 `EXAM_NOT_READY` / `EXAM_INVALID_STATE` |
| 🆕 | `GET /ky-thi/{maKT}/giam-sat` | khối "Kỳ thi đang diễn ra", phòng giám sát (poll 10 giây) |
| 🆕 | `POST /ky-thi/{maKT}/bai-lam/{maBL}/cong-gio` | `{ "soPhut": 5, "lyDo": "..." }`, có nhật ký |
| 🆕 | `POST /ky-thi/{maKT}/cong-bo-ket-qua` | D-CONGBO-01: chỉ khi `da_ket_thuc`, ghi `NGAYCONGBO`/`MTKCONGBO` + nhật ký; công bố rồi ⇒ 409 `EXAM_RESULT_ALREADY_PUBLISHED`; chưa kết thúc ⇒ 409 `EXAM_INVALID_STATE` |

Bộ lọc: danh sách kỳ thi lọc theo `trangThai` và `daCongBo`; danh sách thí sinh của kỳ
thi lọc `trangThaiThiSinh` theo D-TRANGTHAI-TS-01 (`chua_thi`, `dang_thi`, `da_nop`,
`he_thong_thu`, `vang_thi`).

Phần tử danh sách:

```json
{
  "maKT": 12, "tenKT": "Cơ sở dữ liệu nâng cao", "monThi": "Cơ sở dữ liệu",
  "tgBatDau": "2026-11-06T02:00:00Z", "thoiLuong": 60,
  "trangThai": "dang_thi", "soThiSinh": 120, "soDaNop": 18,
  "daGanDe": true
}
```

Chi tiết thêm: `soPhutChoVaoTre`, `hienDiem`, `choXemLai`, `cachPhanDe`,
`deGoc { maDe, maDeGocHienThi, soDeCon }`, `ngayPhatHanh`, `phienBan`.

Giám sát:

```json
{
  "maKT": 12, "trangThai": "dang_thi", "gioServer": "2026-11-06T02:25:32Z",
  "ketThucLuc": "2026-11-06T03:00:00Z",
  "soLuong": { "duocPhanCong": 120, "chuaVao": 11, "dangLam": 91, "daNop": 18, "heThongThu": 0 }
}
```

### 2.10 Kết quả, nhật ký

| | Method & path | Vai trò |
|---|---|---|
| 🆕 | `GET /ky-thi/{maKT}/ket-qua?tuKhoa&trang&sapXep=-diem` | admin |
| 🆕 | `GET /ky-thi/{maKT}/ket-qua/xuat?dinhDang=xlsx\|csv` | admin |
| 🆕 | `GET /nhat-ky?nhom&mtk&tu&den&trang` | admin |

```json
{ "maNhatKy": 801, "thoiGian": "2026-11-06T01:18:00Z",
  "nguoiThucHien": { "mtk": 1, "hoTen": "Trần Minh Hoàng" },
  "nhom": "KYTHI", "hanhDong": "Tạo kỳ thi", "doiTuong": "Lập trình hướng đối tượng" }
```

### 2.11 Cổng thí sinh — `/toi`

Menu theo yêu cầu (09/11/2026): **Trang chủ** (`/candidate`) · **Thi trực tuyến** gồm
*Tham gia thi* (`/candidate/thi/tham-gia`) và *Kết quả thi* (`/candidate/thi/ket-qua`) ·
**Tài khoản cá nhân** (`/candidate/tai-khoan`). Hai trang Thi trực tuyến hiện là khung,
có dữ liệu khi làm xong kỳ thi/bài làm (bước 6–7).

| | Method & path | Màn hình |
|---|---|---|
| ✅ | `GET /toi/ho-so` | Tài khoản cá nhân: hồ sơ của chính thí sinh (không có trạng thái khóa, số lần phân công) |
| 🆕 | `GET /toi/tong-quan` | Thẻ "Tổng quan học kỳ" (điểm TB, đã thi, sắp thi) |
| 🆕 | `GET /toi/ky-thi?phamVi=sap_toi\|da_thi&trang` | "Kỳ thi sắp diễn ra", "Lịch thi" |
| 🆕 | `GET /toi/ky-thi/{maKT}` | Thẻ kỳ thi tiếp theo, trang chờ vào thi |
| 🆕 | `GET /toi/ket-qua?trang` | "Kết quả gần đây", trang Kết quả |

```json
GET /api/toi/ky-thi?phamVi=sap_toi
{
  "gioServer": "2026-11-06T01:35:44Z",
  "duLieu": [{
    "maKT": 12, "tenKT": "Cơ sở dữ liệu nâng cao", "monThi": "Cơ sở dữ liệu",
    "tgBatDau": "2026-11-06T02:00:00Z", "thoiLuong": 60, "soCau": 40,
    "soMay": "A-17",
    "moCongLuc": "2026-11-06T02:00:00Z", "dongCongLuc": "2026-11-06T02:15:00Z",
    "trangThaiBaiLam": "chua_vao", "duocVaoThi": false
  }]
}
```

`gioServer` để frontend đếm ngược theo đồng hồ server (tính độ lệch một lần),
không tin đồng hồ máy thí sinh. `duocVaoThi` do backend quyết định.

`/toi/ky-thi` chỉ trả kỳ thi đã phát hành (1–3) mà thí sinh được phân công.

Kết quả (D-CONGBO-01): kỳ thi chưa công bố không xuất hiện ở `/toi/ket-qua`. Đã công
bố thì trả `diem`/`soCauDung` khi `hienDiem = true`; ngược lại trả `null` kèm
`anDiem: true` và chỉ hiện trạng thái bài (`da_nop`, `he_thong_thu`, `vang_thi`).

### 2.12 Làm bài — `/toi/ky-thi/{maKT}/bai-lam`

Thiết kế cho mạng chập chờn: mọi lệnh ghi đều idempotent.

| | Method & path | Ghi chú |
|---|---|---|
| 🆕 | `POST /toi/ky-thi/{maKT}/bai-lam` | Bắt đầu **hoặc** tiếp tục; một `BAILAM` mỗi phân công |
| 🆕 | `GET /toi/ky-thi/{maKT}/bai-lam` | Đề đã hoán vị + câu trả lời đã lưu + thời gian còn lại |
| 🆕 | `PUT /toi/ky-thi/{maKT}/bai-lam/tra-loi` | Lưu lô thay đổi (tự lưu) |
| 🆕 | `POST /toi/ky-thi/{maKT}/bai-lam/nop` | Nộp bài |
| 🆕 | `GET /toi/ky-thi/{maKT}/bai-lam/hinh-anh/{maHinh}` | Ảnh câu hỏi, chỉ trong thời gian làm bài |

```json
GET /api/toi/ky-thi/12/bai-lam
{
  "maBL": 5531, "trangThai": "dang_lam",
  "gioServer": "2026-11-06T02:25:32Z",
  "hanNop": "2026-11-06T03:00:00Z",
  "kyThi": { "tenKT": "Cơ sở dữ liệu nâng cao", "tenDeCon": "102" },
  "cauHoi": [
    { "ttCau": 1, "noiDung": "Trong mô hình CSDL quan hệ, ...",
      "phuongAn": [ { "nhan": "A", "noiDung": "..." }, { "nhan": "B", "noiDung": "..." },
                    { "nhan": "C", "noiDung": "..." }, { "nhan": "D", "noiDung": "..." } ],
      "hinhAnh": [] }
  ],
  "traLoi": [ { "ttCau": 2, "dapAnChon": "C", "danhDau": false },
              { "ttCau": 3, "dapAnChon": null, "danhDau": true } ]
}
```

- Câu hỏi định danh bằng **`ttCau`** (`HOAN_VI.TTCAU`), không lộ `MACH` của ngân hàng.
- Phương án đã được hoán vị theo `TTDAPAN`; **không** có đáp án đúng, mức độ, KQHT
  hay mã câu hỏi.

```json
PUT /api/toi/ky-thi/12/bai-lam/tra-loi
{ "soThuTuLo": 47,
  "thayDoi": [ { "ttCau": 4, "dapAnChon": "B" }, { "ttCau": 3, "danhDau": false } ] }
→ 200 { "loDaLuu": 47, "luuLuc": "2026-11-06T02:25:40Z", "hanNop": "2026-11-06T03:00:00Z" }
```

- Frontend gom thay đổi, gửi mỗi 3–5 giây hoặc khi đổi câu; `soThuTuLo` tăng
  dần, backend bỏ qua lô có số ≤ lô đã lưu (chống lô cũ đến sau ghi đè).
- Hết giờ (theo server) ⇒ `409 ATTEMPT_CLOSED`; frontend chuyển sang màn kết thúc.
- `hanNop` = `min(TGBATDAU bài + THOILUONG + PHUTCONGTHEM, giờ kết thúc chung)`
  tùy quyết định "vào muộn" còn mở trong kế hoạch.
- `nop` gửi kèm lô thay đổi cuối (`{ "soThuTuLo": 52, "thayDoi": [...] }`) để
  không mất câu vừa chọn; gọi lại lần hai trả kết quả của lần đầu.

### 2.13 Health

✅ `GET /health`, ✅ `GET /health/database` — giữ nguyên.

---

## 3. Đối chiếu giao diện ↔ database: các chỗ lệch

Giao diện hiện hiển thị một số dữ liệu **không có trong schema**. Mỗi dòng cần
một quyết định: bổ sung schema (migration mới) hoặc bỏ khỏi UI.

| Màn hình | Dữ liệu UI | Hiện trạng DB | Đề xuất |
|---|---|---|---|
| Ngân hàng câu hỏi | Trạng thái "Bản nháp" | `CAUHOI.TRANGTHAI` chỉ 0/1 | **Bỏ "Bản nháp"** khỏi UI (P0 không có luồng duyệt); thẻ "BẢN NHÁP" đổi thành "NGỪNG SỬ DỤNG" |
| Ngân hàng câu hỏi | Mã `A1-001`, `B3-008` | CHECK bắt buộc `A1-1` (không số 0 đầu) | Dữ liệu mẫu đổi thành `A1-1`, `B3-8` |
| Ngân hàng câu hỏi | "Trắc nghiệm · 4 phương án" | luôn đúng ở P0 | Giữ dạng tĩnh, hoặc hiện "Có hình" từ `coHinhAnh` |
| Kỳ thi, dashboard, cổng SV | "Phòng A203", "Trực tuyến" | Không có bảng phòng; chỉ `PHANCONGTHI.SOMAY` | Bỏ phòng ở mức kỳ thi; cổng SV hiện **số máy** (`soMay`). Nếu bắt buộc có phòng ⇒ migration thêm `KYTHI.DIADIEM nvarchar(100) NULL` |
| Cổng SV | "Mã học phần CSE304" | `KYTHI.MONTHI` chỉ là tên | Hiện `monThi`; bỏ mã học phần hoặc thêm cột `MAHOCPHAN` |
| Kỳ thi | Bộ lọc "Học kỳ I", "Học kỳ I · 2026–2027" | Không có học kỳ | Thay bằng bộ lọc khoảng ngày + trạng thái; tiêu đề học kỳ tính từ ngày |
| Kỳ thi | Cột "HOÀN THÀNH %" cho kỳ chưa thi (100%, 92%) | — | Định nghĩa = `soDaNop / soThiSinh`; kỳ chưa diễn ra hiển thị "—" |
| Kỳ thi | Trạng thái "Sắp diễn ra" và "Đã phát hành" song song | Chỉ có `da_phat_hanh` | Gộp: `da_phat_hanh` + `tgBatDau` > now ⇒ "Sắp diễn ra"; thêm hiển thị "Nháp", "Đã hủy" |
| Thí sinh | Email `@duytan.edu.vn` | Không có email | Bỏ email; dòng phụ hiện ngày sinh hoặc tên đăng nhập |
| Thí sinh | "K29 - Công nghệ phần mềm", lọc "khóa học" | Chỉ `THISINH.LOP` | Lọc theo `lop` từ `GET /thi-sinh/lop` |
| Cổng SV | Xếp loại "Giỏi/Khá", vòng 82% | — | Tính ở frontend từ điểm; cần chốt thang xếp loại |
| Topbar, cổng SV | Chuông thông báo có chấm đỏ | Không có bảng thông báo | Ẩn chấm đỏ hoặc ẩn nút cho tới khi có tính năng (ngoài P0) |
| Đăng nhập | "Quên mật khẩu?" | Không có email/SMS (ngoài phạm vi) | Đổi thành chú thích "Liên hệ quản trị viên để cấp lại mật khẩu" |
| Đăng nhập | "12K+ lượt dự thi, 99.9%, 24/7" | — | Nội dung giới thiệu tĩnh, không gọi API |
| Làm bài | "MỨC ĐỘ: KHÓ" | `GOM_CAU` không lưu mức độ | **Bỏ** — không nên lộ mức độ cho thí sinh |

---

## 4. Kiến trúc tích hợp phía frontend

### 4.1 Thư viện bổ sung (đã có trong kế hoạch)

- `@tanstack/react-query` — cache server state, retry, polling (giám sát).
- `openapi-typescript` (dev) — sinh `src/types/api.gen.ts` từ OpenAPI của
  backend để kiểu dữ liệu không lệch hợp đồng.
- `react-hook-form` + `zod` — form tạo/sửa.

### 4.2 Cấu trúc thư mục

```
src/
  services/
    httpClient.ts      # fetch wrapper: auth header, làm mới phiên, ProblemDetails, FormData
    authSession.ts     # giữ access token trong bộ nhớ, single-flight refresh
    queryClient.ts
  types/
    api.gen.ts         # sinh tự động, không sửa tay
    api.ts             # TrangDuLieu<T>, ProblemDetails, alias tiện dùng
  features/
    auth/          api.ts  hooks.ts  RequireRole.tsx
    question-bank/ api.ts  hooks.ts  (useCauHoi, useThongKeCauHoi, ...)
    exams/ ...     candidates/ ...     exam-taking/ (useBaiLam, useTuDongLuu)
```

Mỗi feature có `api.ts` (hàm gọi HTTP thuần) và `hooks.ts` (bọc React Query với
query key thống nhất, ví dụ `["cau-hoi", boLoc]`).

### 4.3 Thay đổi cần làm trong code hiện có

1. **`httpClient.ts`**
   - Đang `JSON.stringify` mọi body ⇒ upload `FormData` (nhập DOCX/XLSX) sẽ hỏng.
     Phải bỏ qua stringify và không đặt `Content-Type` khi body là `FormData`.
   - Parse lỗi thành `ProblemDetails` (`code`, `errors`, `traceId`) thay vì chuỗi.
   - Gắn `Authorization: Bearer`; gặp 401 thì gọi `/xac-thuc/lam-moi` **một lần**
     (dùng chung một promise cho các request đồng thời) rồi thử lại; làm mới thất
     bại ⇒ xóa phiên, về `/login`.
   - `credentials: "include"` cho các lệnh `/xac-thuc/*`.
   - Hỗ trợ `signal` để hủy request khi rời trang; tải file (`blob`) cho xuất kết quả.
2. **Route guard:** bọc `/admin` bằng `RequireRole vaiTro="ADMIN"`, `/candidate` và
   `/exam` bằng `RequireRole vaiTro="THISINH"`. Khi tải trang lần đầu gọi
   `/xac-thuc/lam-moi` để khôi phục phiên trước khi render.
3. **Đăng nhập:** nút chọn "Quản trị viên / Thí sinh" chỉ còn tác dụng gợi ý
   placeholder; điều hướng theo `taiKhoan.vaiTro` server trả về. Bỏ `defaultValue`
   mật khẩu `campusexam123`.
4. **Layout:** tên, avatar chữ cái, mã thí sinh lấy từ `useToi()` thay vì cứng
   "Nguyễn Thảo", "Minh Long".
5. **Thời gian:** helper `formatDateTime(utc)` dùng `Intl.DateTimeFormat("vi-VN",
   { timeZone: "Asia/Ho_Chi_Minh" })`; đồng hồ đếm ngược dùng `gioServer` để
   tính độ lệch.
6. **Màn làm bài:** state đáp án lấy từ `GET bai-lam`; hàng đợi thay đổi lưu thêm
   vào `sessionStorage` để không mất khi tải lại trang; hook `useTuDongLuu` gửi
   lô theo mục 2.12 và cập nhật nhãn "Đã tự động lưu" theo `luuLuc` thật.

URL trang phía frontend (`/admin/questions`, `/candidate`...) không thuộc hợp đồng
API; đổi sang tiếng Việt hay không là quyết định riêng của giao diện.

---

## 5. Triển khai phía backend

- **Route:** khai báo tường minh bằng `[Route("api/cau-hoi")]`, không dùng
  `[controller]` token (tên lớp C# có thể khác đường dẫn).
- **OpenAPI:** dùng `Microsoft.AspNetCore.OpenApi` có sẵn trong .NET 10
  (`builder.Services.AddOpenApi(); app.MapOpenApi();` chỉ ở Development); frontend
  chạy `npx openapi-typescript https://localhost:7000/openapi/v1.json -o
  src/types/api.gen.ts`.
- **JSON enum dạng chuỗi:** enum C# riêng cho API, mỗi giá trị gắn
  `[JsonStringEnumMemberName("dang_thi")]` (có từ .NET 9) + `JsonStringEnumConverter`;
  không lộ số của DB.
- **Mã lỗi:** gom vào một lớp `ErrorCodes` + exception nghiệp vụ
  (`NotFoundException`, `ConflictException`, `ConcurrencyException`) được
  `IExceptionHandler` đổi thành `ProblemDetails` — thay cho việc tự dựng
  `ProblemDetails` trong từng controller như `KqhtController` hiện nay.
- **Xác thực:** `Microsoft.AspNetCore.Authentication.JwtBearer`; khóa ký đặt trong
  User Secrets (`Auth:SigningKey`), không commit. `OnTokenValidated` so `sstamp`
  với `MABAOMAT` bằng truy vấn theo khóa chính, cache `Auth:TokenCheckCacheSeconds`
  (mặc định 30 giây, 0 = tắt). Đổi mật khẩu xóa cache ngay (D-AUTH-KHOA-01).
- **Rate limit:** `AddRateLimiter` — policy `dang-nhap` (`Auth:LoginPermitsPerMinute`,
  mặc định 60/phút/IP), `tu-dong-luu` (60/phút/bài làm).
- **Reverse proxy:** `UseForwardedHeaders` đứng đầu pipeline; chỉ tin
  `X-Forwarded-For` từ loopback và proxy khai báo ở `ReverseProxy:KnownProxies`/
  `KnownNetworks` (một bước).
- **CORS:** không cần khi dev (proxy cùng origin). Khi triển khai khác origin mới
  bật allowlist, và lúc đó cookie refresh phải đổi `SameSite=None; Secure`.
- **Kiểm thử hợp đồng:** mỗi controller có integration test khẳng định đường
  dẫn, tên trường JSON, mã lỗi và policy (ví dụ "thí sinh gọi API quản trị ⇒
  403", "thí sinh lấy đề ⇒ không có `dapAnDung`").

---

## 6. Thứ tự triển khai đề xuất

| Bước | Nội dung | Mở khóa màn hình |
|---|---|---|
| 1 | Chuẩn hóa nền: OpenAPI, enum chuỗi, `ErrorCodes`, `TrangDuLieu<T>`; đổi `/question-imports/preview` → `/nhap-cau-hoi/xem-truoc` và tên trường; `trangThai` KQHT sang chuỗi; nâng cấp `httpClient` | — |
| 2 | Xác thực: đăng nhập/làm mới/đăng xuất/tôi/đổi mật khẩu, policy, route guard | Đăng nhập, layout |
| 3 | KQHT CRUD + danh sách/chi tiết/tạo/sửa câu hỏi + thống kê | Ngân hàng câu hỏi |
| 4 | Xác nhận nhập câu hỏi + lịch sử; thí sinh CRUD + nhập Excel | Nhập từ Word/Excel, Thí sinh |
| 5 | Ma trận, sinh đề | Ma trận & đề thi |
| 6 | Kỳ thi, phân công, phát hành, giám sát; tổng quan; nhật ký | Kỳ thi, Tổng quan |
| 7 | `/toi/*` và engine làm bài | Cổng thí sinh, Làm bài |
| 8 | Kết quả, xuất file | Báo cáo |

## 7. Quyết định

| Mã | Câu hỏi | Trạng thái |
|---|---|---|
| D-API-01 | Ngôn ngữ đường dẫn API | **Đã chốt 06/11/2026:** tiếng Việt không dấu, kebab-case; trường JSON camelCase không dấu theo tên cột DB |
| D-API-02 | Có quản lý phòng thi không | **Đã chốt 06/11/2026:** không ở P0; dùng `SOMAY` |
| D-API-03 | Trạng thái nháp cho câu hỏi | **Đã chốt 06/11/2026:** không; bỏ khỏi UI |
| D-API-04 | Định nghĩa "tỷ lệ hoàn thành" ở dashboard | **Đã chốt 06/11/2026:** `đã nộp / được phân công`, kỳ đã kết thúc trong 30 ngày |
| D-API-05 | Thời hạn access/refresh token | **Đã chốt 06/11/2026:** 15 phút / 12 giờ (7 ngày nếu ghi nhớ) |

## 8. Tiến độ triển khai

### Bước 1 — xong (06/11/2026)

- `TrangThaiSuDung` trả/nhận chuỗi `dang_su_dung`/`ngung_su_dung`, từ chối số
  (`EnumChuoiConverter`); KQHT mới mặc định `dang_su_dung`.
- `TrangDuLieu<T>`, `TruyVanTrang` (chưa endpoint nào dùng — dành cho bước 3).
- `ErrorCodes`, `ApiProblem` (ProblemDetails có `code` + `traceId`);
  `ValidationProblemFactory`: lỗi 400 có `code = VALIDATION_FAILED`, khóa `errors`
  camelCase trùng tên trường JSON, lỗi đọc JSON báo "Giá trị không hợp lệ." và
  không lộ tên kiểu .NET.
- `POST /api/nhap-cau-hoi/xem-truoc` thay `/api/question-imports/preview`; trường
  JSON tiếng Việt như mục 2.5. **Chưa có** `maXemTruoc`/`hetHan` — làm cùng bước
  xác nhận nhập (bước 4) vì cần lưu tệp tạm.
- OpenAPI tại `/openapi/v1.json` (chỉ Development).
- Frontend: `httpClient` mới (FormData, ProblemDetails → `ApiError`, Bearer token,
  làm mới phiên single-flight khi 401, `NETWORK_ERROR`, tải blob, query builder),
  `authSession`, `types/api.ts`, `features/learning-outcomes/api.ts`,
  `features/question-bank/importApi.ts`; Vitest 4.1.12, `npm test`.

### Bước 2 — xong (06/11/2026)

Backend:

- `POST /api/xac-thuc/dang-nhap`, `lam-moi`, `dang-xuat`, `doi-mat-khau`,
  `GET /api/xac-thuc/toi` theo mục 2.1. `dang-xuat` không cần access token (chỉ
  cookie + `X-CSRF`) để vẫn đăng xuất được khi token đã hết hạn. `doi-mat-khau`
  trả phiên mới (body như đăng nhập + cookie mới) vì mọi phiên cũ bị thu hồi.
- Refresh token 256 bit, lưu SHA-256; xoay vòng mỗi lần làm mới với cập nhật có
  điều kiện (hai request cùng token chỉ một thắng). Token đã xoay vòng bị dùng lại
  sau 30 giây ⇒ thu hồi mọi phiên của tài khoản; trong 30 giây (nhiều tab cùng làm
  mới) chỉ trả 401. Thời hạn phiên trượt theo thời hạn ban đầu (12 giờ hoặc 7 ngày).
- Khóa tạm 15 phút sau 5 lần sai theo cặp tên đăng nhập + IP (bảng `KHOADANGNHAP`,
  D-AUTH-KHOA-01); tên đăng nhập không tồn tại vẫn băm mật khẩu giả để thời gian
  phản hồi giống sai mật khẩu. Rate limit 60 lần/phút/IP (cấu hình được).
- Middleware chặn `403 AUTH_PASSWORD_CHANGE_REQUIRED` khi token có `doi_mk = 1`,
  trừ controller `/api/xac-thuc`. Fallback policy bắt buộc đăng nhập; `AdminOnly`
  áp cho `/api/kqht`, `/api/muc-do`, `/api/nhap-cau-hoi`; `/api/health` và OpenAPI
  ẩn danh. 401/403 đều trả ProblemDetails có `code`.
- Ánh xạ EF bảng `THISINH`; `ChinhSachMatKhau` dùng chung (bootstrap admin và đổi
  mật khẩu), kiểm tra thêm "không chứa ngày sinh" với thí sinh.
- Ghi `NHATKYHD` nhóm `XACTHUC`: đăng nhập (thành công/thất bại kèm lý do), tạm
  khóa, đăng xuất, đổi mật khẩu, thu hồi mọi phiên — đều có IP.
- Cấu hình `Auth` (`AuthOptions`): `SigningKey` trong User Secrets;
  `AllowedOrigins` trong `appsettings.Development.json`.

Frontend:

- `AuthProvider` (khôi phục phiên bằng cookie khi tải trang, handler làm mới cho
  `httpClient`, đăng xuất khi phiên hết hạn), `RequireRole` cho `/admin` (ADMIN),
  `/candidate` và `/exam` (THISINH); tài khoản phải đổi mật khẩu luôn bị đưa về
  `/doi-mat-khau`.
- Trang đăng nhập gọi API thật, hiển thị lỗi theo `code`, có "Ghi nhớ đăng nhập";
  nút chọn vai trò chỉ đổi gợi ý nhập liệu. Thêm trang đổi mật khẩu.
- Layout và lời chào dùng tên thật; có nút đổi mật khẩu và đăng xuất.

Chưa làm (để bước sau): quản trị tài khoản (khóa/mở, cấp lại mật khẩu) đi cùng
CRUD thí sinh ở bước 4.

### Bước 3 — xong (09/11/2026)

Theo D-KQHT-01 (phương án A), D-TRANGTHAI-01, D-CAUHOI-TT-01 chốt cùng ngày.

Backend:

- KQHT: `PUT`, `PATCH trang-thai`, `DELETE`; `GET` trả thêm `soCauHoi`. Sửa dùng
  `ExecuteUpdate` có điều kiện "chưa có câu hỏi" ngay trong câu lệnh; khóa ngoại
  `(MAKQHT, KYHIEU)` của `CAUHOI` chặn tiếp nếu vẫn lọt.
- Ánh xạ EF `CAUHOI` (`PHIENBAN` là `rowversion`) và `GOM_CAU` (chỉ khóa, để đếm
  `soDeDaDung`).
- `/api/cau-hoi`: danh sách phân trang (`TrangDuLieu<T>`), thống kê, chi tiết, thêm,
  sửa, đổi trạng thái, xóa. Cấp `STT` trong transaction serializable với `UPDLOCK,
  HOLDLOCK`, thử lại tối đa 3 lần khi trùng khóa — test 8 request đồng thời nhận đủ
  `A1-1..A1-8`.
- Lỗi nghiệp vụ trả qua `KetQua<T>`/`LoiNghiepVu` và `ApiProblem.FromLoi` thay cho
  dựng ProblemDetails trong từng nhánh controller. Mã lỗi mới: `KQHT_IN_USE`,
  `KQHT_INACTIVE`, `QUESTION_NOT_FOUND`, `QUESTION_IN_USE`, `CONCURRENCY_CONFLICT`.
- 25 integration test HTTP mới (`NganHangCauHoiApiTests`).

Frontend:

- Màn KQHT: cột số câu hỏi, sửa (khóa thứ tự khi đã có câu hỏi), ngừng dùng/khôi
  phục, xóa (tắt khi đã có câu hỏi); mã mẫu đổi thành `A1-1`.
- Màn ngân hàng câu hỏi bỏ dữ liệu mẫu, nối API thật: thẻ thống kê, tìm kiếm (chờ
  300 ms), lọc KQHT/mức độ/trạng thái, sắp xếp, phân trang, form thêm/sửa (chọn đáp
  án đúng, cố định phương án, cảnh báo câu đã nằm trong đề, báo xung đột phiên bản),
  ngừng dùng/khôi phục, xóa. Bỏ trạng thái "Bản nháp".
- `ConfirmDialog` dùng chung; Vitest cho `questionForm`.

Chưa làm: ảnh trong câu hỏi (thêm/sửa/hiển thị) — cần lưu file, làm ở bước 4.

### Bước 4 — đang làm

Xong (09/11/2026) — thí sinh và tài khoản thí sinh:

- `/api/thi-sinh` theo mục 2.6, thêm `DELETE`. Thêm thí sinh tạo `TAIKHOAN` +
  `THISINH` trong một transaction; MSTS trùng với thí sinh **hoặc** tài khoản bất kỳ
  (kể cả admin) ⇒ 409 `STUDENT_MSTS_CONFLICT`; CCCD trùng ⇒ 409 `STUDENT_CCCD_CONFLICT`.
- Khóa tài khoản và cấp lại mật khẩu đổi `MABAOMAT`, thu hồi mọi phiên và xóa cache
  kiểm tra token (`AuthCacheKeys`) nên access token cũ bị từ chối ngay; ghi `NHATKYHD`
  nhóm `THISINH` kèm IP. Mở khóa không đổi mật khẩu.
- Ánh xạ EF tối thiểu `PHANCONGTHI`, `BAILAM` để đếm `soLanPhanCong`/`soLuotThi`.
- Frontend: màn quản lý thí sinh nối API thật (tìm, lọc lớp/trạng thái/chưa đổi mật
  khẩu, phân trang, thêm/sửa, khóa/mở, cấp lại mật khẩu, xóa).
- 17 integration test HTTP (`ThiSinhApiTests`), Vitest cho `studentForm`.

Xong (09/11/2026) — ghi kết quả nhập câu hỏi và ảnh (D-IMPORT-TRUNG-01, D-MEDIA-01):

- `IFileStorage` + `LocalFileStorage` (thư mục `Storage:Root`, mặc định
  `App_Data/storage`): key do backend sinh, kiểm tra chặt (không `..`), không ghi đè.
- Parser có `ReadAsync` trả kèm byte hình; preview đối chiếu database (mã trùng, KQHT
  thiếu/ngừng dùng, độ dài cột). Hết lỗi thì lưu tệp tạm `tam/nhap-cau-hoi/` 30 phút;
  tệp bỏ dở quá một ngày được dọn khi có lần xem trước mới.
- Xác nhận: đọc lại tệp, kiểm tra lại, lưu hình (`cau-hoi/yyyy/MM/{guid}.ext`) rồi ghi
  `LANIMPORT` + `CAUHOI` + `TAINGUYEN_CAUHOI` + `NHATKYHD` trong một transaction; lỗi
  trước commit thì xóa các file vừa lưu. Xóa câu hỏi xóa cả file hình sau khi commit.
- `GET /cau-hoi/{maCH}/hinh-anh/{maTN}`: `Cache-Control: private, immutable`,
  `nosniff`, CSP `sandbox`. Frontend tải ảnh dạng blob vì cần access token.
- Frontend: trang `/admin/questions/import` (chọn tệp, bảng kiểm tra từng dòng, xác
  nhận, lịch sử); form sửa câu hỏi hiển thị hình và mã vị trí.
- 12 integration test mới (`NhapCauHoiApiTests`, parser `ReadAsync`/cảnh báo EMF).

Còn lại: nhập thí sinh `.xlsx`.
