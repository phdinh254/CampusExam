using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;

namespace CampusExam.Application.ThiSinh;

public interface IThiSinhQuery
{
    Task<TrangDuLieu<ThiSinhTomTatDto>> GetPageAsync(TruyVanThiSinh truyVan, CancellationToken cancellationToken);

    /// <summary>Các lớp đang có thí sinh, cho bộ lọc.</summary>
    Task<IReadOnlyList<string>> GetLopAsync(CancellationToken cancellationToken);

    Task<ThiSinhChiTietDto?> GetByMstsAsync(string msts, CancellationToken cancellationToken);
}

public interface IThiSinhCommandService
{
    /// <summary>Tạo hồ sơ và tài khoản trong cùng transaction (D-THISINH-01).</summary>
    Task<KetQua<ThiSinhChiTietDto>> TaoAsync(TaoThiSinhCommand command, NguoiThucHien nguoi, CancellationToken cancellationToken);

    /// <summary>Sửa hồ sơ; đổi ngày sinh không đổi mật khẩu.</summary>
    Task<KetQua<ThiSinhChiTietDto>> SuaAsync(string msts, SuaThiSinhCommand command, NguoiThucHien nguoi, CancellationToken cancellationToken);

    /// <summary>Khóa thì thu hồi mọi phiên và vô hiệu access token ngay.</summary>
    Task<KetQua<ThiSinhChiTietDto>> DoiTrangThaiTaiKhoanAsync(string msts, TrangThaiTaiKhoan trangThai, NguoiThucHien nguoi, CancellationToken cancellationToken);

    /// <summary>Đặt lại mật khẩu về ngày sinh, bắt buộc đổi, thu hồi mọi phiên.</summary>
    Task<KetQua<ThiSinhChiTietDto>> CapLaiMatKhauAsync(string msts, NguoiThucHien nguoi, CancellationToken cancellationToken);

    /// <summary>Xóa thí sinh và tài khoản; chỉ khi chưa được phân công kỳ thi nào.</summary>
    Task<KetQua<KhongCo>> XoaAsync(string msts, NguoiThucHien nguoi, CancellationToken cancellationToken);
}
