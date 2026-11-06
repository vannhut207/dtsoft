# KTHC Offline — kế toán HCSN & xây dựng cơ bản

C# WinForms · .NET Framework 4.8 · SQL Server Express LocalDB · Windows.

**Trạng thái: bản phát triển 0.1, chưa đủ điều kiện dùng cho sổ sách chính thức.** Phạm vi yêu cầu là phần mềm kế toán HCSN đầy đủ theo TT24/2024/TT-BTC và quản lý XDCB; kho này cung cấp lõi chạy thử và danh sách phần còn phải hoàn thiện, không chứng nhận tuân thủ.

## Đã triển khai

- Tự tạo database, bảng và quan hệ, kiểm tra dữ liệu, transaction, chỉ mục.
- Danh mục tài khoản tối thiểu, nguồn, mục lục NS, đối tượng, thông tin đơn vị.
- Chứng từ nhiều dòng, nháp/ghi sổ, cân Nợ/Có trong bảng, ghi đơn ngoài bảng, không sửa/xóa chứng từ đã ghi sổ, đảo chứng từ bằng giao dịch nguyên tử, khóa/mở kỳ, audit.
- Sổ nhật ký, cân đối phát sinh, công nợ, tổng hợp theo nguồn/MLNS, lọc thời gian và dự án, xuất CSV UTF-8.
- XDCB: dự án, tổng mức đầu tư, kế hoạch vốn theo năm/nguồn, hợp đồng/nhà thầu, nghiệm thu, chi phí 241 theo khoản mục; đối chiếu lũy kế hợp đồng. Kiểm tra nghiệm thu vượt hợp đồng, vốn vượt tổng mức và hợp đồng sai dự án/nhà thầu.
- Sổ nhập/xuất số lượng, vật tư, tài sản, nhân viên và bảng lương nhập liệu. Đây là sổ chi tiết độc lập, chưa tự động hạch toán.
- Sao lưu SQL `.bak` có checksum; hướng dẫn khôi phục thử qua SSMS.
- Workflow Windows build, kiểm thử tích hợp LocalDB và artifact EXE sau khi kiểm thử thành công.

## Chạy thử

Xem [hướng dẫn cài đặt](docs/INSTALL.md). Không có package NuGet. Database tạo tự động ở lần chạy đầu. Chưa đóng gói bộ cài tự cài LocalDB/.NET.

## Còn thiếu trước khi nghiệm thu đầy đủ

Xem [ma trận chức năng](docs/SCOPE.md): toàn bộ tài khoản và mẫu sổ pháp định, BCTC/quyết toán, phân quyền/duyệt, tự động kho–lương–tài sản, nghiệp vụ Kho bạc, chuyển số dư, quyết toán dự án và kiểm thử nghiệp vụ bởi đơn vị sử dụng.

## Căn cứ cần đối chiếu

- [TT24/2024/TT-BTC, Công báo chính thức, đủ các phần phụ lục](https://congbao.chinhphu.vn/van-ban/thong-tu-so-24-2024-tt-btc-41775/49886.htm).
- [Bộ Tài chính thông tin TT46/2025/TT-BTC sửa đổi TT24, hiệu lực 01/07/2025](https://www.mof.gov.vn/tin-tuc-tai-chinh/tin-tuc-su-kien-8/bo-tai-chinh-sua-doi-loat-thong-tu-trong-linh-vuc-ke-toan-kiem-toan-nham-day-manh-phan-cap-phan-quyen).

Đây mới là các nguồn đã xác định, chưa phải kết luận rà soát toàn bộ văn bản có hiệu lực đến ngày triển khai. XDCB cần thêm quy định quản lý vốn đầu tư, thanh toán/quyết toán và chế độ áp dụng theo vai trò đơn vị/chủ đầu tư/BQLDA; TT24 riêng lẻ không bao quát mọi nghiệp vụ đó.
