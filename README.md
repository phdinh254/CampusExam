# CampusExam

Hệ thống thi trắc nghiệm trong phòng máy: quản trị ngân hàng câu hỏi (import từ Word
mẫu IU03), ma trận đề, kỳ thi và phòng thi cho thí sinh.

| Thành phần | Công nghệ | Thư mục |
|---|---|---|
| Backend | ASP.NET Core (.NET 10), EF Core, SQL Server | `backend/` |
| Frontend | React 19, React Router 7, Vite, TypeScript | `frontend/` |
| Database | SQL Server 2019 trở lên | `database/` |
| Tài liệu | Kế hoạch, thiết kế API, quyết định nghiệp vụ, tiến độ | `docs/` |

## Yêu cầu

- .NET SDK 10
- Node.js 22 (npm 10)
- SQL Server cục bộ (Windows authentication) và `sqlcmd` hoặc SSMS

## Cài đặt lần đầu

### 1. Database

Database mới — chạy baseline:

```
sqlcmd -S localhost -E -C -f 65001 -i database/CampusExamDB.sql
```

Database đã có — chạy các script trong `database/migrations/` theo thứ tự tên file
(01 → 02 → 03 → 04 …). Mỗi script tự bỏ qua nếu đã áp dụng.

### 2. Backend

```
cd backend/src/CampusExam.Api
dotnet user-secrets set "ConnectionStrings:CampusExam" "Server=localhost;Database=CampusExam;Integrated Security=True;TrustServerCertificate=True"
dotnet user-secrets set "Auth:SigningKey" "<chuỗi ngẫu nhiên tối thiểu 32 ký tự>"
dotnet dev-certs https --trust
dotnet run -- bootstrap-admin --ten-dn admin --ho-ten "Quản trị viên"
dotnet run --launch-profile https
```

`bootstrap-admin` tạo tài khoản quản trị đầu tiên (hỏi mật khẩu ẩn hai lần) và chỉ
chạy được khi chưa có admin. API chạy ở `https://localhost:7000`; OpenAPI ở
`/openapi/v1.json` (chỉ môi trường Development).

### 3. Frontend

```
cd frontend
npm install
npm run dev
```

Mở `http://localhost:5173`. Vite chuyển `/api` sang `https://localhost:7000`; đổi
bằng `API_PROXY_TARGET` (xem `frontend/.env.example`).

## Kiểm thử

```
cd backend && dotnet test CampusExam.sln
cd frontend && npm test && npm run build
```

Integration test tạo database tạm `CampusExam_Test_*` từ `database/CampusExamDB.sql`
trên SQL Server cục bộ rồi xóa khi xong; đổi máy chủ bằng biến môi trường
`CAMPUSEXAM_TEST_SQLSERVER`.

CI (`.github/workflows/ci.yml`) chạy mỗi lần push lên `main` và mỗi pull request:
backend kiểm tra `dotnet format`, build Release không cảnh báo, chạy toàn bộ test trên
container SQL Server 2022; frontend chạy `npm ci`, typecheck, test, build. Trước khi
push, chạy `dotnet format CampusExam.sln` trong `backend/` để tránh CI báo lỗi định dạng.

## Cấu hình triển khai

| Khóa | Mặc định | Ý nghĩa |
|---|---|---|
| `Auth:SigningKey` | — (bắt buộc) | Khóa ký JWT HS256, tối thiểu 32 byte; không commit |
| `Auth:AllowedOrigins` | `[]` | Origin được gọi làm mới/đăng xuất (chống CSRF) |
| `Auth:LoginPermitsPerMinute` | 60 | Hạn mức đăng nhập/đổi mật khẩu mỗi phút cho một IP |
| `Auth:TokenCheckCacheSeconds` | 30 | Cache kiểm tra mã bảo mật token; 0 = tắt |
| `ReverseProxy:KnownProxies` | `[]` | IP reverse proxy được tin `X-Forwarded-For` |
| `ReverseProxy:KnownNetworks` | `[]` | Dải mạng proxy được tin, ví dụ `10.0.0.0/24` |

Chạy sau reverse proxy (IIS, nginx…) **phải** khai báo proxy ở `ReverseProxy`, nếu
không mọi người dùng chung IP của proxy: chung hạn mức đăng nhập và chung bộ đếm
khóa tạm.

## Tài liệu

- `docs/PROGRESS.md` — tiến độ và trạng thái kiểm thử
- `docs/IMPLEMENTATION_PLAN.md` — kế hoạch triển khai
- `docs/API_DESIGN.md` — hợp đồng API
- `docs/BUSINESS_RULE_DECISIONS.md` — các quyết định nghiệp vụ đã chốt
