# Phạm vi và tiêu chí nghiệm thu

## Mục tiêu

Kế toán HCSN offline theo chế độ áp dụng cho đơn vị, có dữ liệu XDCB liên kết sổ cái. Bản 0.1 chưa đáp ứng toàn bộ mục tiêu.

| Phân hệ | Bản 0.1 | Phải hoàn thiện để nghiệm thu |
|---|---|---|
| Cơ sở dữ liệu | SQL LocalDB, transaction, khóa ngoại, backup | Migration nâng cấp, khôi phục giao diện, thử phục hồi định kỳ |
| Đơn vị/kỳ | Một đơn vị, khóa tháng | Nhiều niên độ, số dư, kết chuyển, khóa năm, quản lý mở lại kỳ có phê duyệt |
| Tài khoản | Danh mục tối thiểu thử nghiệm | Đủ phụ lục, tài khoản chi tiết, kiểm tra quan hệ tài khoản, chuyển đổi TT107 |
| Chứng từ | Nháp, ghi sổ, đảo; audit | Phê duyệt, đính kèm, đánh số theo loại/năm, in phiếu và ký |
| Tiền mặt/tiền gửi | Hạch toán qua chứng từ, sổ lọc | Sổ quỹ đúng mẫu, đối chiếu ngân hàng/Kho bạc, ngoại tệ/tỷ giá |
| Dự toán/nguồn | Danh mục nguồn, MLNS, tài khoản ngoài bảng | Giao/rút/hủy/chuyển dự toán, cam kết chi, kiểm soát nguồn, đối chiếu KBNN |
| Công nợ | Tổng hợp từ sổ cái | Hạn nợ, tạm ứng, thanh toán chi tiết, xác nhận đối tượng |
| Vật tư/CCDC | Nhập xuất và số lượng tồn, chặn âm | Phương pháp giá xuất, điều chuyển, kiểm kê, phân bổ và tự hạch toán |
| TSCĐ | Danh mục nguyên giá/hao mòn nhập tay | Tăng/giảm, tính hao mòn/khấu hao, điều chuyển, kiểm kê, tự hạch toán |
| Tiền lương | Nhân viên và bảng lương nhập tay | Chấm công, hệ số/phụ cấp, BHXH/TNCN, tự hạch toán, dữ liệu nhân sự được bảo vệ |
| XDCB | Dự án, kế hoạch vốn, hợp đồng, nghiệm thu, chi phí 241 | Lệnh tạm ứng, thu hồi, thanh toán phân bổ theo nghiệm thu, bảo hành/bảo lãnh, điều chỉnh hợp đồng, quyết toán dự án, bàn giao tài sản và chứng từ liên động |
| Báo cáo | Báo cáo quản trị, CSV | Đầy đủ sổ theo Phụ lục II, quyết toán Phụ lục III, BCTC Phụ lục IV, mapping chỉ tiêu, đối chiếu chéo, in và xuất theo yêu cầu |
| Bảo mật | Windows Authentication, LocalDB cá nhân | Vai trò người dùng, lập/duyệt/ghi sổ, quyền mở kỳ, mã hóa, audit chống sửa, chính sách sao lưu |
| Triển khai | Build workflow Windows, hướng dẫn | EXE được kiểm thử, bộ cài, chữ ký mã, kiểm thử giao diện/DPI và nghiệm thu với kế toán |

## Quy trình XDCB hiện tại

Khai báo dự án → kế hoạch vốn → nhà thầu → hợp đồng → nhập biên bản nghiệm thu. Chứng từ kế toán phải lập riêng và chọn đúng dự án/hợp đồng/đối tượng trên từng dòng. Chọn tài khoản và bút toán theo nghiệp vụ đã được người có chuyên môn duyệt. Bản 0.1 không tự suy diễn nghiệp vụ từ biên bản nghiệm thu.

Chỉ tiêu 241 là số dư chi phí dở dang, không phải tổng giá trị nghiệm thu hay tiền giải ngân. Các khoản thuế, tạm ứng, bù trừ, giữ lại bảo hành và vốn khác cần theo dõi nghiệp vụ riêng trước khi có báo cáo thanh toán đầy đủ.

## Kiểm thử được chuẩn bị

`tests/IntegrationTests.cs` kiểm tra migration lặp lại, nháp không lên sổ, cân đối, ghi sổ hai lần, chặn sửa/xóa, bất biến loại tài khoản, lệch Nợ/Có, trùng số, precision, ngoài bảng, khóa kỳ, đảo và rollback, chi phí 241 và quan hệ hợp đồng/dự án, audit. Workflow chỉ tạo artifact nếu các kiểm thử này qua.

Chưa có kết quả chạy Windows tại thời điểm tạo mã nguồn; xem Actions để biết kết quả thực tế. Kiểm thử này không thay việc kiểm thử toàn bộ chế độ kế toán hay kiểm thử giao diện.
