# Mini Cloud

Backend REST API cho một dịch vụ lưu trữ file đám mây thu nhỏ (kiểu Google Drive mini): đăng ký / đăng nhập, tải file lên, tìm kiếm, tải xuống, đổi tên, xóa và chia sẻ file qua link công khai có thời hạn.

Xây dựng bằng **ASP.NET Core (.NET 10)** theo mô hình **Clean Architecture** + **CQRS với MediatR**, lưu metadata trong **MySQL**, lưu file thật trên **ổ đĩa cục bộ**.

---

## Tính năng

- **Xác thực JWT**: đăng ký, đăng nhập; mật khẩu băm bằng **BCrypt**; thông báo lỗi đăng nhập chung chung và cân bằng thời gian xử lý để chống dò email.
- **Quản lý file** (mỗi người dùng chỉ thấy file của mình):
  - Upload (tối đa **100 MB**/file, tổng dung lượng tối đa **500 MB**/người dùng).
  - Danh sách có **phân trang** và **tìm kiếm** theo tên (không phân biệt hoa thường), mới nhất trước.
  - Tải xuống (hỗ trợ range request), đổi tên, xóa (xóa cả bản ghi lẫn file vật lý).
- **Chia sẻ link**: tạo link công khai (không cần đăng nhập) với token ngẫu nhiên 256-bit, tùy chọn thời hạn 1 giờ – 8760 giờ (1 năm), có thể thu hồi bất cứ lúc nào.
- **Bảo mật**:
  - Rate limit cho `/api/auth/*`: 20 request/phút/IP (trả `429`).
  - Chống path traversal: file trên đĩa được lưu bằng tên GUID ngẫu nhiên, chỉ giữ phần đuôi an toàn.
  - Truy cập file của người khác trả `404` (không lộ việc file có tồn tại).
  - Fail-fast khi thiếu / yếu cấu hình `Jwt:SecretKey` (< 32 ký tự).
- **Xử lý lỗi tập trung**: middleware trả JSON thống nhất `{ "statusCode": ..., "message": "..." }`.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Runtime | .NET 10 / ASP.NET Core Web API |
| CQRS | MediatR 14 |
| ORM / CSDL | Entity Framework Core 10 + Pomelo MySQL (MySQL 8) |
| Xác thực | JWT Bearer, BCrypt.Net-Next |
| Lưu trữ file | Ổ đĩa cục bộ (`IFileStorageService`) |
| Test | xUnit, EF Core InMemory |

## Cấu trúc dự án

```
Mini_Cloud/
├── Mini_Cloud.slnx
└── Backend/
    ├── Mini_Cloud.Api/            # Controllers, middleware, Program.cs (JWT, rate limiter, OpenAPI)
    ├── Mini_Cloud.Application/    # Use case (Command/Query + Handler), interface, DTO, exception
    │   └── Features/{Auth,Files}/
    ├── Mini_Cloud.Domain/         # Entity: User, StoredFile
    ├── Mini_Cloud.Infrastructure/ # EF Core DbContext, BCrypt, JWT generator, LocalFileStorageService
    └── MiniCloud.Tests/           # Unit/integration test (xUnit)
```

Luồng phụ thuộc: `Api → Application ← Infrastructure`, `Domain` không phụ thuộc lớp nào.

## Yêu cầu

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- MySQL 8.x

## Cài đặt & chạy

1. **Tạo database MySQL** (ví dụ tên `files`).

2. **Cấu hình** — không commit secret vào Git. Dùng User Secrets hoặc biến môi trường:

   ```bash
   cd Backend/Mini_Cloud.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=files;User=<user>;Password=<password>;"
   dotnet user-secrets set "Jwt:SecretKey" "<chuoi-bi-mat-dai-it-nhat-32-ky-tu>"
   ```

   Các khóa cấu hình:

   | Khóa | Bắt buộc | Mặc định | Mô tả |
   |---|---|---|---|
   | `ConnectionStrings:DefaultConnection` | Có | — | Chuỗi kết nối MySQL |
   | `Jwt:SecretKey` | Có | — | Khóa ký JWT, tối thiểu 32 ký tự |
   | `Jwt:Issuer` | Không | `CloudDriveAPI` | Issuer của token |
   | `Jwt:Audience` | Không | `CloudDriveClient` | Audience của token |
   | `Jwt:ExpiryInHours` | Không | `24` | Thời hạn token (giờ) |
   | `Storage:RootPath` | Không | `Storage` | Thư mục lưu file trên đĩa |

   Với biến môi trường, dùng dấu `__`, ví dụ `Jwt__SecretKey`.

3. **Chạy API**:

   ```bash
   dotnet run --project Backend/Mini_Cloud.Api --launch-profile http
   ```

   API lắng nghe tại `http://localhost:5264` (profile `https`: `https://localhost:7099`).

   Ở môi trường **Development**, ứng dụng tự tạo schema bằng `EnsureCreated()` và bật OpenAPI (`/openapi/v1.json`).
   > Với production, hãy dùng EF Core Migrations (`Database.Migrate()`) thay cho `EnsureCreated()`.

## API

Mọi endpoint (trừ đăng ký, đăng nhập và tải file qua link chia sẻ) cần header `Authorization: Bearer <token>`.

### Xác thực — `/api/auth`

| Method | Đường dẫn | Body | Mô tả |
|---|---|---|---|
| POST | `/api/auth/register` | `{ "userName", "gmail", "password" }` | Đăng ký. `201` kèm `id`. |
| POST | `/api/auth/login` | `{ "gmail", "password" }` | Đăng nhập. Trả `{ token, user }`. |

Quy tắc: tên 3–50 ký tự; email hợp lệ ≤ 255 ký tự (được chuẩn hóa chữ thường, duy nhất); mật khẩu ≥ 8 ký tự và ≤ 72 byte.

### File — `/api/files`

| Method | Đường dẫn | Mô tả |
|---|---|---|
| POST | `/api/files/upload` | Upload (`multipart/form-data`, trường `file`). `201`. |
| GET | `/api/files` | Danh sách file: tham số `query`, `page` (mặc định 1), `pageSize` (mặc định 10, tối đa 100). |
| GET | `/api/files/{id}/download` | Tải file của mình. |
| PATCH | `/api/files/{id}/rename` | Body `{ "newFileName": "..." }` (≤ 255 ký tự, không chứa `/` `\` hay ký tự điều khiển). |
| DELETE | `/api/files/{id}` | Xóa file. |
| POST | `/api/files/{id}/share` | Tạo link chia sẻ. Body tùy chọn `{ "expiresInHours": 24 }`; bỏ trống = không hết hạn. |
| POST / DELETE | `/api/files/{id}/revoke-share`, `/api/files/{id}/share` | Thu hồi link chia sẻ. |
| GET | `/api/files/shared/{token}` | Tải file qua link chia sẻ (không cần đăng nhập). |

### Mã lỗi

| Mã | Ý nghĩa |
|---|---|
| 400 | Dữ liệu không hợp lệ |
| 401 | Chưa đăng nhập / token sai hoặc hết hạn |
| 404 | Không tìm thấy hoặc không có quyền |
| 409 | Email đã được sử dụng |
| 413 | Vượt giới hạn 100 MB/file hoặc quota 500 MB |
| 429 | Gọi quá nhiều (rate limit auth) |
| 500 | Lỗi hệ thống |

## Mô hình dữ liệu

- **User**: `Id`, `UserName`, `Gmail` (unique), `PasswordHash`, `CreatedAt`.
- **StoredFile**: `Id`, `FileName` (tên hiển thị), `FilePath` (tên GUID trên đĩa), `FileSize`, `UserId` (FK, xóa user → xóa file), `CreatedAt`, `IsShared`, `ShareToken` (unique), `ShareExpiresAt`.

## Kiểm thử

```bash
dotnet test Backend/MiniCloud.Tests
```

Test dùng EF Core InMemory và thư mục tạm, không cần MySQL. Bộ test bao phủ: đăng ký/đăng nhập, upload (giới hạn dung lượng, quota, tải đồng thời, path traversal), danh sách/tìm kiếm/phân trang, tải xuống, đổi tên, xóa, và vòng đời link chia sẻ (tạo, hết hạn, thu hồi).
