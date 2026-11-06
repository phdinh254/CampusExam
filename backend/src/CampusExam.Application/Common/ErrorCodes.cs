namespace CampusExam.Application.Common;

/// <summary>
/// Mã lỗi ổn định trả trong trường <c>code</c> của ProblemDetails. Frontend rẽ nhánh theo
/// các mã này, nên chỉ được thêm mới, không đổi tên mã đã phát hành.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";

    public const string KqhtNotFound = "KQHT_NOT_FOUND";
    public const string KqhtKyHieuConflict = "KQHT_KY_HIEU_CONFLICT";

    public const string QuestionImportEmptyFile = "QUESTION_IMPORT_EMPTY_FILE";
    public const string QuestionImportFileTooLarge = "QUESTION_IMPORT_FILE_TOO_LARGE";
    public const string QuestionImportUnsupportedFileType = "QUESTION_IMPORT_UNSUPPORTED_FILE_TYPE";
    public const string QuestionImportInvalidFile = "QUESTION_IMPORT_INVALID_FILE";
}
