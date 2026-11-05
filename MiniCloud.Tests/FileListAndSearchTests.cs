using Microsoft.EntityFrameworkCore;
using Mini_Cloud.Application.Features.Files;
using Mini_Cloud.Domain.Entities;
using MiniCloud.Tests.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCloud.Tests;

public class FileListAndSearchTests : TestBase
{
    private readonly GetFilesQueryHandler _handler;
    private readonly Guid _userA;
    private readonly Guid _userB;
   public FileListAndSearchTests()
    {
        _handler = new GetFilesQueryHandler(DbContext);
        _userA= Guid.NewGuid();
        _userB= Guid.NewGuid();
        DbContext.StoredFiles.AddRange(
            new StoredFile { Id = Guid.NewGuid(), UserId = _userA, FileName = "Report_2026.pdf", FilePath = "f1.pdf", FileSize = 100, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new StoredFile { Id = Guid.NewGuid(), UserId = _userA, FileName = "Invoice_October.docx", FilePath = "f2.docx", FileSize = 200, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new StoredFile { Id = Guid.NewGuid(), UserId = _userA, FileName = "Photo_Vacation.png", FilePath = "f3.png", FileSize = 300, CreatedAt = DateTime.UtcNow }
        );

        // Seed file for User B
        DbContext.StoredFiles.Add(
            new StoredFile { Id = Guid.NewGuid(), UserId = _userB, FileName = "Confidential_B.pdf", FilePath = "fb.pdf", FileSize = 500, CreatedAt = DateTime.UtcNow }
        );

        DbContext.SaveChanges();
    }
    [Fact]
    public async Task GetFiles_ShouldOnlyReturnCurrentUsersFiles()
    {
        var result = await _handler.Handle(new GetFilesQuery(_userA), CancellationToken.None);

        Assert.Equal(3, result.TotalFiles);
        Assert.All(result.Items, f => Assert.DoesNotContain("Confidential_B", f.FileName));
    }

    [Fact]
    public async Task GetFiles_SearchCaseInsensitive_ShouldFilterCorrectly()
    {
        var result = await _handler.Handle(new GetFilesQuery(_userA, SearchTerm: "report"), CancellationToken.None);

        Assert.Equal(1, result.TotalFiles);
        Assert.Equal("Report_2026.pdf", result.Items[0].FileName);
    }

    [Theory]
    [InlineData(0, 10, 1, 10)] // Page 0 -> normalized to 1
    [InlineData(-5, 0, 1, 10)] // Page -5 -> 1, PageSize 0 -> default 10
    [InlineData(1, 10000, 1, 100)] // PageSize 10000 -> clamped to 100
    public async Task GetFiles_BoundaryPagination_ShouldNotThrow_And_NormalizeSafely(
        int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _handler.Handle(new GetFilesQuery(_userA, null, page, pageSize), CancellationToken.None);

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
    }

}



