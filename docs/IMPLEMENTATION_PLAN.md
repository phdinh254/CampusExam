# Kế hoạch hoàn thiện đồ án CampusExam

**Cập nhật:** 08/11/2026 (tuần 2)
**Mô hình ước lượng:** 2 lập trình viên, 12 tuần (02/11/2026 - 24/01/2027); nếu 1
lập trình viên, dự kiến 18-22 tuần.
**Ký hiệu:** [x] đã xong · [~] đang làm/làm một phần · [ ] chưa làm.

## 1. Mục tiêu hoàn thành

CampusExam hoàn chỉnh khi quản trị viên chuẩn bị được toàn bộ kỳ thi và thí sinh
dự thi được từ đầu đến cuối:

1. Quản trị viên đăng nhập, quản lý tài khoản và thí sinh.
2. Quản lý KQHT, mức độ và ngân hàng câu hỏi.
3. Nhập câu hỏi (`.docx`) và thí sinh (`.xlsx`), xem lỗi từng dòng trước khi ghi.
4. Tạo ma trận đề, kiểm tra đủ câu, sinh đề gốc và các đề con hoán vị.
5. Tạo kỳ thi, gắn đề, phân công thí sinh, phát hành và theo dõi.
6. Thí sinh vào thi đúng giờ, làm bài, tự lưu, nộp hoặc được tự thu bài.
7. Chấm điểm chính xác, hiển thị theo cấu hình, tra cứu và xuất kết quả.
8. Có nhật ký thao tác, kiểm thử, tài liệu, quy trình triển khai và sao lưu.

## 2. Hiện trạng

### Đã có

- Backend .NET 10 bốn lớp `Domain`, `Application`, `Infrastructure`, `Api`;
  `ProblemDetails` và xử lý exception tập trung.
- Git + GitHub (`phdinh254/CampusExam`).
- API: `GET /api/muc-do`; `GET /api/kqht`, `GET /api/kqht/{id}`, `POST /api/kqht`
  (ký hiệu phải khớp thứ tự KQHT, trùng ký hiệu trả 409);
  `POST /api/nhap-cau-hoi/xem-truoc` đọc DOCX (chặn zip bomb), tách câu hỏi/phương
  án/hình và báo lỗi từng dòng; `/api/xac-thuc/*` đăng nhập, làm mới, đăng xuất,
  `toi`, đổi mật khẩu.
- Database hoàn chỉnh 22 bảng (`database/CampusExamDB.sql`) và migration 01-04 đã
  chạy trên database phát triển:
  - khóa chính clustered, CHECK cho mọi cột trạng thái, `rowversion` cho câu hỏi,
    ma trận, kỳ thi, bài làm;
  - mã câu hỏi `[KyHieu][MaMD]-[STT]` được database bảo đảm;
  - khóa ngoại kép bảo đảm đề con thuộc đề gốc của kỳ thi, câu trả lời thuộc đề
    con được giao;
  - bảng phiên đăng nhập (refresh token), bản sao hình ảnh của đề, seed ba mức độ.
- Lệnh `bootstrap-admin` tạo tài khoản quản trị đầu tiên (mật khẩu nhập lúc chạy,
  băm bằng ASP.NET Core Identity, có ghi nhật ký).
- Xác thực JWT + refresh cookie, policy theo vai trò, rate limit, khóa tạm theo tên
  đăng nhập + IP, OpenAPI ở Development.
- Frontend: giao diện ba không gian (quản trị, thí sinh, phòng thi), đăng nhập, đổi
  mật khẩu, route guard; màn hình KQHT nối API thật, các màn hình khác còn dữ liệu mẫu.
- 141 test tự động (45 unit, 66 integration backend trên SQL Server thật; 30 Vitest
  frontend) chạy thành công; frontend typecheck và build thành công.
- Tài liệu: `README.md`, `API_DESIGN.md`, `BUSINESS_RULE_DECISIONS.md`, `PROGRESS.md`.

### Chưa có hoặc chưa đủ

- EF Core mới ánh xạ 8/22 bảng (`MUCDO`, `KQHOCTAP`, `TAINGUYEN_CAUHOI`,
  `TAIKHOAN`, `PHIENDANGNHAP`, `NHATKYHD`, `THISINH`, `KHOADANGNHAP`).
- Import câu hỏi mới có bước preview; chưa lưu file, chưa ghi `LANIMPORT`,
  `CAUHOI`, `TAINGUYEN_CAUHOI`.
- Chưa có CRUD đầy đủ, sinh đề, kỳ thi, engine làm bài, chấm điểm, báo cáo.
- Chưa có quản trị tài khoản (khóa/mở, cấp lại mật khẩu).
- Chưa có bảng lịch sử migration, dữ liệu demo, CI, formatter/linter.

## 3. Phạm vi

### Bắt buộc (P0)

- Hai vai trò `ADMIN` và `THISINH`.
- Trắc nghiệm một đáp án đúng A/B/C/D; nội dung và phương án có thể chứa hình.
- Quản lý KQHT, mức độ, câu hỏi, ma trận, đề gốc, đề con, thí sinh, kỳ thi, phân công.
- Import `.docx` câu hỏi và `.xlsx` thí sinh: tệp mẫu, preview, báo cáo lỗi.
- Làm bài trên trình duyệt: tự lưu, đánh dấu câu, nộp/tự thu, chấm thang 10.
- Kết quả, tra cứu, xuất `.xlsx`/`.csv`, nhật ký hoạt động.
- Tối ưu laptop/desktop; màn hình thi dùng được trên tablet.

### Ngoài phạm vi (P1/P2)

- Tự luận, nhiều đáp án đúng, media lớn (âm thanh/video).
- Giám sát webcam/AI, trình duyệt khóa, chống gian lận cấp hệ điều hành.
- SSO, email/SMS, đa trường, ứng dụng mobile.
- Phúc khảo, ngân hàng nhiều môn có chương/bài.

Chỉ làm P1/P2 sau khi P0 đạt nghiệm thu.

## 4. Quyết định nghiệp vụ

Chi tiết ở `docs/BUSINESS_RULE_DECISIONS.md`.

### Đã chốt

| Mã | Nội dung |
|---|---|
| D-KQHT-01 (một phần) | `KYHIEU` là chữ cái thứ `THUTU`: A - KQHT 1, B - KQHT 2, ...; tối đa 26 KQHT |
| D-KQHT-01 (một phần) | Mức độ: 1 dễ, 2 trung bình, 3 khó; mã câu hỏi `[KyHieu][MaMD]-[STT]`, duy nhất |
| D-IMPORT-01 | Câu hỏi nhập bằng DOCX theo mẫu IU03 |
| D-MEDIA-01 | Hình trong nội dung/phương án lưu ở `TAINGUYEN_CAUHOI`; file bất biến theo `STORAGEKEY` |
| D-GHICHU-01 | Ghi chú chỉ hiển thị khi preview/import, không lưu vào đề |
| Schema | Đề lưu bản sao câu hỏi và hình (`GOM_CAU`, `TAINGUYEN_GOMCAU`); câu hỏi đã dùng chỉ ngừng sử dụng, không xóa cứng |
| Schema | Mỗi đề gốc gắn cho một kỳ thi; không đổi đề sau khi đã phân công |

### Đề xuất, chờ người hướng dẫn xác nhận

- D-TRANGTHAI-01: giá trị trạng thái KQHT, câu hỏi, kỳ thi, cách phân đề, giới
  tính (đã áp dụng CHECK; đổi bằng migration mới).
- Làm tròn điểm: `số câu đúng / tổng số câu * 10`, làm tròn 2 chữ số.

### Còn mở - phải chốt trước giai đoạn tương ứng

| Quyết định | Hạn chốt | Chặn giai đoạn |
|---|---|---|
| Chuyển trạng thái hợp lệ của câu hỏi, KQHT | Tuần 2 | 2 |
| D-CAUDAN-01: xử lý cột "Đánh dấu câu dẫn" | Tuần 2 | 3 (import câu hỏi) |
| Quy tắc trùng mã/tài khoản khi import, import từng phần | Tuần 3 | 3 |
| Chuyển trạng thái kỳ thi; hủy kỳ thi | Tuần 5 | 6 |
| Thí sinh vào muộn: đủ thời lượng hay kết thúc theo giờ chung | Tuần 6 | 7 |
| Thi lại, cộng giờ, mở lại bài | Tuần 6 | 7 |
| Chính sách xem điểm/xem lại | Tuần 7 | 8 |

Không triển khai engine thi (giai đoạn 7) khi các quyết định thời gian chưa thành
test nghiệp vụ.

## 5. Kiến trúc mục tiêu

### Backend

- `Domain`: entity, enum trạng thái, quy tắc chuyển trạng thái, sinh mã, hoán vị,
  chấm điểm.
- `Application`: command/query, DTO, validation, authorization policy, ranh giới
  transaction.
- `Infrastructure`: EF Core mapping 21 bảng, query, password hashing, token,
  import/export, lưu file, audit log.
- `Api`: controller, authentication, `ProblemDetails`, OpenAPI, rate limit, health
  check.

`Domain` không phụ thuộc lớp khác; `Application` chỉ phụ thuộc `Domain`;
`Infrastructure` triển khai interface của `Application`; `Api` là composition root.

### Frontend

- `features/`: auth, learning-outcomes, question-bank, students, exam-matrices,
  exam-papers, exams, exam-assignments, exam-monitoring, exam-taking, results.
- `components/` dùng chung; `layouts/` theo vai trò; `services/` HTTP client và
  session.
- Bổ sung TanStack Query (server state), React Hook Form + Zod (form), Vitest +
  Testing Library, Playwright (E2E).

### Database

- [x] Schema hoàn chỉnh, CHECK trạng thái, khóa chính clustered, `rowversion`.
- [x] Bảng phiên đăng nhập; cột khóa tạm/bắt buộc đổi mật khẩu/mã bảo mật trong
  `TAIKHOAN`.
- [x] Seed ba mức độ.
- [x] Migration nâng cấp giữ dữ liệu, chạy lại được.
- [ ] Bảng lịch sử migration để biết database đã chạy đến bản nào.
- [x] Bootstrap tài khoản admin an toàn (không ghi mật khẩu mặc định vào repo).
- [ ] Dữ liệu demo deterministic.
- [ ] Index bổ sung theo execution plan thực tế, không tạo theo cảm tính.

Quy ước: mọi thời điểm lưu UTC, hiển thị `Asia/Ho_Chi_Minh`. Database đã tồn tại
chỉ thay đổi bằng file mới trong `database/migrations/`; cập nhật đồng thời
`CampusExamDB.sql`. Chạy `sqlcmd` phải có cờ `-I` (filtered index cần
`QUOTED_IDENTIFIER ON`).

## 6. Kế hoạch theo giai đoạn

### Giai đoạn 0 - Nền tảng (Tuần 1-2, đến 15/11/2026)

- [x] Solution 4 lớp, `ProblemDetails`, health API.
- [x] Schema database hoàn chỉnh và migration 01-04.
- [x] Tài liệu quyết định nghiệp vụ và tiến độ.
- [~] Git + GitHub (xong); quy ước nhánh chưa chốt.
- [x] Proxy `/api` trong Vite (dev). CORS allowlist làm khi triển khai frontend và
  API khác origin.
- [~] OpenAPI (xong: `/openapi/v1.json` ở Development), health check database,
  structured logging.
- [~] EF Core mapping các bảng lõi: xong `TAIKHOAN`, `PHIENDANGNHAP`, `NHATKYHD`
  (kèm quy ước thời gian UTC và test đối chiếu cột EF với database); còn
  `CAUHOI`, `LANIMPORT`, `LOINHAPDULIEU` (làm cùng giai đoạn 2-3).
- [x] Fixture integration test trên SQL Server thật (database tạm theo lần chạy),
  thay EF InMemory cho các test hành vi database.
- [~] Chuẩn hóa pagination, filter, sort và mã lỗi nghiệp vụ: đã có hợp đồng
  (`docs/API_DESIGN.md`), `TrangDuLieu<T>`, `ErrorCodes`, lỗi validation chuẩn;
  còn áp vào endpoint danh sách đầu tiên.
- [x] Bootstrap admin (`bootstrap-admin`).
- [ ] Bảng lịch sử migration, dữ liệu demo.
- [~] Frontend: design tokens, component cơ bản, loading/empty (xong); trang
  403/404/lỗi chưa có.
- [ ] Formatter/linter, CI chạy build + test backend/frontend.
- [x] README: cài SQL Server, chạy script database, User Secrets, lệnh chạy.

**Nghiệm thu:** máy mới dựng được dự án từ README; script tạo database sạch; CI
xanh; không có secret trong Git; frontend gọi được API.

### Giai đoạn 1 - Xác thực và tài khoản (Tuần 2-3, đến 22/11/2026)

- [x] API đăng nhập, refresh, đăng xuất, `me`, đổi mật khẩu.
- [x] Hash mật khẩu bằng `PasswordHasher` của ASP.NET Core.
- [x] Access token ngắn hạn; refresh token trong cookie `HttpOnly`/`Secure`/`SameSite`,
  lưu hash ở `PHIENDANGNHAP`, xoay vòng và thu hồi phiên.
- [~] Đổi `MABAOMAT` khi đổi mật khẩu/khóa tài khoản để vô hiệu phiên cũ (xong đổi
  mật khẩu; khóa tài khoản làm cùng quản trị tài khoản).
- [x] Khóa tạm sau nhiều lần sai theo tên đăng nhập + IP (`KHOADANGNHAP`); rate limit đăng nhập;
  thông báo lỗi không lộ tài khoản có tồn tại.
- [x] Bắt buộc đổi mật khẩu lần đầu (`PHAIDOIMATKHAU`).
- [x] Policy `AdminOnly`, `CandidateOnly`; áp cho các API hiện có.
- [ ] Quản trị tài khoản: tạo admin, khóa/mở, reset mật khẩu, có audit.
- [x] UI đăng nhập, đổi mật khẩu, route guard, gia hạn phiên.

**Nghiệm thu:** mỗi vai trò chỉ vào đúng route/API; token hết hạn được xử lý; tài
khoản bị khóa mất quyền; có test cho đăng nhập sai, khóa tạm, thu hồi phiên.

### Giai đoạn 2 - KQHT và ngân hàng câu hỏi (Tuần 3, đến 22/11/2026)

- [~] KQHT: đã có xem và tạo; còn sửa, ngừng/khôi phục, chặn đổi thứ tự khi đã có
  câu hỏi.
- [~] UI KQHT: danh sách, thêm (ký hiệu tự điền theo thứ tự); còn sửa, ngừng dùng.
- [ ] CRUD câu hỏi; tìm theo mã/nội dung; lọc theo KQHT/mức độ/trạng thái; phân trang.
- [ ] Cấp `STT` tiếp theo theo (KQHT, mức độ) trong transaction, xử lý tranh chấp
  unique key; điền `CAUHOI.KYHIEU`.
- [ ] Sửa câu hỏi dùng `rowversion` chống ghi đè.
- [ ] UI danh sách, form tạo/sửa có hình, xem chi tiết, ngừng sử dụng.
- [ ] Không trả `DAPANDUNG` cho bất kỳ API phía thí sinh.

**Nghiệm thu:** tạo/sửa/tìm câu hỏi end-to-end; không tạo được mã trùng; câu đã
dùng không xóa được; test authorization và validation đầy đủ.

### Giai đoạn 3 - Import và thí sinh (Tuần 4, đến 29/11/2026)

**Import câu hỏi DOCX**

- [x] Preview: đọc bảng câu hỏi, phương án, đáp án, hình; lỗi/cảnh báo từng dòng.
- [ ] Kiểm thử với bản `.docx` thật của mẫu IU03 (41 dòng, 74 ảnh).
- [ ] Lưu file hình vào storage theo `STORAGEKEY` bất biến; kiểm tra loại/kích thước
  ảnh.
- [ ] Commit import trong transaction: `LANIMPORT`, `LOINHAPDULIEU`, `CAUHOI`,
  `TAINGUYEN_CAUHOI`; đối chiếu mã với KQHT/mức độ trong database.
- [ ] UI upload, preview, xác nhận, lịch sử import, tải báo cáo lỗi.

**Thí sinh**

- [ ] CRUD thí sinh gắn 1-1 tài khoản; khóa thay vì xóa khi đã có lịch sử thi.
- [ ] Import `.xlsx` thí sinh: tệp mẫu, preview, ghi theo batch, báo cáo lỗi.
- [ ] Mật khẩu khởi tạo không ghi log; bắt buộc đổi ở lần đăng nhập đầu.

**Nghiệm thu:** import tệp hợp lệ thành công; tệp lỗi không để lại dữ liệu nửa vời;
số tổng/thành công/lỗi luôn khớp; file sai loại/quá lớn bị từ chối.

### Giai đoạn 4 - Ma trận đề (Tuần 5, đến 06/12/2026)

- [ ] CRUD `MATRAN` + `CAU_TRUC` trong một transaction.
- [ ] UI bảng KQHT x mức độ, nhập số câu, hiển thị tỷ lệ.
- [ ] Validate tổng `SOCAU` = `TONGSOCAU`, thời gian > 0.
- [ ] API độ phủ ngân hàng: số câu khả dụng/yêu cầu từng ô.
- [ ] Chỉ kích hoạt khi hợp lệ và đủ câu; ma trận đã sinh đề chỉ ngừng sử dụng.

**Nghiệm thu:** ma trận thiếu/tổng sai không kích hoạt được; UI chỉ rõ ô thiếu;
sửa đồng thời bị chặn bằng `rowversion`.

### Giai đoạn 5 - Sinh đề gốc và đề con (Tuần 6-7, đến 20/12/2026)

- [ ] Sinh `DEGOC` trong transaction, chọn ngẫu nhiên đủ câu từng ô; lưu `HATGIONG`.
- [ ] Ghi bản sao vào `GOM_CAU` và `TAINGUYEN_GOMCAU`.
- [ ] Sinh `DECON` và `HOAN_VI` (điền `MADE`); tôn trọng `CODINHDA`; tính
  `DAPANDUNGHIENTHI` từ `TTDAPAN`.
- [ ] Test invariant: đúng tập câu, không trùng thứ tự, đáp án hiển thị đúng.
- [ ] UI preview đề gốc/đề con, bản in thử.

**Nghiệm thu:** không sinh đề khi thiếu câu; lỗi giữa chừng rollback toàn bộ; 100
lần sinh tự động giữ đúng invariant.

### Giai đoạn 6 - Kỳ thi và phân công (Tuần 8, đến 27/12/2026)

- [ ] CRUD kỳ thi nháp; gắn một đề gốc chưa dùng.
- [ ] Cấu hình giờ bắt đầu, thời lượng, phút vào trễ, hiện điểm, xem lại, cách phân đề.
- [ ] Chọn/import thí sinh; tạo `PHANCONGTHI` (điền `MADE`); gán đề con ngẫu nhiên
  hoặc cân bằng.
- [ ] Kiểm tra trước phát hành; sau phát hành khóa trường ảnh hưởng công bằng, thay
  đổi đặc biệt có action riêng và audit.
- [ ] Dashboard: đã phân công/chưa vào/đang làm/đã nộp/tự thu.

**Nghiệm thu:** kỳ thi thiếu đề hoặc sai cấu hình không phát hành được; mỗi thí
sinh một phân công mỗi kỳ; phát hành có transaction và audit.

### Giai đoạn 7 - Engine làm bài (Tuần 9-10, đến 10/01/2027)

- [ ] Danh sách kỳ thi của thí sinh đăng nhập.
- [ ] Bắt đầu bài idempotent: một `BAILAM` mỗi phân công (điền `MADECON`).
- [ ] API đề chỉ trả nội dung đã hoán vị, không có đáp án đúng.
- [ ] Đồng hồ theo giờ server; mọi kiểm tra hết giờ ở server.
- [ ] Tự lưu dạng upsert idempotent (điền `TRA_LOI.MADECON`); trạng thái
  lưu/mất mạng; đánh dấu câu; heartbeat `LANHDCUOI`.
- [ ] Nộp bài idempotent; job nền tự thu bài hết giờ.
- [ ] Khôi phục sau reload/mất mạng; từ chối sửa sau khi nộp/hết giờ.

**Nghiệm thu:** reload/mất mạng không mất đáp án; hai tab không tạo hai bài; request
sau hết giờ bị từ chối; payload không chứa đáp án đúng; tự thu chạy khi không có
client.

### Giai đoạn 8 - Chấm điểm, kết quả, nhật ký (Tuần 11, đến 17/01/2027)

- [ ] Chấm trong transaction khi nộp/tự thu; chấm lại idempotent và có audit.
- [ ] Thí sinh xem điểm/xem lại theo `HIENDIEM`/`CHOXEMLAI`.
- [ ] Quản trị lọc kết quả, xem chi tiết, xuất file; thống kê cơ bản.
- [ ] Ghi `NHATKYHD` cho đăng nhập quản trị, CRUD quan trọng, import, sinh đề, phát
  hành, cộng giờ, chấm lại, xuất dữ liệu.

**Nghiệm thu:** điểm khớp bộ test chuẩn; quyền xem đúng cấu hình; file xuất khớp màn
hình; audit đủ người-thời điểm-hành động-đối tượng.

### Giai đoạn 9 - Hardening và bàn giao (Tuần 12, đến 24/01/2027)

- [ ] E2E toàn luồng trên SQL Server riêng; kiểm thử tải lúc bắt đầu và nộp bài.
- [ ] Rà soát OWASP: injection, XSS, phân quyền, cookie/token, upload, secret, log.
- [ ] Accessibility cơ bản.
- [ ] Cấu hình production, HTTPS, reverse proxy, log rotation, monitoring tối thiểu.
- [ ] Diễn tập backup/restore; runbook triển khai, rollback, xử lý kỳ thi đang diễn ra.
- [ ] UAT, sửa lỗi P0/P1, đóng băng bản demo.
- [ ] README, tài liệu API, ERD, hướng dẫn admin/thí sinh, kịch bản demo.

**Nghiệm thu:** không còn lỗi P0/P1; restore thành công; triển khai được từ tài
liệu; demo end-to-end trên môi trường sạch.

## 7. Danh mục API

Đường dẫn tiếng Việt không dấu, kebab-case (D-API-01); phân quyền bằng policy, không
bằng tiền tố đường dẫn. Hợp đồng chi tiết ở `docs/API_DESIGN.md` mục 2.

| Nhóm | API chính | Vai trò | Trạng thái |
|---|---|---|---|
| Xác thực | `/api/xac-thuc/dang-nhap`, `/lam-moi`, `/dang-xuat`, `/toi`, `/doi-mat-khau` | Công khai/đã đăng nhập | [x] |
| Tài khoản | quản trị tài khoản (đi cùng thí sinh, bước 4) | Admin | [ ] |
| Danh mục | `/api/kqht`, `/api/muc-do` | Admin | [~] |
| Câu hỏi | `/api/cau-hoi` | Admin | [ ] |
| Nhập câu hỏi | `/api/nhap-cau-hoi/xem-truoc`, xác nhận, lịch sử | Admin | [~] |
| Thí sinh | `/api/thi-sinh` | Admin | [ ] |
| Ma trận | `/api/ma-tran` | Admin | [ ] |
| Đề | `/api/de-goc` | Admin | [ ] |
| Kỳ thi | `/api/ky-thi` (phân công, phát hành, giám sát) | Admin | [ ] |
| Kết quả, nhật ký | xem `API_DESIGN.md` mục 2.10 | Admin | [ ] |
| Cổng thí sinh, làm bài | `/api/toi/*`, `/api/toi/ky-thi/{maKT}/bai-lam` | Thí sinh | [ ] |

Mọi API danh sách có phân trang; mọi API ghi có validate, authorize và mã lỗi ổn định.

## 8. Route frontend

- `/login`, `/change-password`.
- `/admin` (tổng quan), `/admin/learning-outcomes` (KQHT), `/admin/questions`,
  `/admin/questions/import`.
- `/admin/candidates`, `/admin/candidates/import`.
- `/admin/matrices`, `/admin/matrices/:id`, `/admin/papers`, `/admin/papers/:id/preview`.
- `/admin/exams`, `/admin/exams/:id`, `/assignments`, `/monitor`, `/results`.
- `/admin/audit-logs`.
- `/student/exams`, `/student/results`.
- `/exam/:assignmentId`: giao diện thi toàn màn hình.

## 9. Chiến lược kiểm thử

### Backend

- **Unit:** validation, sinh mã, hoán vị, chấm điểm, chuyển trạng thái, quy tắc
  thời gian.
- **Integration trên SQL Server thật:** mapping, ràng buộc database, transaction,
  query. Database tạm theo lần chạy, tạo từ `CampusExamDB.sql`. Không dùng InMemory
  cho hành vi database.
- **API:** `WebApplicationFactory`, phân quyền, error contract, idempotency,
  concurrency.
- **Invariant:** sinh nhiều đề ngẫu nhiên, kiểm tra không thiếu/trùng câu và đáp án
  sau hoán vị đúng.
- **Script database:** mỗi migration mới phải chạy được trên bản sao database phát
  triển và cho cấu trúc giống database tạo mới (cách đã dùng cho migration 03).

### Frontend

- **Component:** form, timer, route guard, điều hướng câu, trạng thái tự lưu.
- **Contract:** type TypeScript sinh từ OpenAPI.
- **E2E Playwright:** admin chuẩn bị kỳ thi; thí sinh dự thi; tự thu; xem/xuất kết quả.

### Ca bắt buộc trước release

- Vào sớm, đúng giờ, vào muộn hợp lệ, quá hạn vào.
- Reload, mất mạng, gửi lặp, hai tab, token hết hạn khi đang thi.
- Nộp đúng lúc đang tự lưu; nộp đồng thời với job tự thu.
- Thiếu câu khả dụng; import trùng; sửa đồng thời; tài khoản bị khóa.
- Thí sinh A không đọc được bài/kết quả của B; payload thi không chứa đáp án đúng.

Ngưỡng khởi đầu 70% coverage cho `Application`/`Domain`; các quy tắc P0 phải có
test cho từng ca nghiệp vụ.

## 10. CI/CD và môi trường

### Pipeline mỗi pull request

1. Restore, quét secret.
2. Backend: format/analyzer, build Release, unit + integration test (SQL Server
   container).
3. Frontend: lint, typecheck, test, build.
4. E2E smoke nếu hạ tầng cho phép.
5. Không merge khi pipeline đỏ.

### Môi trường

- `Development`: dữ liệu demo, User Secrets.
- `Test/CI`: database tạo mới mỗi lần chạy.
- `Staging`: gần production, dùng cho UAT/demo.
- `Production`: secret từ biến môi trường, HTTPS bắt buộc, log hạn chế dữ liệu cá nhân.

Mỗi release: migration đã review, backup trước deploy, health check sau deploy, thủ
tục rollback.

## 11. Phân công mẫu (2 người)

| Giai đoạn | Thành viên A | Thành viên B | Làm chung |
|---|---|---|---|
| 0-1 | Git/CI, EF mapping, test SQL Server, auth API | UI kit, proxy/CORS, auth UI | API contract, README |
| 2-3 | API câu hỏi, commit import, lưu file | UI KQHT, câu hỏi, import | Test end-to-end từng lát cắt |
| 4-6 | Ma trận, sinh đề, kỳ thi, phân công | UI ma trận, preview đề, kỳ thi, monitor | Test invariant, nghiệm thu phát hành |
| 7 | Start/tự lưu/nộp/job tự thu | Giao diện thi, timer, mất mạng | Test tải, race condition |
| 8-9 | Chấm điểm, export, triển khai | Kết quả, dashboard, tài liệu | UAT, bảo mật, demo |

Giao việc theo lát cắt dọc: migration/entity, use case, API, UI, test, tài liệu.

## 12. Nhịp làm việc

- Sprint 1 tuần: planning đầu tuần, demo + retrospective cuối tuần.
- Ticket có: mục tiêu, quy tắc nghiệp vụ, API/UI, tiêu chí nghiệm thu, test case.
- Mỗi người tối đa 1 hạng mục lớn đang làm.
- Nhánh ngắn, PR nhỏ; bắt buộc review migration, auth, sinh đề, nộp/chấm điểm.
- Không sửa cùng một file khi chưa đồng bộ Git; file database chỉ một người sửa
  mỗi lúc.
- Mức lỗi: P0 sai/mất bài/lộ đáp án/không thi được; P1 sai quyền/sai điểm/không
  phát hành; P2 lỗi phụ; P3 giao diện.

### Definition of Done

- Quy tắc nghiệp vụ được ghi trong `BUSINESS_RULE_DECISIONS.md` nếu mới.
- Có test tương xứng rủi ro; build/test xanh.
- API có phân quyền, validation, error contract.
- UI có loading, empty, error, success.
- Thay đổi database có migration giữ dữ liệu và cập nhật `CampusExamDB.sql`.
- Demo được trên dữ liệu thật; cập nhật `PROGRESS.md`.

## 13. Rủi ro chính

| Rủi ro | Mức độ | Giảm thiểu |
|---|---:|---|
| Nhiều người sửa cùng file | Trung bình | Đã có Git; PR; một người sửa file database mỗi lúc |
| Quy tắc thời gian/trạng thái chưa chốt | Rất cao | Hạn chốt theo bảng mục 4; test trước engine thi |
| Mất đáp án do mạng/race condition | Rất cao | Upsert idempotent, transaction, `rowversion`, E2E mất mạng |
| Lộ đáp án qua API/log | Rất cao | DTO riêng cho thí sinh, test serialization, review bảo mật |
| Sai hoán vị/chấm điểm | Rất cao | Bản sao đề, khóa ngoại kép, invariant test, bộ dữ liệu đối chiếu |
| Mẫu IU03 thật chỉ có dạng `.doc` | Cao | Xin bản `.docx` hợp lệ; dùng làm fixture test |
| Dồn tải lúc bắt đầu/nộp bài | Cao | Index theo execution plan, load test, job tự thu |
| Quá phạm vi | Cao | Giữ P0, đóng băng P1/P2 đến sau UAT |
| Không dựng lại được môi trường demo | Cao | README, seed deterministic, diễn tập từ máy sạch |
| Import dữ liệu bẩn | Trung bình | Mẫu, preview, validation từng dòng, giới hạn file |

## 14. Mốc nghiệm thu

| Mốc | Hạn | Sản phẩm chứng minh |
|---|---:|---|
| M1 - Nền tảng | 22/11/2026 (cuối tuần 3) | Git + CI xanh, dựng DB từ README, đăng nhập/phân quyền |
| M2 - Ngân hàng | 29/11/2026 (cuối tuần 4) | Câu hỏi và thí sinh: CRUD + import end-to-end |
| M3 - Đề thi | 20/12/2026 (cuối tuần 7) | Ma trận hợp lệ, sinh/preview đề gốc và đề con |
| M4 - Kỳ thi | 27/12/2026 (cuối tuần 8) | Tạo, phân công, phát hành, theo dõi kỳ thi |
| M5 - Thi hoàn chỉnh | 10/01/2027 (cuối tuần 10) | Bắt đầu, tự lưu, kết nối lại, nộp, tự thu |
| M6 - Release candidate | 17/01/2027 (cuối tuần 11) | Chấm điểm, kết quả, export, audit, E2E xanh |
| M7 - Bàn giao | 24/01/2027 (cuối tuần 12) | UAT, triển khai, restore, tài liệu, demo |

M1 lùi một tuần so với kế hoạch ban đầu vì tuần 1 dồn cho database; tuần 2-3 phải
làm song song giai đoạn 0 và 1.

## 15. Việc tiếp theo (theo thứ tự)

Đã xong đến 08/11/2026: Git, proxy Vite, README, fixture SQL Server, EF mapping tài
khoản, bootstrap admin, xác thực backend + frontend, gia cố bảo mật, migration 04.

1. CI build + test backend/frontend (GitHub Actions); formatter/linter.
2. Chốt D-KQHT-01, D-TRANGTHAI-01, quy tắc chuyển trạng thái câu hỏi và D-CAUDAN-01
   trước khi làm CRUD câu hỏi.
3. Bước 3 `API_DESIGN.md`: KQHT sửa/ngừng dùng, CRUD câu hỏi, nối màn hình ngân hàng.
4. Xin bản `.docx` hợp lệ của mẫu IU03 để kiểm thử nhập câu hỏi.

Cuối mỗi tuần đối chiếu tiến độ thực tế với mốc ở mục 14 và điều chỉnh kế hoạch.
Chưa xong auth thì không bắt đầu engine thi.
