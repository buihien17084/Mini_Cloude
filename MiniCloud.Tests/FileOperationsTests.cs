using Microsoft.Extensions.Logging.Abstractions;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Features.Files;
using Mini_Cloud.Domain.Entities;
using MiniCloud.Tests.Common;

using System.Text;

namespace MiniCloud.Tests;
public class FileOperationsTests : TestBase
{
    private readonly DownloadFileQueryHandler _downloadHandler;
    private readonly RenameFileCommandHandler _renameHandler;
    private readonly DeleteFileCommandHandler _deleteHandler;

    private readonly Guid _userOwner;
    private readonly Guid _userStranger;
    private readonly StoredFile _ownedFile;

    public FileOperationsTests()
    {
        _downloadHandler = new DownloadFileQueryHandler(DbContext, StorageService);
        _renameHandler = new RenameFileCommandHandler(DbContext);
        _deleteHandler = new DeleteFileCommandHandler(DbContext, StorageService, NullLogger<DeleteFileCommandHandler>.Instance);

        _userOwner = Guid.NewGuid();
        _userStranger = Guid.NewGuid();

        // Save a physical file
        var content = "My confidential content";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var physicalPath = StorageService.SaveFileAsync(stream, "my_notes.txt").GetAwaiter().GetResult();

        _ownedFile = new StoredFile
        {
            Id = Guid.NewGuid(),
            UserId = _userOwner,
            FileName = "my_notes.txt",
            FilePath = physicalPath,
            FileSize = bytes.Length,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.StoredFiles.Add(_ownedFile);
        DbContext.SaveChanges();
    }

    [Fact]
    public async Task Download_OwnFile_ShouldReturnStreamAndOriginalName()
    {
        var result = await _downloadHandler.Handle(new DownloadFileQuery(_userOwner, _ownedFile.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("my_notes.txt", result.FileName);
        Assert.Equal("text/plain", result.ContentType);

        using var reader = new StreamReader(result.Stream);
        var text = await reader.ReadToEndAsync();
        Assert.Equal("My confidential content", text);
    }

    [Fact]
    public async Task Download_OthersFile_ShouldThrowNotFoundException_NotForbidden()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _downloadHandler.Handle(new DownloadFileQuery(_userStranger, _ownedFile.Id), CancellationToken.None));

        Assert.Equal("Không tìm thấy file hoặc bạn không có quyền truy cập.", ex.Message);
    }

    [Fact]
    public async Task Rename_ValidNewName_ShouldUpdateDisplayNameOnly()
    {
        var result = await _renameHandler.Handle(new RenameFileCommand(_userOwner, _ownedFile.Id, "updated_notes.txt"), CancellationToken.None);

        Assert.Equal("updated_notes.txt", result.FileName);

        var dbFile = await DbContext.StoredFiles.FindAsync(_ownedFile.Id);
        Assert.NotNull(dbFile);
        Assert.Equal("updated_notes.txt", dbFile.FileName);
        Assert.Equal(_ownedFile.FilePath, dbFile.FilePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("folder/file.txt")]
    [InlineData(@"folder\file.txt")]
    public async Task Rename_InvalidName_ShouldThrowBadRequestException(string invalidName)
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _renameHandler.Handle(new RenameFileCommand(_userOwner, _ownedFile.Id, invalidName), CancellationToken.None));
    }

    [Fact]
    public async Task Rename_OthersFile_ShouldThrowNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _renameHandler.Handle(new RenameFileCommand(_userStranger, _ownedFile.Id, "hacked.txt"), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_OwnFile_ShouldRemoveDatabaseRecordAndPhysicalFile()
    {
        var diskName = _ownedFile.FilePath;
        Assert.True(StorageService.FileExists(diskName));

        var result = await _deleteHandler.Handle(new DeleteFileCommand(_userOwner, _ownedFile.Id), CancellationToken.None);

        Assert.Equal(_ownedFile.Id, result.Id);
        var dbFile = await DbContext.StoredFiles.FindAsync(_ownedFile.Id);
        Assert.Null(dbFile);
        Assert.False(StorageService.FileExists(diskName));
    }

    [Fact]
    public async Task Delete_OthersFile_ShouldThrowNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _deleteHandler.Handle(new DeleteFileCommand(_userStranger, _ownedFile.Id), CancellationToken.None));
    }
}







