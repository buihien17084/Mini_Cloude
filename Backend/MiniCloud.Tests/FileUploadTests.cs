using Microsoft.Extensions.Logging.Abstractions;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Features.Files;
using Mini_Cloud.Domain.Entities;
using MiniCloud.Tests.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCloud.Tests;

public class FileUploadTests : TestBase
{
    private readonly UploadFileCommandHandler _uploadHandler;
    private readonly Guid _userId;

    public FileUploadTests()
    {
        _uploadHandler = new UploadFileCommandHandler(
            DbContext,
            StorageService,
            NullLogger<UploadFileCommandHandler>.Instance);
        _userId = Guid.NewGuid();

        DbContext.Users.Add(new User
        {
            Id = _userId,
            UserName = "uploader",
            Gmail = "uploader@example.com",
            PasswordHash = "hash"
        });
        DbContext.SaveChanges();
    }

    [Fact]
    public async Task Upload_SmallFile_ShouldSucceed_And_GenerateRandomDiskName()
    {
        var content = "Hello World Cloud Storage";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        var command = new UploadFileCommand(_userId, stream, "my_document.txt", bytes.Length);

        var result = await _uploadHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("my_document.txt", result.FileName);
        Assert.Equal(bytes.Length, result.FileSize);

        var dbRecord = await DbContext.StoredFiles.FindAsync(result.Id);
        Assert.NotNull(dbRecord);
        Assert.NotEqual("my_document.txt", dbRecord.FilePath);
        Assert.EndsWith(".txt", dbRecord.FilePath);
        Assert.True(StorageService.FileExists(dbRecord.FilePath));
    }

    [Fact]
    public async Task Upload_PathTraversalFileName_ShouldSanitizeFileName()
    {
        var content = "Malicious file attempt";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        var command = new UploadFileCommand(_userId, stream, @"..\..\..\evil.txt", bytes.Length);

        var result = await _uploadHandler.Handle(command, CancellationToken.None);

        Assert.Equal("evil.txt", result.FileName);
        var dbRecord = await DbContext.StoredFiles.FindAsync(result.Id);
        Assert.NotNull(dbRecord);
        Assert.DoesNotContain("..", dbRecord.FilePath);
        Assert.True(StorageService.FileExists(dbRecord.FilePath));
    }

    [Fact]
    public async Task Upload_ExceedingTotalQuota_ShouldThrowPayloadTooLarge_AndNotWriteDisk()
    {
        DbContext.StoredFiles.Add(new StoredFile
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileName = "large.bin",
            FilePath = "large.bin",
            FileSize = 490L * 1024 * 1024,
            CreatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        var newFileSize = 20L * 1024 * 1024;
        using var stream = new MemoryStream(new byte[1024]);

        var command = new UploadFileCommand(_userId, stream, "overflow.bin", newFileSize);

        var ex = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
            _uploadHandler.Handle(command, CancellationToken.None));
        Assert.Equal("Tổng dung lượng lưu trữ vượt quá giới hạn 500MB.", ex.Message);
    }

    [Fact]
    public async Task Upload_EmptyFile_ShouldThrowBadRequestException()
    {
        using var stream = new MemoryStream();
        var command = new UploadFileCommand(_userId, stream, "empty.txt", 0);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _uploadHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Upload_FileExceeding100MB_ShouldThrowPayloadTooLargeException()
    {
        using var stream = new MemoryStream();
        var largeSize = 101L * 1024 * 1024;
        var command = new UploadFileCommand(_userId, stream, "too_big.zip", largeSize);

        await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
            _uploadHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Upload_ConcurrentUploads_ExceedingQuota_ShouldSafelyRejectOverQuota()
    {
        DbContext.StoredFiles.Add(new StoredFile
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileName = "initial_large.bin",
            FilePath = "initial.bin",
            FileSize = 450L * 1024 * 1024,
            CreatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();
        var fileSize = 30L * 1024 * 1024;

        var task1 = Task.Run(async () =>
        {
            using var stream1 = new MemoryStream(new byte[64]);
            return await _uploadHandler.Handle(new UploadFileCommand(_userId, stream1, "concurrent_1.bin", fileSize), CancellationToken.None);
        });

        var task2 = Task.Run(async () =>
        {
            using var stream2 = new MemoryStream(new byte[64]);
            return await _uploadHandler.Handle(new UploadFileCommand(_userId, stream2, "concurrent_2.bin", fileSize), CancellationToken.None);
        });

        var results = await TaskTestHelper.WhenSettled(task1, task2);

        var succeededCount = results.Count(r => r.IsSuccess);
        var quotaErrorsCount = results.Count(r => r.Exception?.GetBaseException() is PayloadTooLargeException);

        Assert.Equal(1, succeededCount);
        Assert.Equal(1, quotaErrorsCount);
    }

    private static class TaskExtensions
    {
        
    }
}

public static class TaskTestHelper
{
    public record TaskOutcome(bool IsSuccess, Exception? Exception);

    public static async Task<TaskOutcome[]> WhenSettled<T>(Task<T> t1, Task<T> t2)
    {
        var outcomes = new List<TaskOutcome>();
        foreach (var task in new[] { (Task)t1, (Task)t2 })
        {
            try
            {
                await task;
                outcomes.Add(new TaskOutcome(true, null));
            }
            catch (Exception ex)
            {
                outcomes.Add(new TaskOutcome(false, ex));
            }
        }
        return outcomes.ToArray();
    }
}

