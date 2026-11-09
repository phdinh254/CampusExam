# Tiến độ CampusExam

Ngày cập nhật gần nhất: 09/11/2026.

## Đã hoàn thành

### Nền tảng backend

- Solution .NET 10 gồm Domain, Application, Infrastructure, Api và hai project kiểm thử.
- Entity Framework Core kết nối SQL Server qua chuỗi kết nối trong User Secrets.
- Có health API và ánh xạ ban đầu cho bảng `MUCDO`.
- Đã đăng ký `ProblemDetails`, xử lý exception và status code tập trung; phản hồi lỗi
  có `traceId` để đối chiếu log.

### Danh mục mức độ

- `GET /api/muc-do` trả danh sách mức độ theo `MaMD` tăng dần.
- Controller gọi hợp đồng `IMucDoQuery` trong Application.
- Truy vấn EF Core và chuyển đổi sang DTO nằm trong Infrastructure.

### Kết quả học tập (KQHT)

- Đã ánh xạ entity `KqHocTap` với bảng `dbo.KQHOCTAP`.
- `GET /api/kqht` trả danh sách theo `ThuTu`, sau đó theo `MaKQHT`.
- `GET /api/kqht/{maKQHT}` trả chi tiết hoặc HTTP 404 khi không tìm thấy.
- `POST /api/kqht` kiểm tra tên, thứ tự, mô tả, trạng thái và ký hiệu trước khi ghi.
- Ký hiệu được chuẩn hóa thành chữ hoa; API trả HTTP 409 với mã
  `KQHT_KY_HIEU_CONFLICT` nếu `KyHieu` đã tồn tại, kể cả khi hai request ghi đồng thời.
- Tạo thành công trả HTTP 201 cùng header `Location` trỏ tới API chi tiết.
- Lỗi không tìm thấy KQHT dùng `ProblemDetails` với mã ổn định
  `KQHT_NOT_FOUND`.
- DTO và hợp đồng truy vấn nằm trong Application; EF Core nằm trong Infrastructure.
- Có kiểm thử cấu trúc JSON, metadata ánh xạ EF và hành vi controller chi tiết.
- Test ghi và truy vấn KQHT chạy trên SQL Server thật; giả định về trạng thái,
  `ThuTu`, `KyHieu` ghi tại `docs/BUSINESS_RULE_DECISIONS.md`.

### Import ngân hàng câu hỏi từ Word

- Đã đối chiếu tài liệu yêu cầu và mẫu IU03: mã câu hỏi theo dạng
  `[KyHieu][MaMD]-[STT]`; mức độ 1/2/3 tương ứng dễ/trung bình/khó.
- Đã chốt `.docx` là định dạng nhập chính thức. `.doc` cũ phải được chuyển đổi
  ngoài hệ thống; API không phụ thuộc Microsoft Word hoặc LibreOffice.
- Đã bổ sung bảng `TAINGUYEN_CAUHOI` vào baseline SQL và migration riêng
  `20261105_01_add_tainguyen_cauhoi.sql`; nội dung ảnh nằm ở storage, database
  chỉ lưu metadata và vị trí `NOIDUNG`/`DAA`/`DAB`/`DAC`/`DAD`.
- Đã ánh xạ `TAINGUYEN_CAUHOI` vào EF Core.
- Đã triển khai parser Open XML cho bảng IU03: đọc mã, tách câu hỏi và bốn đáp
  án, kiểm tra đáp án đúng, phát hiện mã sai/trùng và bảo toàn marker ảnh.
- `POST /api/nhap-cau-hoi/xem-truoc` nhận multipart `.docx`, giới hạn 20 MB;
  trả tổng dòng hợp lệ/lỗi, số ảnh, dữ liệu từng dòng và mã lỗi/cảnh báo.
- Dòng có `Đánh dấu câu dẫn` bị chặn bằng `UNSUPPORTED_STEM_MARKER` cho tới khi
  có quy tắc nghiệp vụ; ghi chú được cảnh báo và không trả cho thí sinh.
- Đã xác nhận file IU03 nguồn có 41 dòng câu hỏi và 74 ảnh bằng chế độ đọc Word.
  Chưa chạy parser Open XML trực tiếp trên IU03 vì nguồn hiện là `.doc` và thao
  tác chuyển đổi tự động của Word bị treo; cần một bản `.docx` được lưu hợp lệ.

### Database

- `database/CampusExamDB.sql` tạo database mới hoàn chỉnh: 22 bảng, khóa chính
  clustered, CHECK cho mọi cột trạng thái (xem `D-TRANGTHAI-01`), `rowversion`
  cho câu hỏi/ma trận/kỳ thi/bài làm, bảng phiên đăng nhập, bản sao hình ảnh
  của đề và seed ba mức độ.
- Database đã có: chạy `migrations/` theo thứ tự 01 → 02 → 03 → 04; các script chạy
  lại được và giữ dữ liệu.
- Đã kiểm tra trên SQL Server 2019 cục bộ: database tạo mới và database nâng cấp
  từ bản sao `CampusExam` có cấu trúc giống hệt nhau; 28 kịch bản ghi dữ liệu
  hợp lệ/không hợp lệ cho kết quả đúng.
- Database `CampusExam` phát triển đã chạy đủ 01 → 04 (22 bảng; migration 04 chạy
  08/11/2026, giữ tài khoản admin). Máy khác chưa chạy 04 thì đăng nhập lỗi 500.
- Migration 04 đã kiểm tra trên database tạm: dựng từ baseline cũ rồi chạy 04 cho
  cấu trúc giống hệt baseline mới (339 cột/ràng buộc/index), giữ dữ liệu, chạy lại
  được.

### Tài khoản

- Đã ánh xạ EF `TAIKHOAN`, `PHIENDANGNHAP`, `NHATKYHD`; mọi cột `datetime2` đọc ra
  có `Kind = Utc`.
- Test `Ef_model_columns_match_database_columns` đối chiếu mọi cột EF với database
  dựng từ `CampusExamDB.sql`.
- Tạo tài khoản quản trị đầu tiên (chỉ chạy được khi chưa có admin):

  ```
  cd backend/src/CampusExam.Api
  dotnet run -- bootstrap-admin --ten-dn admin --ho-ten "Quản trị viên"
  ```

  Mật khẩu nhập ẩn hai lần (hoặc đặt biến `CAMPUSEXAM_ADMIN_PASSWORD`); tối thiểu
  8 ký tự, có chữ và số, không chứa tên đăng nhập. Mã thoát: 0 thành công, 1 dữ
  liệu không hợp lệ, 2 đã có admin hoặc trùng tên đăng nhập.
- Database `CampusExam` phát triển đã có tài khoản quản trị `admin` (MTK 1, tạo
  06/11/2026 bằng `bootstrap-admin`); mật khẩu không ghi trong repo.

## Trạng thái kiểm tra

- Build solution thành công, không có cảnh báo hoặc lỗi tại mốc gần nhất (Debug và
  Release `-warnaserror`); `dotnet format --verify-no-changes` sạch.
- 136/136 kiểm thử backend thành công: 45 unit test và 91 integration test (gồm
  test HTTP qua `WebApplicationFactory` cho route, mã lỗi, OpenAPI, toàn bộ luồng
  xác thực, KQHT và ngân hàng câu hỏi trên SQL Server tạm).
- 36/36 kiểm thử frontend (Vitest, `npm test`) cho `httpClient`, xác thực và form
  câu hỏi.
- Integration test có truy cập database dùng `SqlServerDatabaseFixture`: mỗi lần
  chạy tạo database tạm `CampusExam_Test_*` từ `CampusExamDB.sql` rồi xóa. Máy chủ
  mặc định là SQL Server cục bộ (Windows auth); đổi bằng biến môi trường
  `CAMPUSEXAM_TEST_SQLSERVER`.
- Frontend typecheck và production build thành công tại mốc gần nhất.
- Frontend dev gọi API qua proxy Vite `/api` → `https://localhost:7000`; đã xác
  nhận `GET /api/health/database`, `/api/muc-do`, `/api/kqht` trả HTTP 200 với
  database `CampusExam` thật.

### Giao diện frontend (06/11/2026)

- Đã thay toàn bộ placeholder bằng giao diện responsive cho ba không gian:
  quản trị viên, thí sinh và phòng thi.
- Đã có các màn hình mẫu: đăng nhập theo vai trò, dashboard quản trị, ngân hàng
  câu hỏi, kỳ thi, thí sinh, cổng thí sinh và làm bài thi.
- Màn hình làm bài hỗ trợ chọn đáp án, đánh dấu câu, bảng điều hướng 40 câu và
  hộp xác nhận nộp bài ở phía client; màn hình ngân hàng câu hỏi có tìm kiếm và
  lọc mức độ bằng dữ liệu mẫu.
- Đã bổ sung design token, component/icon dùng chung, trạng thái bảng/thẻ và
  responsive cho desktop, tablet, điện thoại.
- Frontend typecheck và production build thành công. Dữ liệu trên các màn hình
  mới hiện là dữ liệu trình diễn; chưa nối auth và API nghiệp vụ.
- Đã tinh gọn vòng giao diện thứ hai: chỉ giữ các phân hệ đã có màn hình, loại
  bỏ thông báo/nút giả, rút ngắn nội dung, mở rộng vùng hiển thị và bổ sung lọc
  hoạt động cho câu hỏi, kỳ thi, thí sinh.

### Hợp đồng API (06/11/2026)

- Thiết kế đầy đủ tại `docs/API_DESIGN.md`; đã chốt D-API-01..05 (đường dẫn và
  trường JSON tiếng Việt không dấu; không quản lý phòng thi ở P0; câu hỏi không có
  trạng thái nháp; định nghĩa tỷ lệ hoàn thành; thời hạn token).
- Đã xong bước 1 (chi tiết ở `API_DESIGN.md` mục 8). Thay đổi phá vỡ hợp đồng cũ:
  `/api/question-imports/preview` → `/api/nhap-cau-hoi/xem-truoc` với tên trường
  tiếng Việt; `trangThai` của KQHT là chuỗi thay vì số.
- Cài frontend: npm 10.9 lỗi `Cannot read properties of null (reading 'edgesOut')`
  khi thêm package có peer dependency; dùng `npm install --legacy-peer-deps`.
  `npm install` trên lockfile hiện tại chạy bình thường.

### Xác thực (06/11/2026)

- Xong bước 2 của `API_DESIGN.md` (chi tiết mục 8): đăng nhập, làm mới, đăng xuất,
  đổi mật khẩu; JWT 15 phút + refresh cookie xoay vòng; khóa tạm, rate limit;
  policy theo vai trò; route guard, trang đăng nhập và đổi mật khẩu ở frontend.
- **Mọi API nghiệp vụ hiện yêu cầu đăng nhập quản trị.** Chạy backend cần thêm
  khóa ký trong User Secrets (máy này đã đặt):

  ```
  cd backend/src/CampusExam.Api
  dotnet user-secrets set "Auth:SigningKey" "<chuỗi ngẫu nhiên tối thiểu 32 ký tự>"
  ```

- `doi-mat-khau` dùng chung hạn mức với đăng nhập.

### Gia cố sau rà soát (07/11/2026)

- Import DOCX chặn zip bomb: kiểm tra thư mục trung tâm zip trước khi mở (tối đa
  200 MB sau giải nén, 5.000 thành phần) và `MaxCharactersInPart` 50 triệu ký tự
  (`GioiHanTepDocx`).
- Khóa đăng nhập theo cặp tên đăng nhập + IP thay cho theo tài khoản
  (D-AUTH-KHOA-01); `423` không còn lộ tài khoản có tồn tại; bộ đếm tăng nguyên tử.
- Sai mật khẩu hiện tại khi đổi mật khẩu dùng chung bộ đếm khóa; frontend hiện giờ
  hết khóa ở cả trang đổi mật khẩu.
- `UseForwardedHeaders` với proxy tin cậy cấu hình ở `ReverseProxy:KnownProxies`/
  `KnownNetworks`; hạn mức đăng nhập mặc định 60/phút/IP (`Auth:LoginPermitsPerMinute`).
  **Khi triển khai sau reverse proxy phải khai báo IP proxy**, nếu không mọi người
  dùng chung một hạn mức và một bộ đếm khóa.
- Cache kết quả kiểm tra mã bảo mật access token 30 giây
  (`Auth:TokenCheckCacheSeconds`); đổi mật khẩu xóa cache ngay. API khóa tài khoản/
  cấp lại mật khẩu sau này phải xóa cache tương tự.

### CI (08/11/2026)

- `.github/workflows/ci.yml`: job backend (restore, `dotnet format --verify-no-changes`,
  build Release `-warnaserror`, unit + integration test trên container SQL Server 2022)
  và job frontend (`npm ci`, typecheck, Vitest, build).
- Đã chạy thử cục bộ từng bước (format sạch, build Release 0 cảnh báo, `npm ci` trên
  bản sao sạch). Lần chạy đầu trên GitHub xanh ở commit `f4a164f`.

### Màn hình KQHT (08/11/2026)

- `/admin/learning-outcomes` (menu "Kết quả học tập") nối API thật: danh sách, tìm kiếm, lọc
  trạng thái, thẻ tóm tắt; hộp thoại thêm KQHT tự điền ký hiệu theo thứ tự, hiển thị
  lỗi validation từ server theo từng trường.
- 09/11/2026: thêm sửa, ngừng dùng/khôi phục, xóa (xem mục bước 3 bên dưới).

### Quyết định nghiệp vụ (09/11/2026)

- Người phụ trách chốt D-KQHT-01 (phương án A, hai trạng thái), D-TRANGTHAI-01 (giữ
  bảng giá trị hiện có), D-CAUHOI-TT-01 (chuyển trạng thái câu hỏi tự do, xóa cứng chỉ
  khi chưa nằm trong đề) và D-CAUDAN-01 (giữ chặn ở P0). Chi tiết ở
  `BUSINESS_RULE_DECISIONS.md`.

### KQHT và ngân hàng câu hỏi — bước 3 (09/11/2026)

- KQHT: `PUT`, `PATCH /trang-thai`, `DELETE`; danh sách có `soCauHoi`. Đã có câu
  hỏi thì không đổi thứ tự, không xóa (`409 KQHT_IN_USE`); KQHT ngừng dùng không
  nhận câu mới (`409 KQHT_INACTIVE`).
- `/api/cau-hoi`: danh sách phân trang + tìm/lọc/sắp xếp, thống kê, chi tiết, thêm
  (cấp `STT` tự động, an toàn khi ghi đồng thời), sửa có `rowversion`, đổi trạng thái,
  xóa khi chưa nằm trong đề (`409 QUESTION_IN_USE`).
- Ánh xạ EF thêm `CAUHOI` và `GOM_CAU` (chỉ khóa). Không cần migration mới.
- Frontend: màn KQHT có sửa/ngừng dùng/xóa; màn ngân hàng câu hỏi bỏ dữ liệu mẫu,
  nối API thật (thống kê, lọc, phân trang, form thêm/sửa, ngừng dùng, xóa).
- Chưa làm: ảnh trong câu hỏi (cần lưu file, bước 4). Chưa thử giao diện trên trình
  duyệt với backend thật; mới kiểm tra bằng test HTTP, Vitest, typecheck và build.

## Bước nhỏ tiếp theo

- ESLint cho frontend (bổ sung vào CI).
- Bước 4 của `API_DESIGN.md`: lưu file, xác nhận nhập câu hỏi + lịch sử, ảnh câu
  hỏi; CRUD thí sinh + nhập Excel, quản trị tài khoản.

- Lưu mẫu IU03 thành `.docx`, chạy qua endpoint preview và đối chiếu kết quả phải
  có 41 dòng/74 ảnh; tất cả 41 dòng hiện phải báo `MISSING_CODE` vì cột mã trống.
- Sau khi fixture thực tế đạt yêu cầu, triển khai lưu file vào storage và commit
  import trong transaction với `LANIMPORT`, `LOINHAPDULIEU`, `CAUHOI` và
  `TAINGUYEN_CAUHOI`.
