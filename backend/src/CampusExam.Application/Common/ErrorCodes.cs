namespace CampusExam.Application.Common;

/// <summary>
/// Mã lỗi ổn định trả trong trường <c>code</c> của ProblemDetails. Frontend rẽ nhánh theo
/// các mã này, nên chỉ được thêm mới, không đổi tên mã đã phát hành.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Forbidden = "FORBIDDEN";
    public const string RateLimited = "RATE_LIMITED";

    public const string AuthUnauthenticated = "AUTH_UNAUTHENTICATED";
    public const string AuthTokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthAccountLocked = "AUTH_ACCOUNT_LOCKED";
    public const string AuthAccountDisabled = "AUTH_ACCOUNT_DISABLED";
    public const string AuthRefreshInvalid = "AUTH_REFRESH_INVALID";
    public const string AuthCsrfFailed = "AUTH_CSRF_FAILED";
    public const string AuthPasswordChangeRequired = "AUTH_PASSWORD_CHANGE_REQUIRED";
    public const string AuthCurrentPasswordIncorrect = "AUTH_CURRENT_PASSWORD_INCORRECT";

    public const string KqhtNotFound = "KQHT_NOT_FOUND";
    public const string KqhtKyHieuConflict = "KQHT_KY_HIEU_CONFLICT";
    public const string KqhtInUse = "KQHT_IN_USE";
    public const string KqhtInactive = "KQHT_INACTIVE";

    public const string QuestionNotFound = "QUESTION_NOT_FOUND";
    public const string QuestionInUse = "QUESTION_IN_USE";

    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

    public const string StudentNotFound = "STUDENT_NOT_FOUND";
    public const string StudentMstsConflict = "STUDENT_MSTS_CONFLICT";
    public const string StudentCccdConflict = "STUDENT_CCCD_CONFLICT";
    public const string StudentInUse = "STUDENT_IN_USE";

    public const string QuestionImportEmptyFile = "QUESTION_IMPORT_EMPTY_FILE";
    public const string QuestionImportFileTooLarge = "QUESTION_IMPORT_FILE_TOO_LARGE";
    public const string QuestionImportUnsupportedFileType = "QUESTION_IMPORT_UNSUPPORTED_FILE_TYPE";
    public const string QuestionImportInvalidFile = "QUESTION_IMPORT_INVALID_FILE";
}
