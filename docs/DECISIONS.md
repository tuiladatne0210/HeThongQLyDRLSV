# DECISIONS.md — Ghi nhận các quyết định kỹ thuật và đối soát tài liệu

> **Dự án:** Hệ thống Quản lý và Tích điểm Hoạt động Sinh viên (EMS-MVC)  
> **Mốc triển khai:** ~60% phạm vi phân công nhóm  
> **Ngày cập nhật:** 14/10/2026  
> **Thành viên:** Lê Thành Đạt (Nhóm trưởng), Lê Thành Hy, Ngô Trọng Thiện  

---

## 1. Quyết định Kiến trúc Nền tảng

| Nội dung | Quyết định đã chốt | Căn cứ & Rationale | Tác động triển khai |
|---|---|---|---|
| **Mô hình kiến trúc** | **ASP.NET Core MVC + Razor Views + Bootstrap + Service Layer + EF Core 10 + SQL Server + Cookie Authentication** | `design.md` Mục 1.2 (Câu 1) và Mục 3.1. | Không xây dựng frontend ReactJS riêng; không dùng JWT làm phương thức xác thực chính; toàn bộ các luồng nghiệp vụ chạy trên server-rendered MVC Razor Views. |
| **Framework & SDK** | **.NET 10 (`net10.0`) & EF Core 10.0.12** | `design.md` Mục 1.2 (Câu 2). .NET 10 là phiên bản LTS của Microsoft. | Toàn bộ solution `EMS-MVC.slnx` dùng SDK .NET 10 (`10.0.401`) và EF Core 10.0.12. |
| **Xác thực & Phiên làm việc** | **Cookie Authentication** (`EMS_MVC_AuthCookie`) kết hợp ASP.NET Core `IPasswordHasher<User>` | `design.md` Mục 3.1 & Yêu cầu bảo mật đề tài. | Phiên làm việc được quản lý qua HttpOnly cookie, thời hạn 8 giờ, sliding expiration; mật khẩu được băm bằng thuật toán chuẩn PBKDF2/HMAC-SHA256, tuyệt đối không lưu plain-text. |
| **Cơ sở dữ liệu** | **Microsoft SQL Server (`QLyDRL`)** | `Database.md` & `Database/QuanLyDRLDB.sql`. | Kết nối tới instance nội bộ `Server=localhost;Database=QLyDRL;Trusted_Connection=True;TrustServerCertificate=True;`. |

---

## 2. Đối soát Schema Cơ sở dữ liệu và EF Core Mapping

1. **Chiến lược khởi tạo Schema:**
   - **Thực tế:** Database đã có bản thiết kế hoàn chỉnh gồm 21 bảng tại `Database/QuanLyDRLDB.sql` cùng các composite keys, check constraints, trigger append-only và view `vw_StudentPointTotals`.
   - **Quyết định:** Sử dụng script SQL để tạo cấu trúc cơ sở dữ liệu gốc nhằm giữ trọn vẹn các triggers chống sửa xóa (`TR_PointTransactions_PreventMutation`, `TR_AuditLogs_PreventMutation`) và composite FKs.
   - **EF Core Mapping:** `ApplicationDbContext` được cấu hình Fluent API mapping 1:1 chính xác với schema này (tên bảng, kiểu cột, composite PKs, unique filtered indexes). Không thực hiện migration phá hủy (drop/alter destructive).

2. **Xử lý ràng buộc duy nhất Đăng ký (BR01) và Hủy đăng ký:**
   - **Vấn đề:** Bảng `ActivityRegistrations` có ràng buộc `UNIQUE (ActivityId, StudentId)`. Khi sinh viên hủy đăng ký, hệ thống cập nhật `Status = 'CANCELLED'`. Nếu sinh viên đổi ý muốn đăng ký lại trong thời hạn cho phép, nếu lệnh INSERT được gọi sẽ vi phạm khóa duy nhất `UQ_ActivityRegistrations_Activity_Student`.
   - **Quyết định:** Trong `RegistrationService.RegisterAsync`, kiểm tra nếu bản ghi đăng ký đã tồn tại ở trạng thái `CANCELLED`, hệ thống tái kích hoạt bản ghi hiện có (`Status = 'REGISTERED'`, `CancelledAt = null`, `UpdatedAt = now`) thay vì INSERT bản ghi mới. Toàn bộ thao tác thực hiện trong transaction có kiểm tra sức chứa đồng thời (BR03).

3. **Mở nhận đăng ký hoạt động (Workflow Open Registration):**
   - **Vấn đề:** Khi hoạt động được phê duyệt (`APPROVED`), hoạt động có tự động chuyển thành `OPEN_REGISTRATION` khi đến giờ không?
   - **Quyết định:** Theo quyết định tại `design.md` (Mục 1.2, Câu 6), Ban tổ chức (Organizer) sẽ **chủ động bấm "Mở đăng ký"** trên giao diện chi tiết hoạt động sau khi được Manager phê duyệt. Thao tác này kích hoạt chuyển trạng thái qua `IActivityService.OpenRegistrationAsync`.

---

## 3. Phân định Phạm vi Mốc 60% và 40% Còn lại

- **Hoàn thành mốc 60%:**
  - Khung ứng dụng MVC, kết nối DB, EF Core 21 entities, Master seed data an toàn.
  - Cookie Authentication, Đăng ký (trạng thái chờ duyệt), Đăng nhập/Đăng xuất, Admin kích hoạt tài khoản & phân quyền.
  - Student Portal: Dashboard (KPIs, lịch hoạt động), Danh mục tra cứu hoạt động, Hồ sơ cá nhân.
  - Organizer: Tạo hoạt động (DRAFT), chỉnh sửa, gửi duyệt (PENDING_APPROVAL), mở đăng ký (OPEN_REGISTRATION).
  - Manager: Hàng đợi phê duyệt, thẩm định chi tiết, phê duyệt hoặc từ chối bắt buộc có lý do.
  - Đăng ký & Hủy: Đăng ký với BR01 (chống trùng), BR02 (đúng thời hạn), BR03 (đúng sức chứa trong transaction).
  - Điểm danh thủ công: Organizer tạo/đóng phiên điểm danh, xem danh sách đăng ký và ghi nhận điểm danh thủ công theo BR04.
- **Để lại cho giai đoạn sau (40% - Đánh dấu NOT_STARTED / IN_PROGRESS):**
  - QR token động có TTL, replay attack prevention và quét QR realtime trên camera.
  - Check-out và thuật toán tính toán thời lượng tham gia tối thiểu.
  - Tự động kết toán điểm rèn luyện khi hoạt động chuyển `COMPLETED`.
  - Quy trình gửi đề xuất điều chỉnh điểm (`PointAdjustments`) và xét duyệt đa cấp.
  - Bảng xếp hạng toàn trường/khoa/lớp và đánh giá danh hiệu/huy hiệu (`Achievements`).
  - Hệ thống chuông thông báo realtime trong ứng dụng (`Notifications`).
  - Dashboard báo cáo thống kê chuyên sâu, biểu đồ và xuất báo cáo Excel/PDF.
  - Giao diện tra cứu nhật ký kiểm toán `AuditLogs`.
