namespace CampusExam.Application.Kqht;

public enum CreateKqhtStatus
{
    Created,
    DuplicateKyHieu
}

public sealed record CreateKqhtResult(
    CreateKqhtStatus Status,
    KqhtDto? Kqht)
{
    public static CreateKqhtResult Success(KqhtDto kqht) =>
        new(CreateKqhtStatus.Created, kqht);

    public static CreateKqhtResult Duplicate() =>
        new(CreateKqhtStatus.DuplicateKyHieu, null);
}
