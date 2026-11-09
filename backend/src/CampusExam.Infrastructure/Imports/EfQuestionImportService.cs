using System.Text.Json;
using System.Text.RegularExpressions;
using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Application.QuestionImports;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Imports;

/// <summary>
/// Nhập câu hỏi từ DOCX: xem trước có đối chiếu database, xác nhận ghi tất cả hoặc không ghi gì
/// (D-IMPORT-01, D-IMPORT-TRUNG-01). Hình lưu ở <see cref="IFileStorage"/> (D-MEDIA-01).
/// </summary>
public sealed partial class EfQuestionImportService(
    CampusExamDbContext dbContext,
    IQuestionDocxParser parser,
    IFileStorage storage,
    TimeProvider timeProvider) : IQuestionImportService
{
    private const string ThuMucTam = "tam/nhap-cau-hoi";
    private const int DoDaiNoiDung = 4000;
    private const int DoDaiPhuongAn = 1000;

    public async Task<QuestionImportPreview> XemTruocAsync(
        Stream docx,
        string tenFile,
        NguoiThucHien nguoi,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await DonTepTamCuAsync(now, cancellationToken);

        var preview = await DoiChieuDatabaseAsync(await parser.PreviewAsync(docx, cancellationToken), cancellationToken);
        if (preview.TotalRows == 0 || preview.ErrorRows > 0)
        {
            return preview;
        }

        var maXemTruoc = Guid.NewGuid().ToString("N");
        var hetHan = now + IQuestionImportService.ThoiHanXemTruoc;

        docx.Position = 0;
        await storage.SaveAsync(KeyTep(maXemTruoc), docx, cancellationToken);
        using var meta = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new ThongTinXemTruoc(nguoi.MaTK, tenFile, hetHan)));
        await storage.SaveAsync(KeyThongTin(maXemTruoc), meta, cancellationToken);

        return preview with { PreviewId = maXemTruoc, ExpiresAt = hetHan };
    }

    public async Task<KetQua<LanImportDto>> XacNhanAsync(
        string maXemTruoc,
        NguoiThucHien nguoi,
        CancellationToken cancellationToken)
    {
        if (!MaXemTruocHopLe().IsMatch(maXemTruoc))
        {
            return KhongCoXemTruoc();
        }

        var thongTin = await DocThongTinAsync(maXemTruoc, cancellationToken);
        // Bản xem trước của tài khoản khác coi như không tồn tại.
        if (thongTin is null || thongTin.MaTK != nguoi.MaTK)
        {
            return KhongCoXemTruoc();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (now > thongTin.HetHan)
        {
            await XoaXemTruocAsync(maXemTruoc, cancellationToken);
            return new LoiNghiepVu(
                LoaiLoi.HetHan,
                ErrorCodes.ImportPreviewExpired,
                "Bản xem trước đã hết hạn",
                "Bản xem trước chỉ giữ 30 phút. Hãy tải tệp lên lại.");
        }

        QuestionImportDocument taiLieu;
        await using (var tep = await storage.OpenReadAsync(KeyTep(maXemTruoc), cancellationToken))
        {
            if (tep is null)
            {
                return KhongCoXemTruoc();
            }

            using var buffer = new MemoryStream();
            await tep.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            taiLieu = await parser.ReadAsync(buffer, cancellationToken);
        }

        // Kiểm tra lại: từ lúc xem trước có thể đã có câu hỏi trùng mã hoặc KQHT bị ngừng dùng.
        var preview = await DoiChieuDatabaseAsync(taiLieu.Preview, cancellationToken);
        if (preview.ErrorRows > 0)
        {
            await XoaXemTruocAsync(maXemTruoc, cancellationToken);
            return CoLoi("Dữ liệu đã thay đổi kể từ lúc xem trước (mã câu hỏi trùng hoặc KQHT đã ngừng dùng). Hãy tải tệp lên lại để xem lỗi.");
        }

        var kqht = await dbContext.KqHocTaps.AsNoTracking()
            .ToDictionaryAsync(x => x.KyHieu, x => x.MaKQHT, cancellationToken);

        // Lưu hình trước; nếu ghi database thất bại thì xóa các file vừa lưu. Chỉ dọn file trước khi
        // commit: sau commit, file thuộc về dữ liệu đã ghi và không được xóa dù bước sau có lỗi.
        var keyHinh = new Dictionary<QuestionImportMediaContent, string>(ReferenceEqualityComparer.Instance);
        int maLanImport;
        try
        {
            foreach (var hinh in taiLieu.Media)
            {
                var key = $"cau-hoi/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{DuoiFile(hinh.ContentType)}";
                using var data = new MemoryStream(hinh.Data, writable: false);
                await storage.SaveAsync(key, data, cancellationToken);
                keyHinh[hinh] = key;
            }

            maLanImport = await GhiDatabaseAsync(preview, taiLieu.Media, keyHinh, kqht, thongTin.TenFile, nguoi, now, cancellationToken);
        }
        catch (DbUpdateException exception) when (SqlLoi.LaTrungKhoa(exception))
        {
            await XoaFileAsync(keyHinh.Values);
            return CoLoi("Một số mã câu hỏi vừa được thêm vào ngân hàng trong lúc nhập. Hãy tải tệp lên lại để xem lỗi.");
        }
        catch
        {
            await XoaFileAsync(keyHinh.Values);
            throw;
        }

        await XoaXemTruocAsync(maXemTruoc, CancellationToken.None);
        return KetQua<LanImportDto>.Ok((await GetByIdAsync(maLanImport, CancellationToken.None))!);
    }

    public async Task<TrangDuLieu<LanImportDto>> GetPageAsync(TruyVanTrang truyVan, CancellationToken cancellationToken)
    {
        var nguon =
            from lan in dbContext.LanImports.AsNoTracking()
            join taiKhoan in dbContext.TaiKhoans.AsNoTracking() on lan.MaTK equals taiKhoan.MaTK
            where lan.LoaiImport == LanImport.LoaiCauHoi
            select new { Lan = lan, taiKhoan.HoTen };
        var tong = await nguon.CountAsync(cancellationToken);
        var duLieu = await nguon
            .OrderByDescending(x => x.Lan.ThoiGian)
            .ThenByDescending(x => x.Lan.MaLanImport)
            .Skip(truyVan.SoDongBoQua)
            .Take(truyVan.KichThuocTrang)
            .Select(x => new LanImportDto(
                x.Lan.MaLanImport,
                x.Lan.TenFile,
                x.Lan.TongDong,
                x.Lan.SoThanhCong,
                x.Lan.SoDongLoi,
                x.Lan.ThoiGian,
                (TrangThaiLanImport)x.Lan.TrangThai,
                x.HoTen))
            .ToListAsync(cancellationToken);
        return new TrangDuLieu<LanImportDto>(duLieu, truyVan.Trang, truyVan.KichThuocTrang, tong);
    }

    public Task<LanImportDto?> GetByIdAsync(int maLanImport, CancellationToken cancellationToken) =>
        ChieuDto(dbContext.LanImports.AsNoTracking()
                .Where(x => x.LoaiImport == LanImport.LoaiCauHoi && x.MaLanImport == maLanImport))
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>Chiếu sang DTO sau cùng: EF không dịch được Where/OrderBy đặt sau phép chiếu record.</summary>
    private IQueryable<LanImportDto> ChieuDto(IQueryable<LanImport> nguon) =>
        from lan in nguon
        join taiKhoan in dbContext.TaiKhoans.AsNoTracking() on lan.MaTK equals taiKhoan.MaTK
        select new LanImportDto(
            lan.MaLanImport,
            lan.TenFile,
            lan.TongDong,
            lan.SoThanhCong,
            lan.SoDongLoi,
            lan.ThoiGian,
            (TrangThaiLanImport)lan.TrangThai,
            taiKhoan.HoTen);

    private async Task<int> GhiDatabaseAsync(
        QuestionImportPreview preview,
        IReadOnlyList<QuestionImportMediaContent> media,
        IReadOnlyDictionary<QuestionImportMediaContent, string> keyHinh,
        IReadOnlyDictionary<string, int> kqht,
        string tenFile,
        NguoiThucHien nguoi,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var lanImport = new LanImport
            {
                MaTK = nguoi.MaTK,
                LoaiImport = LanImport.LoaiCauHoi,
                TenFile = Cat(tenFile, 255),
                TongDong = preview.TotalRows,
                SoThanhCong = preview.TotalRows,
                SoDongLoi = 0,
                ThoiGian = now,
                TrangThai = (byte)TrangThaiLanImport.HoanTat
            };
            dbContext.LanImports.Add(lanImport);
            await dbContext.SaveChangesAsync(cancellationToken);

            var cauHoiTheoDong = new Dictionary<int, CauHoi>();
            foreach (var dong in preview.Rows)
            {
                var cauHoi = new CauHoi
                {
                    MaLanImport = lanImport.MaLanImport,
                    MaKQHT = kqht[dong.KqhtSymbol!],
                    KyHieu = dong.KqhtSymbol!,
                    MaMD = dong.Difficulty!.Value,
                    Stt = dong.SequenceNumber!.Value,
                    MaCauHoi = dong.QuestionCode!,
                    NoiDung = dong.QuestionText,
                    DaA = dong.AnswerA,
                    DaB = dong.AnswerB,
                    DaC = dong.AnswerC,
                    DaD = dong.AnswerD,
                    DapAnDung = dong.CorrectAnswer!,
                    CoDinhDa = 0,
                    TrangThai = CauHoi.TrangThaiDangSuDung,
                    NgayTao = now
                };
                dbContext.CauHois.Add(cauHoi);
                cauHoiTheoDong[dong.RowNumber] = cauHoi;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var hinh in media)
            {
                dbContext.TaiNguyenCauHois.Add(new TaiNguyenCauHoi
                {
                    MaCH = cauHoiTheoDong[hinh.RowNumber].MaCH,
                    ViTri = hinh.Position,
                    ThuTu = hinh.Order,
                    TenFile = Cat(hinh.FileName, 255),
                    StorageKey = keyHinh[hinh],
                    MimeType = Cat(hinh.ContentType, 100),
                    NgayTao = now
                });
            }

            dbContext.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                MaTK = nguoi.MaTK,
                Nhom = "CAUHOI",
                HanhDong = "Nhập câu hỏi",
                DoiTuong = $"LANIMPORT:{lanImport.MaLanImport}",
                ChiTiet = Cat($"{preview.TotalRows} câu hỏi, {media.Count} hình từ {tenFile}", 2000),
                ThoiGian = now,
                DiaChiIP = nguoi.DiaChiIP is { Length: > 45 } ip ? ip[..45] : nguoi.DiaChiIP
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return lanImport.MaLanImport;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    /// <summary>Thêm lỗi chỉ phát hiện được khi đối chiếu database và giới hạn độ dài cột.</summary>
    private async Task<QuestionImportPreview> DoiChieuDatabaseAsync(QuestionImportPreview preview, CancellationToken cancellationToken)
    {
        var kyHieu = preview.Rows.Select(x => x.KqhtSymbol).OfType<string>().Distinct().ToList();
        var maCauHoi = preview.Rows.Where(x => x.KqhtSymbol is not null).Select(x => x.QuestionCode).OfType<string>().ToList();

        var kqht = await dbContext.KqHocTaps.AsNoTracking()
            .Where(x => kyHieu.Contains(x.KyHieu))
            .ToDictionaryAsync(x => x.KyHieu, x => x.TrangThai, cancellationToken);
        var daCo = (await dbContext.CauHois.AsNoTracking()
                .Where(x => maCauHoi.Contains(x.MaCauHoi))
                .Select(x => x.MaCauHoi)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var dong = preview.Rows.Select(row =>
        {
            var them = new List<QuestionImportIssue>();
            if (row.KqhtSymbol is { } ky)
            {
                if (!kqht.TryGetValue(ky, out var trangThai))
                {
                    them.Add(Loi("KQHT_NOT_FOUND", $"Chưa có KQHT ký hiệu {ky} (KQHT thứ {ky[0] - 'A' + 1}); hãy tạo KQHT trước khi nhập."));
                }
                else if (trangThai != (short)TrangThaiSuDung.DangSuDung)
                {
                    them.Add(Loi("KQHT_INACTIVE", $"KQHT {ky} đã ngừng sử dụng nên không nhận câu hỏi mới."));
                }
            }

            if (row.QuestionCode is { } ma && daCo.Contains(ma))
            {
                them.Add(Loi("DUPLICATE_IN_DATABASE", $"Mã câu hỏi {ma} đã có trong ngân hàng câu hỏi."));
            }

            if (row.QuestionText.Length > DoDaiNoiDung)
            {
                them.Add(Loi("CONTENT_TOO_LONG", $"Nội dung câu hỏi dài {row.QuestionText.Length} ký tự, vượt giới hạn {DoDaiNoiDung}."));
            }

            foreach (var (nhan, phuongAn) in new[] { ("A", row.AnswerA), ("B", row.AnswerB), ("C", row.AnswerC), ("D", row.AnswerD) })
            {
                if (phuongAn.Length > DoDaiPhuongAn)
                {
                    them.Add(Loi("ANSWER_TOO_LONG", $"Phương án {nhan} dài {phuongAn.Length} ký tự, vượt giới hạn {DoDaiPhuongAn}."));
                }
            }

            return them.Count == 0 ? row : row with { Issues = [.. row.Issues, .. them] };
        }).ToList();

        return QuestionImportPreview.FromRows(dong);
    }

    private async Task<ThongTinXemTruoc?> DocThongTinAsync(string maXemTruoc, CancellationToken cancellationToken)
    {
        await using var stream = await storage.OpenReadAsync(KeyThongTin(maXemTruoc), cancellationToken);
        return stream is null
            ? null
            : await JsonSerializer.DeserializeAsync<ThongTinXemTruoc>(stream, cancellationToken: cancellationToken);
    }

    private async Task XoaXemTruocAsync(string maXemTruoc, CancellationToken cancellationToken)
    {
        await storage.DeleteAsync(KeyTep(maXemTruoc), cancellationToken);
        await storage.DeleteAsync(KeyThongTin(maXemTruoc), cancellationToken);
    }

    /// <summary>Dọn bản xem trước bị bỏ dở (quá một ngày).</summary>
    private async Task DonTepTamCuAsync(DateTime now, CancellationToken cancellationToken)
    {
        foreach (var key in storage.ListOlderThan(ThuMucTam, now.AddDays(-1)))
        {
            await storage.DeleteAsync(key, cancellationToken);
        }
    }

    private async Task XoaFileAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            await storage.DeleteAsync(key, CancellationToken.None);
        }
    }

    private static string KeyTep(string maXemTruoc) => $"{ThuMucTam}/{maXemTruoc}.docx";

    private static string KeyThongTin(string maXemTruoc) => $"{ThuMucTam}/{maXemTruoc}.json";

    private static string DuoiFile(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/bmp" => ".bmp",
        "image/tiff" => ".tiff",
        "image/x-emf" => ".emf",
        "image/x-wmf" => ".wmf",
        _ => ".bin"
    };

    private static string Cat(string value, int max) => value.Length <= max ? value : value[..max];

    private static QuestionImportIssue Loi(string code, string message) =>
        new(code, message, QuestionImportIssueSeverity.Error);

    private static LoiNghiepVu KhongCoXemTruoc() => new(
        LoaiLoi.KhongTimThay,
        ErrorCodes.ImportPreviewNotFound,
        "Không tìm thấy bản xem trước",
        "Bản xem trước không tồn tại, đã được xác nhận hoặc thuộc tài khoản khác.");

    private static LoiNghiepVu CoLoi(string chiTiet) => new(
        LoaiLoi.XungDot,
        ErrorCodes.QuestionImportHasErrors,
        "Tệp nhập còn lỗi",
        chiTiet);

    private sealed record ThongTinXemTruoc(int MaTK, string TenFile, DateTime HetHan);

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex MaXemTruocHopLe();
}
