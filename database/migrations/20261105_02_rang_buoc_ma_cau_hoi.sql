-- CampusExam - ràng buộc mã câu hỏi: MACAUHOI = KYHIEU + MAMD + '-' + STT (VD A1-1, B1-24).
-- KYHIEU là chữ cái thứ THUTU của KQHT (A - KQHT 1, B - KQHT 2, ...). Chạy lại được.
USE [CampusExam];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* Kiểm tra dữ liệu hiện có trước khi thêm ràng buộc. */
    IF EXISTS (
        SELECT 1
          FROM dbo.KQHOCTAP
         WHERE THUTU NOT BETWEEN 1 AND 26
            OR KYHIEU COLLATE Latin1_General_100_BIN2 <> CHAR(64 + THUTU)
    )
        THROW 50001,
            N'Có KQHT có KYHIEU không khớp THUTU (1 → A, 2 → B, ...). Chạy truy vấn rà soát (1) ở cuối file.',
            1;

    IF EXISTS (
        SELECT 1
          FROM dbo.CAUHOI AS ch
          JOIN dbo.KQHOCTAP AS kq ON kq.MAKQHT = ch.MAKQHT
         WHERE ch.MACAUHOI COLLATE Latin1_General_100_BIN2
                   <> kq.KYHIEU + CAST(ch.MAMD AS varchar(1)) + '-' + CAST(ch.STT AS varchar(10))
            OR DATALENGTH(ch.MACAUHOI) <> 3 + LEN(CAST(ch.STT AS varchar(10)))
    )
        THROW 50002,
            N'Có CAUHOI.MACAUHOI không khớp [KyHieu][MaMD]-[STT]. Chạy truy vấn rà soát (2) ở cuối file.',
            1;

    /* 1. Ký hiệu KQHT là chữ cái thứ THUTU: 1 → A, 2 → B, ..., 26 → Z. */
    IF OBJECT_ID(N'dbo.CKT_KQHOCTAP_KYHIEU', N'C') IS NULL
        ALTER TABLE dbo.KQHOCTAP WITH CHECK
            ADD CONSTRAINT CKT_KQHOCTAP_KYHIEU
            CHECK (
                THUTU BETWEEN 1 AND 26
                AND KYHIEU COLLATE Latin1_General_100_BIN2 = CHAR(64 + THUTU)
            );

    /* Khóa đích cho khóa ngoại kép từ CAUHOI. */
    IF OBJECT_ID(N'dbo.AK_KQHOCTAP_MAKQHT_KYHIEU', N'UQ') IS NULL
        ALTER TABLE dbo.KQHOCTAP
            ADD CONSTRAINT AK_KQHOCTAP_MAKQHT_KYHIEU UNIQUE (MAKQHT, KYHIEU);

    /* 2. CAUHOI.KYHIEU, lấy giá trị từ KQHT hiện tại. */
    IF COL_LENGTH(N'dbo.CAUHOI', N'KYHIEU') IS NULL
        ALTER TABLE dbo.CAUHOI ADD KYHIEU char(1) NULL;

    /* Các lệnh tham chiếu cột mới chạy qua EXEC để được biên dịch sau khi cột tồn tại. */
    EXEC (N'
        UPDATE ch
           SET ch.KYHIEU = kq.KYHIEU
          FROM dbo.CAUHOI AS ch
          JOIN dbo.KQHOCTAP AS kq ON kq.MAKQHT = ch.MAKQHT
         WHERE ch.KYHIEU IS NULL
            OR ch.KYHIEU COLLATE Latin1_General_100_BIN2 <> kq.KYHIEU;

        ALTER TABLE dbo.CAUHOI ALTER COLUMN KYHIEU char(1) NOT NULL;');

    IF OBJECT_ID(N'dbo.FK_CAUHOI_KQHOCTAP_KYHIEU', N'F') IS NULL
        EXEC (N'
            ALTER TABLE dbo.CAUHOI WITH CHECK
                ADD CONSTRAINT FK_CAUHOI_KQHOCTAP_KYHIEU
                FOREIGN KEY (MAKQHT, KYHIEU)
                REFERENCES dbo.KQHOCTAP (MAKQHT, KYHIEU);');

    /* 3. Mã câu hỏi đúng bằng KYHIEU + MAMD + ''-'' + STT. */
    IF OBJECT_ID(N'dbo.CKT_CAUHOI_MACAUHOI', N'C') IS NULL
        EXEC (N'
            ALTER TABLE dbo.CAUHOI WITH CHECK
                ADD CONSTRAINT CKT_CAUHOI_MACAUHOI
                CHECK (
                    MACAUHOI COLLATE Latin1_General_100_BIN2
                        = KYHIEU + CAST(MAMD AS varchar(1)) + ''-'' + CAST(STT AS varchar(10))
                    AND DATALENGTH(MACAUHOI) = 3 + LEN(CAST(STT AS varchar(10)))
                );');

    COMMIT TRANSACTION;
    PRINT N'Đã áp dụng ràng buộc mã câu hỏi.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Truy vấn rà soát khi script báo lỗi.

-- (1) KQHT có ký hiệu không khớp thứ tự
SELECT MAKQHT, TENKQHT, THUTU, KYHIEU,
       CASE WHEN THUTU BETWEEN 1 AND 26 THEN CHAR(64 + THUTU) END AS KYHIEU_DUNG
  FROM dbo.KQHOCTAP
 WHERE THUTU NOT BETWEEN 1 AND 26
    OR KYHIEU COLLATE Latin1_General_100_BIN2 <> CHAR(64 + THUTU);

-- (2) Mã câu hỏi không khớp ký hiệu KQHT, mức độ hoặc STT
SELECT ch.MACH, ch.MACAUHOI,
       kq.KYHIEU + CAST(ch.MAMD AS varchar(1)) + '-' + CAST(ch.STT AS varchar(10)) AS MA_DUNG
  FROM dbo.CAUHOI AS ch
  JOIN dbo.KQHOCTAP AS kq ON kq.MAKQHT = ch.MAKQHT
 WHERE ch.MACAUHOI COLLATE Latin1_General_100_BIN2
           <> kq.KYHIEU + CAST(ch.MAMD AS varchar(1)) + '-' + CAST(ch.STT AS varchar(10))
    OR DATALENGTH(ch.MACAUHOI) <> 3 + LEN(CAST(ch.STT AS varchar(10)));
*/
