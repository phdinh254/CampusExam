/* CampusExam - bổ sung metadata tài nguyên câu hỏi.
   Chạy trên database CampusExam hiện có trước khi triển khai import DOCX.
   Script không xóa hoặc thay đổi dữ liệu câu hỏi hiện hữu.
*/
USE [CampusExam];
GO

IF OBJECT_ID(N'dbo.TAINGUYEN_CAUHOI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TAINGUYEN_CAUHOI] (
       MATN                 int                  identity,
       MACH                 int                  not null,
       VITRI                varchar(10)          not null,
       THUTU                smallint             not null,
       TENFILE              nvarchar(255)        not null,
       STORAGEKEY           varchar(500)         not null,
       MIMETYPE             varchar(100)         not null,
       ALTTEXT              nvarchar(500)        null,
       NGAYTAO              datetime2            not null,
       CONSTRAINT PK_TAINGUYEN_CAUHOI PRIMARY KEY NONCLUSTERED (MATN),
       CONSTRAINT AK_TAINGUYEN_CAUHOI_STORAGEKEY UNIQUE (STORAGEKEY),
       CONSTRAINT AK_TAINGUYEN_CAUHOI_VITRI UNIQUE (MACH, VITRI, THUTU),
       CONSTRAINT CKT_TAINGUYEN_CAUHOI CHECK (
          [VITRI] IN ('NOIDUNG','DAA','DAB','DAC','DAD') AND [THUTU] > 0
       ),
       CONSTRAINT FK_TAINGUYEN_CAUHOI_THUOC_CAUHOI FOREIGN KEY (MACH)
          REFERENCES [dbo].[CAUHOI] (MACH)
    );

    CREATE INDEX THUOC_CAUHOI_FK ON [dbo].[TAINGUYEN_CAUHOI] (MACH ASC);
END;
GO
