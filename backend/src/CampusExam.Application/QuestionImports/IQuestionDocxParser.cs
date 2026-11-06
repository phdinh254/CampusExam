namespace CampusExam.Application.QuestionImports;

public interface IQuestionDocxParser
{
    Task<QuestionImportPreview> PreviewAsync(
        Stream docxStream,
        CancellationToken cancellationToken);
}
