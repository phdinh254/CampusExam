using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Api.Controllers;
using CampusExam.Application.Kqht;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusExam.IntegrationTests;

public sealed class KqhtControllerTests
{
    [Fact]
    public async Task GetById_returns_item_and_forwards_cancellation_token()
    {
        var expected = new KqhtDto(
            7,
            "Vận dụng",
            3,
            null,
            TrangThaiSuDung.DangSuDung,
            "C");
        var query = new FakeKqhtQuery(expected);
        var controller = new KqhtController(
            query,
            new FakeKqhtCommandService(CreateKqhtResult.Duplicate()));
        using var cancellationSource = new CancellationTokenSource();

        var result = await controller.GetById(
            expected.MaKQHT,
            cancellationSource.Token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
        Assert.Equal(expected.MaKQHT, query.RequestedMaKQHT);
        Assert.Equal(cancellationSource.Token, query.CancellationToken);
    }

    [Fact]
    public async Task GetById_returns_not_found_when_item_does_not_exist()
    {
        var query = new FakeKqhtQuery(null);
        var controller = new KqhtController(
            query,
            new FakeKqhtCommandService(CreateKqhtResult.Duplicate()));

        var result = await controller.GetById(999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);

        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("Không tìm thấy KQHT", problemDetails.Title);
        Assert.Equal("KQHT_NOT_FOUND", problemDetails.Extensions["code"]);
    }

    [Fact]
    public async Task Create_returns_created_at_action_and_forwards_command()
    {
        var created = new KqhtDto(8, "Phân tích", 4, null, TrangThaiSuDung.DangSuDung, "D");
        var commandService = new FakeKqhtCommandService(
            CreateKqhtResult.Success(created));
        var controller = new KqhtController(
            new FakeKqhtQuery(null),
            commandService);
        var command = new CreateKqhtCommand
        {
            TenKQHT = created.TenKQHT,
            ThuTu = created.ThuTu,
            TrangThai = created.TrangThai,
            KyHieu = created.KyHieu
        };
        using var cancellationSource = new CancellationTokenSource();

        var result = await controller.Create(command, cancellationSource.Token);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(KqhtController.GetById), createdResult.ActionName);
        Assert.Equal(created.MaKQHT, createdResult.RouteValues!["maKQHT"]);
        Assert.Same(created, createdResult.Value);
        Assert.Same(command, commandService.Command);
        Assert.Equal(cancellationSource.Token, commandService.CancellationToken);
    }

    [Fact]
    public async Task Create_returns_conflict_for_duplicate_ky_hieu()
    {
        var controller = new KqhtController(
            new FakeKqhtQuery(null),
            new FakeKqhtCommandService(CreateKqhtResult.Duplicate()));
        var command = new CreateKqhtCommand
        {
            TenKQHT = "Nhớ",
            ThuTu = 1,
            TrangThai = TrangThaiSuDung.DangSuDung,
            KyHieu = "a"
        };

        var result = await controller.Create(command, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);

        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("KQHT_KY_HIEU_CONFLICT", problemDetails.Extensions["code"]);
        Assert.Contains("A", problemDetails.Detail);
    }

    private sealed class FakeKqhtQuery(KqhtDto? result) : IKqhtQuery
    {
        public int? RequestedMaKQHT { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<IReadOnlyList<KqhtDto>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            IReadOnlyList<KqhtDto> empty = [];
            return Task.FromResult(empty);
        }

        public Task<KqhtDto?> GetByIdAsync(
            int maKQHT,
            CancellationToken cancellationToken)
        {
            RequestedMaKQHT = maKQHT;
            CancellationToken = cancellationToken;

            return Task.FromResult(result);
        }
    }

    private sealed class FakeKqhtCommandService(
        CreateKqhtResult result) : IKqhtCommandService
    {
        public CreateKqhtCommand? Command { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<CreateKqhtResult> CreateAsync(
            CreateKqhtCommand command,
            CancellationToken cancellationToken)
        {
            Command = command;
            CancellationToken = cancellationToken;

            return Task.FromResult(result);
        }

        public Task<KetQua<KqhtDto>> UpdateAsync(int maKQHT, UpdateKqhtCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<KetQua<KqhtDto>> DoiTrangThaiAsync(int maKQHT, TrangThaiSuDung trangThai, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<KetQua<KhongCo>> XoaAsync(int maKQHT, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
