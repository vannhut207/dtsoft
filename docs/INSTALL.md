# Cài đặt và chạy thử trên Windows

Bản 0.1 là bản phát triển. Chưa phải phần mềm kế toán đầy đủ đã nghiệm thu TT24.

## Điều kiện

- Windows 10/11, .NET Framework 4.8.
- SQL Server Express **LocalDB**, instance `MSSQLLocalDB`. LocalDB là thành phần cài riêng, không kèm Windows/.NET. Cài trên máy dùng ứng dụng trước khi mở EXE. Sau khi cài, ứng dụng chạy không cần internet.
- Nếu tự biên dịch: Visual Studio 2022, workload .NET desktop development và .NET Framework 4.8 targeting pack.

Mở `Kthc.sln`, chọn Release, Build Solution. Hoặc `msbuild Kthc.sln /p:Configuration=Release` trong Developer Command Prompt.
Mở `src\Kthc\bin\Release\Kthc.exe`.

GitHub Actions có workflow build Windows, chạy kiểm thử tích hợp rồi đóng gói artifact `KTHC-Windows-net48`. Chỉ tải artifact của lượt chạy xanh. Trong Actions → Windows build and database tests → lượt chạy thành công → Artifacts. Bản tải này vẫn cần LocalDB và .NET 4.8.

## Lần đầu

1. Database `KthcOffline` được tạo trong LocalDB thuộc tài khoản Windows hiện tại. Không phải database dùng chung giữa các tài khoản.
2. Cấu hình thông tin đơn vị. Chỉ có danh mục tài khoản tối thiểu để kiểm thử; phải bổ sung và đối chiếu toàn bộ danh mục chính thức trước khi sử dụng thực tế.
3. Khai báo nguồn, mục lục ngân sách, đối tượng, dự án rồi hợp đồng, kế hoạch vốn, nghiệm thu.
4. Tạo chứng từ. Mỗi tài khoản là một dòng Nợ hoặc Có. Tài khoản trong bảng phải cân; ngoài bảng ghi đơn và được tách riêng.
5. Lưu nháp; kiểm tra rồi ghi sổ. Chỉ chứng từ đã ghi sổ vào sổ cái, báo cáo công nợ, chi phí XDCB.
6. Các sổ chi tiết vật tư, tài sản, lương, nghiệm thu **không tự tạo chứng từ kế toán**. Phải nhập chứng từ riêng và đối chiếu, tránh bỏ sót hoặc trùng.
7. Số dư đầu kỳ: nhập chứng từ loại `Số dư đầu kỳ` trước ngày đầu kỳ báo cáo; không có trình chuyển đổi số dư TT107 tự động.
8. Sổ nhật ký/cân đối/công nợ/nguồn/241 lọc ngày và dự án; báo cáo tổng hợp dự án/hợp đồng là lũy kế toàn bộ.

## Sao lưu

Chọn `Sao lưu database`, chọn tên `.bak` mới. Dùng SQL native BACKUP WITH CHECKSUM, không chép MDF khi database đang chạy. Lưu thêm bản sao ở thiết bị khác. Không đẩy `.bak`, `.mdf`, dữ liệu thật lên kho GitHub công khai.

## Khôi phục (quản trị bằng SSMS)

Đóng ứng dụng, kết nối SSMS đến `(localdb)\MSSQLLocalDB` bằng Windows Authentication. Sao lưu hiện trạng trước khi khôi phục. Dùng giao diện Restore Database, chọn file `.bak`, khôi phục thử vào database **mới** trước; kiểm tra ngày sao lưu, số chứng từ, tổng phát sinh và số dư. Dùng `RESTORE VERIFYONLY ... WITH CHECKSUM` để kiểm tra media; thao tác này không thay việc khôi phục thử. Chỉ thay database `KthcOffline` sau khi đối chiếu. Ứng dụng chưa có nút khôi phục hay xác thực quản trị riêng.

## Phân quyền và bảo mật

Phiên bản này dựa vào tài khoản Windows và LocalDB cá nhân. Chưa có phân quyền người lập/duyệt/kế toán trưởng, mã hóa database, chữ ký số hoặc nhật ký chống quản trị viên sửa. Không triển khai dùng chung nhiều người. Người có quyền truy cập trực tiếp database có thể thay cấu hình hoặc vô hiệu trigger. Nhật ký hiện tại hỗ trợ đối chiếu nội bộ.

## Đọc báo cáo

`Debit`, `Credit`: phát sinh Nợ/Có; `OpeningNet`, `ClosingNet`: Nợ trừ Có. Số âm thể hiện dư Có, chưa tách cột dư Nợ/dư Có theo mẫu pháp định. `PayableBalance`: Có trừ Nợ TK 331. `Debit331` không đồng nghĩa tiền thanh toán, có thể gồm tạm ứng hoặc bù trừ. `ConstructionBalance`: Nợ trừ Có TK 241 và tài khoản chi tiết. Báo cáo này không thay bộ BCTC/quyết toán theo Phụ lục III–IV.
