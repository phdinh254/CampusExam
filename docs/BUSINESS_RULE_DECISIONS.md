# Quyết định nghiệp vụ CampusExam

Ngày cập nhật gần nhất: 09/11/2026.

Tài liệu này ghi các quyết định ảnh hưởng trực tiếp đến schema, API và kiểm thử.
Một quyết định chỉ chuyển sang **Đã chốt** khi được người phụ trách đồ án xác nhận.

## D-KQHT-01 — Ký hiệu, trạng thái và quy tắc xóa KQHT

**Trạng thái:** Đã chốt (09/11/2026): người phụ trách chọn **phương án A — hai trạng
thái**. Phần ký hiệu/mã câu hỏi chốt từ tài liệu yêu cầu.

### Quy tắc đã chốt (09/11/2026)

- `0` ngừng sử dụng, `1` đang sử dụng; KQHT mới mặc định `1`.
- Chuyển tự do `1 → 0` và `0 → 1` (`PATCH /api/kqht/{maKQHT}/trang-thai`).
- KQHT ngừng sử dụng không nhận câu hỏi mới (`409 KQHT_INACTIVE`); câu hỏi và đề đã
  có không bị ảnh hưởng.
- Sửa được tên, mô tả, thứ tự (`PUT`); ký hiệu luôn suy ra từ thứ tự. Đã có câu hỏi
  thì không đổi được thứ tự (`409 KQHT_IN_USE`).
- Chỉ xóa cứng KQHT chưa có câu hỏi và chưa nằm trong cấu trúc ma trận
  (`CAU_TRUC`); ngược lại `409 KQHT_IN_USE`, người dùng chuyển sang ngừng sử dụng.
- `THUTU` bắt buộc trong `1..26` (đã có CHECK `CKT_KQHOCTAP_KYHIEU`).
- CHECK `TRANGTHAI IN (0,1)` đã có trong schema (`CKT_KQHOCTAP_TRANGTHAI`).

Các mục "Giả định" và "Phương án đề xuất" bên dưới giữ lại để tham khảo lịch sử.

### Dữ kiện đã xác nhận

- `dbo.KQHOCTAP.TRANGTHAI` là `smallint NOT NULL`.
- Script hiện tại không có CHECK constraint và không mô tả ý nghĩa từng giá trị.
- `KYHIEU` là `char(1)`, duy nhất toàn ngân hàng và dùng để tạo mã câu hỏi.
- `KQHOCTAP` được tham chiếu bởi `CAUHOI` và `CAU_TRUC`.
- Vì có khóa ngoại, không được mặc định coi xóa cứng là thao tác an toàn.

### Quy tắc đã chốt từ tài liệu yêu cầu

- Mỗi câu hỏi phải xác định rõ thuộc KQHT nào và mức độ đánh giá nào.
- `KYHIEU` cố định theo thứ tự KQHT: `A` - KQHT 1, `B` - KQHT 2, `C` - KQHT 3,
  `D` - KQHT 4, ... Do đó `KYHIEU` là chữ cái thứ `THUTU` và `THUTU` chỉ từ `1`
  đến `26` (tối đa 26 KQHT).
- Mã mức độ gồm `1` dễ, `2` trung bình và `3` khó.
- Mã câu hỏi có dạng `[KyHieu][MaMD]-[STT]`, ví dụ `A1-1` hoặc `B1-24`.
- Mã câu hỏi là duy nhất; `STT` trong mã là số nguyên dương.
- Biểu thức kiểm tra mã câu hỏi chuẩn là `^[A-Z][1-3]-[1-9][0-9]*$`.

### Thực thi quy tắc

- `CreateKqhtCommand` từ chối `THUTU` ngoài `1..26` và `KYHIEU` không khớp
  `THUTU`.
- Migration `20261105_02_rang_buoc_ma_cau_hoi.sql` thêm CHECK
  `KYHIEU = CHAR(64 + THUTU)`, khóa ngoại `CAUHOI(MAKQHT, KYHIEU)` và CHECK
  `MACAUHOI = KYHIEU + MAMD + '-' + STT`. Hệ quả: không đổi được thứ tự hoặc
  ký hiệu của KQHT đã có câu hỏi.

### Giả định đang tồn tại trong mã nguồn

`CreateKqhtCommand` hiện còn giả định `TRANGTHAI` chỉ nhận `0` hoặc `1`. Giả
định này chưa được PDM/script hoặc tài liệu nghiệp vụ xác nhận.

### Phương án đề xuất

#### Phương án A — Hai trạng thái

- `0`: Ngừng sử dụng.
- `1`: Đang sử dụng.
- KQHT mới mặc định ở trạng thái `1`.
- Cho phép chuyển `1 → 0` và `0 → 1`.
- Chỉ xóa cứng KQHT chưa được tham chiếu; KQHT đã được dùng chỉ chuyển sang `0`.
- Sau khi rà dữ liệu hiện có, bổ sung CHECK `TRANGTHAI IN (0,1)` bằng script
  cập nhật riêng.

#### Phương án B — Ba trạng thái

- `0`: Nháp.
- `1`: Đang sử dụng.
- `2`: Ngừng sử dụng.
- KQHT mới mặc định ở trạng thái `0`.
- Luồng chính: `0 → 1 → 2`; việc khôi phục `2 → 1` cần chốt thêm.
- Không xóa cứng KQHT đã được tham chiếu.
- Sau khi rà dữ liệu hiện có, bổ sung CHECK `TRANGTHAI IN (0,1,2)` bằng script
  cập nhật riêng.

### Quyết định cần xác nhận

Chọn phương án A, phương án B hoặc cung cấp bảng trạng thái khác. Đồng thời xác
nhận `THUTU` có bắt buộc lớn hơn 0 hay không.

Sau khi chốt cần cập nhật đồng bộ validation, API, giao diện, test nghiệp vụ và
script CHECK constraint riêng; không tự động migrate khi khởi động.

## T-KQHT-01 — Kiểm thử phần ghi KQHT

**Trạng thái:** Đã chuyển sang SQL Server thật (05/11/2026); còn thiếu test tranh
chấp ghi đồng thời.

- Test tạo KQHT, trùng `KYHIEU` và CHECK `CKT_KQHOCTAP_KYHIEU` chạy trên database
  tạm dựng từ `CampusExamDB.sql` (`SqlServerDatabaseFixture`).
- Chưa có test hai request ghi đồng thời cùng `KYHIEU` để đi vào nhánh bắt lỗi
  2601/2627.

## D-IMPORT-01 — Định dạng nhập ngân hàng câu hỏi

**Trạng thái:** Đã chốt cho P0.

- Định dạng nhập chính thức là `.docx` theo mẫu bảng IU03 gồm 6 cột.
- `.doc` là định dạng cũ, chỉ được chuyển đổi một lần sang `.docx` ở ngoài hệ
  thống; API không cài Microsoft Word hoặc LibreOffice để đọc `.doc` khi chạy.
- Cột mã câu hỏi là bắt buộc. Không tự đoán KQHT hoặc mức độ khi mã bị trống.
- Nội dung trong một ô được tách thành câu hỏi và bốn phương án A/B/C/D.
- Hình ảnh là dữ liệu bắt buộc phải bảo toàn đúng vị trí trong nội dung hoặc
  phương án; không lưu Base64 trong các cột văn bản.
- Import phải có bước preview. Chỉ ghi database sau khi người dùng xác nhận và
  mọi lỗi chặn đã được xử lý.
- File thí sinh vẫn dùng `.xlsx`; quyết định này chỉ thay đổi import câu hỏi.

## D-MEDIA-01 — Tài nguyên hình ảnh câu hỏi

**Trạng thái:** Đã chốt cho P0.

- Metadata tài nguyên được lưu trong `TAINGUYEN_CAUHOI`; nội dung nhị phân được
  lưu ở file storage/object storage thông qua `STORAGEKEY`.
- `VITRI` nhận một trong `NOIDUNG`, `DAA`, `DAB`, `DAC`, `DAD`.
- `THUTU` bảo toàn thứ tự nhiều ảnh trong cùng một vị trí.
- API dành cho thí sinh chỉ trả URL đọc được trong thời gian phù hợp, không trả
  đường dẫn vật lý hoặc storage key nội bộ.

## D-CAUDAN-01 — Đánh dấu câu dẫn trong mẫu IU03

**Trạng thái:** Đã chốt cho P0 (09/11/2026): giữ chặn như dưới đây; bổ sung nhóm câu
dẫn khi có mẫu thực tế.

- File mẫu có cột `Đánh dấu câu dẫn` nhưng toàn bộ 41 dòng đều để trống, nên
  chưa đủ bằng chứng để thiết kế quan hệ câu dẫn dùng chung.
- P0 chấp nhận ô trống. Dòng có giá trị ở cột này được đưa ra preview với lỗi
  chặn `UNSUPPORTED_STEM_MARKER`, không âm thầm bỏ dữ liệu.
- Chỉ bổ sung entity câu dẫn/nhóm câu sau khi có mẫu thực tế và quy tắc liên kết.

## D-GHICHU-01 — Ghi chú trong mẫu IU03

**Trạng thái:** Đã chốt tạm thời cho P0.

- Ghi chú được hiển thị trong preview và báo cáo import nhưng không phải nội
  dung đề thi, không trả cho thí sinh.
- Vì file mẫu hiện không có dòng nào chứa ghi chú, chưa thay đổi bảng `CAUHOI`.
- Dòng có ghi chú vẫn được import; hệ thống phát cảnh báo để quản trị viên xác
  nhận trước khi commit. Việc lưu ghi chú lâu dài sẽ được chốt khi có dữ liệu mẫu.

## D-TRANGTHAI-01 — Giá trị các cột trạng thái

**Trạng thái:** Đã chốt (09/11/2026), giữ nguyên bảng dưới đây đã áp dụng CHECK trong
schema. Đổi giá trị về sau thì viết migration mới.

| Cột | Giá trị |
|---|---|
| `KQHOCTAP.TRANGTHAI`, `CAUHOI.TRANGTHAI` | `0` ngừng sử dụng, `1` đang sử dụng |
| `KYTHI.TRANGTHAIKYTHI` | `0` nháp, `1` đã phát hành, `2` đang thi, `3` đã kết thúc, `4` đã hủy |
| `KYTHI.CACHPHANDE` | `0` ngẫu nhiên, `1` cân bằng lần lượt |
| `THISINH.GIOITINH` | `0` nữ, `1` nam, `NULL` không khai báo |
| `TAIKHOAN.TRANGTHAI` | `0` khóa, `1` hoạt động |

Ràng buộc kèm theo trong schema:

- Kỳ thi ở trạng thái `1`-`3` phải có `MADE` và `NGAYPHATHANH`; mỗi đề gốc chỉ
  gắn cho một kỳ thi.
- Bài làm `0` (đang làm) chưa có giờ nộp, số câu đúng và điểm; `1`/`2` phải có
  giờ nộp.
- Khóa ngoại kép bảo đảm: đề con thuộc đề gốc của kỳ thi, câu trong hoán vị
  thuộc đề gốc, câu trả lời thuộc đề con được giao. Không đổi được đề của kỳ thi
  hoặc đề con của phân công đã có bài làm.

## D-CAUHOI-TT-01 — Trạng thái, sửa và xóa câu hỏi

**Trạng thái:** Đã chốt (09/11/2026).

- Câu hỏi mới luôn ở trạng thái đang sử dụng (D-API-03: không có nháp).
- Chuyển tự do hai chiều đang sử dụng ⇄ ngừng sử dụng, bất kỳ lúc nào. Ngừng sử
  dụng chỉ loại câu khỏi các lần sinh đề sau; đề đã sinh dùng bản sao trong
  `GOM_CAU` nên không đổi.
- Xóa cứng chỉ khi câu hỏi chưa nằm trong đề nào (`GOM_CAU`); ngược lại
  `409 QUESTION_IN_USE`. Xóa câu hỏi xóa luôn metadata ảnh `TAINGUYEN_CAUHOI` (khi có
  lưu file ở bước 4 phải xóa cả file trong storage).
- Sửa nội dung/phương án/đáp án/cố định đáp án được cả khi câu đã nằm trong đề (đề
  giữ bản sao); giao diện cảnh báo số đề đã dùng. Bắt buộc gửi `phienBan`, lệch trả
  `409 CONCURRENCY_CONFLICT`.
- Không đổi được KQHT và mức độ của câu hỏi vì chúng quyết định mã câu hỏi; muốn
  phân loại lại thì thêm câu mới và ngừng dùng câu cũ.
- `STT` cấp tự động = `MAX(STT) + 1` theo (KQHT, mức độ), trong transaction
  serializable có `UPDLOCK, HOLDLOCK`; số đã xóa không được cấp lại trừ khi là số
  lớn nhất.

## D-THISINH-01 — Tài khoản đăng nhập của thí sinh

**Trạng thái:** Đã chốt (06/11/2026).

### Quy tắc

- Mỗi thí sinh có đúng một tài khoản `TAIKHOAN` (`VAITRO = 'THISINH'`), liên kết
  qua `THISINH.MTK`; hồ sơ và tài khoản được tạo trong cùng một transaction (thêm
  thủ công hoặc nhập `.xlsx`).
- **Tên đăng nhập = mã số thí sinh**: `TAIKHOAN.TENDN = THISINH.MSTS`. Không cho
  đổi `MSTS` sau khi tạo (là khóa chính và là tên đăng nhập).
- **Mật khẩu mặc định = ngày sinh** theo dạng `ddMMyyyy`, ví dụ sinh ngày
  14/03/2005 ⇒ `14032005`. Chỉ lưu hash (`PasswordHasher`), không ghi log.
- Tài khoản mới có `PHAIDOIMATKHAU = 1`: lần đăng nhập đầu chỉ được gọi API
  `xac-thuc/*`, phải đổi mật khẩu trước khi vào hệ thống.
- Mật khẩu mới phải theo chính sách chung (8-128 ký tự, có chữ và số, không chứa
  tên đăng nhập) và **không được trùng ngày sinh**. Mật khẩu mặc định là ngoại lệ
  duy nhất của chính sách.
- **Cấp lại mật khẩu** (admin): đặt lại về ngày sinh, bật `PHAIDOIMATKHAU`, đổi
  `MABAOMAT` để thu hồi mọi phiên. Có ghi `NHATKYHD`.
- Sửa ngày sinh trong hồ sơ **không** tự đổi mật khẩu; ngày sinh mới chỉ có hiệu
  lực ở lần cấp lại tiếp theo.
- Thí sinh không tự sửa hồ sơ, chỉ đổi được mật khẩu.

### Rủi ro và biện pháp

Ngày sinh dễ đoán, nên người biết ngày sinh có thể đăng nhập trước và đổi mật khẩu
của thí sinh. Biện pháp bắt buộc:

- Khóa tạm 15 phút sau 5 lần sai liên tiếp theo cặp tên đăng nhập + IP
  (D-AUTH-KHOA-01); rate limit đăng nhập theo IP.
- Ghi `NHATKYHD` mỗi lần đăng nhập và đổi mật khẩu (kèm IP) để tra cứu khi có
  khiếu nại.
- Danh sách thí sinh cho admin lọc được "chưa đổi mật khẩu mặc định"
  (`PHAIDOIMATKHAU = 1`) để nhắc thí sinh đổi trước ngày thi.
- Thí sinh bị chiếm tài khoản: admin cấp lại mật khẩu (về ngày sinh) và xác minh
  danh tính trực tiếp.

## D-AUTH-KHOA-01 — Khóa đăng nhập theo cặp tên đăng nhập + IP

Đã chốt ngày 07/11/2026, thay cho khóa theo tài khoản (`TAIKHOAN.SOLANDNSAI`,
`TAIKHOAN.KHOADEN`, đã bỏ ở migration 04).

### Lý do

Khóa theo tài khoản cho phép bất kỳ ai khóa tài khoản người khác: nhập sai 5 lần
mật khẩu của bạn mình ngay trước giờ thi là thí sinh đó không vào được 15 phút. Mã
`423` khi đó còn tiết lộ tên đăng nhập có tồn tại.

### Quy tắc

- Đếm lần sai trong bảng `KHOADANGNHAP`, khóa chính (`TENDN`, `DIACHIIP`). `TENDN`
  lưu chữ thường, không có khóa ngoại tới `TAIKHOAN`: tên không tồn tại cũng được
  đếm và bị khóa y hệt, nên `423` không còn tiết lộ tài khoản có tồn tại.
- Sai 5 lần liên tiếp từ cùng IP ⇒ khóa cặp đó 15 phút, trả `423 AUTH_ACCOUNT_LOCKED`
  kèm `khoaDen`; chủ tài khoản ở máy khác vẫn đăng nhập được.
- Đang khóa thì từ chối cả khi nhập đúng mật khẩu.
- Lần sai cách lần sai trước quá 15 phút, hoặc sau khi hết khóa, thì đếm lại từ 1.
  Đăng nhập đúng xóa bộ đếm.
- Nhập sai **mật khẩu hiện tại** khi đổi mật khẩu dùng chung bộ đếm này (chặn dò
  mật khẩu bằng access token bị lộ). Đang khóa thì đổi mật khẩu cũng trả `423`.
- Bộ đếm tăng bằng một câu `MERGE ... WITH (HOLDLOCK)`, nên nhiều request sai đồng
  thời không làm mất lần đếm. Các cặp không sai thêm trong 1 ngày được dọn tự động.
- IP lấy sau middleware `ForwardedHeaders`. Khi chạy sau reverse proxy phải khai
  báo proxy trong `ReverseProxy:KnownProxies`/`KnownNetworks`, nếu không mọi người
  dùng sẽ chung IP của proxy.

### Rủi ro còn lại

- Kẻ tấn công ngồi chung phòng máy (cùng IP NAT) vẫn khóa được thí sinh khác trong
  phòng. Biện pháp: giám thị theo dõi, admin xem `NHATKYHD` (hành động
  `Tạm khóa đăng nhập`).
- Dò mật khẩu phân tán từ nhiều IP chỉ bị chặn bởi rate limit theo IP và chính sách
  mật khẩu.
- Kiểm tra mã bảo mật access token được cache `Auth:TokenCheckCacheSeconds` (mặc
  định 30 giây). Đổi mật khẩu xóa cache ngay; **API khóa tài khoản/cấp lại mật
  khẩu sau này cũng phải xóa cache** (khóa `auth:mabaomat:{MTK}`), nếu không token
  cũ còn dùng được tối đa 30 giây. Khi chạy nhiều instance, thay đổi ở instance
  khác có hiệu lực chậm tối đa chừng đó.

## Các quyết định tiếp theo theo IMPLEMENTATION_PLAN

- Chuyển trạng thái hợp lệ của `KYTHI` (giá trị xem D-TRANGTHAI-01).
- Quy tắc vào thi muộn và thời điểm kết thúc bài.
- Hủy kỳ thi, thi lại, cộng giờ và mở lại bài.
- Chính sách xem điểm/xem lại và cách làm tròn điểm.
- Quy tắc import dữ liệu trùng.
