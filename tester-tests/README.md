# Bộ test của tester

Thư mục này tách riêng khỏi mã website nhưng nằm trong cùng repository để tester luôn chạy đúng phiên bản code.

## Chạy Unit Test

Mở PowerShell tại thư mục gốc dự án:

```powershell
dotnet test .\tester-tests\Unit.Test\Unit.Test.csproj
```

Hoặc dùng script có báo cáo tổng hợp:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tester-tests\Unit.Test\run-tests.ps1
```

## Chạy Integration Test

```powershell
sqllocaldb start MSSQLLocalDB
powershell -NoProfile -ExecutionPolicy Bypass -File .\tester-tests\Integration.Test\run-tests.ps1
```

Bộ Integration Test tự khởi chạy ứng dụng bằng `WebApplicationFactory`, nên không cần mở website ở cổng 5080 trước. Nếu muốn chạy test HTTP `.mjs`, mở website trước bằng `start-demo.cmd`, sau đó chạy file test ở một PowerShell khác.

Không commit các thư mục `bin`, `obj` hoặc `TestResults`; đây là các file sinh ra sau khi chạy test.
