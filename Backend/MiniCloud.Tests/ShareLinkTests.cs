using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Features.Files;
using Mini_Cloud.Domain.Entities;
using MiniCloud.Tests.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCloud.Tests;

public class ShareLinkTests : TestBase
{
    private readonly CreateShareLinkCommandHandler _createShareHandler;
    private readonly RevokeShareLinkCommandHandler _revokeShareHandler;
    private readonly DownloadSharedFileQueryHandler _downloadSharedHandler;

    private readonly Guid _userId;
    private readonly StoredFile _file;

    public ShareLinkTests()
    {
        _createShareHandler = new CreateShareLinkCommandHandler(DbContext);
        _revokeShareHandler = new RevokeShareLinkCommandHandler(DbContext);
        _downloadSharedHandler = new DownloadSharedFileQueryHandler(DbContext, StorageService);

        _userId = Guid.NewGuid();

        var content = "Shared Public File Content";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var physicalPath = StorageService.SaveFileAsync(stream, "shared_doc.txt").GetAwaiter().GetResult();

        _file = new StoredFile
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileName = "shared_doc.txt",
            FilePath = physicalPath,
            FileSize = bytes.Length,
            CreatedAt = DateTime.UtcNow,
            IsShared = false
        };

        DbContext.StoredFiles.Add(_file);
        DbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateShareLink_ShouldGenerate256BitTokenAndReturnFullUrl()
    {
        var response = await _createShareHandler.Handle(
            new CreateShareLinkCommand(_userId, _file.Id, ExpiresInHours: 24, BaseUrl: "https://clouddrive.example.com"),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.ShareToken));
        Assert.Equal(64, response.ShareToken.Length);
        Assert.Equal($"https://clouddrive.example.com/api/files/shared/{response.ShareToken}", response.ShareUrl);
        Assert.NotNull(response.ExpiresAt);

        var dbFile = await DbContext.StoredFiles.FindAsync(_file.Id);
        Assert.NotNull(dbFile);
        Assert.True(dbFile.IsShared);
        Assert.Equal(response.ShareToken, dbFile.ShareToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9000)] 
    public async Task CreateShareLink_InvalidExpiryHours_ShouldThrowBadRequestException(int invalidHours)
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _createShareHandler.Handle(new CreateShareLinkCommand(_userId, _file.Id, invalidHours), CancellationToken.None));
    }

    [Fact]
    public async Task DownloadSharedFile_ValidToken_ShouldSucceedAnonymously()
    {
        var shareResponse = await _createShareHandler.Handle(new CreateShareLinkCommand(_userId, _file.Id), CancellationToken.None);

        var downloadResult = await _downloadSharedHandler.Handle(new DownloadSharedFileQuery(shareResponse.ShareToken), CancellationToken.None);

        Assert.NotNull(downloadResult);
        Assert.Equal("shared_doc.txt", downloadResult.FileName);

        using var reader = new StreamReader(downloadResult.Stream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal("Shared Public File Content", content);
    }

    [Fact]
    public async Task DownloadSharedFile_ExpiredToken_ShouldThrowNotFoundException()
    {
        _file.IsShared = true;
        _file.ShareToken = "expiredtokenteststringwhichhascertainlengthtoqualify64hexcharacters12";
        _file.ShareExpiresAt = DateTime.UtcNow.AddMinutes(-5); // Expired 5 mins ago
        await DbContext.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _downloadSharedHandler.Handle(new DownloadSharedFileQuery(_file.ShareToken), CancellationToken.None));
        Assert.Equal("Link chia sẻ không tồn tại hoặc đã hết hạn.", ex.Message);
    }

    [Fact]
    public async Task RevokeShareLink_ThenDownload_ShouldThrowNotFoundException()
    {
        var shareResponse = await _createShareHandler.Handle(new CreateShareLinkCommand(_userId, _file.Id), CancellationToken.None);
        var token = shareResponse.ShareToken;
        var revokeResult = await _revokeShareHandler.Handle(new RevokeShareLinkCommand(_userId, _file.Id), CancellationToken.None);
        Assert.True(revokeResult);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _downloadSharedHandler.Handle(new DownloadSharedFileQuery(token), CancellationToken.None));
        var newShareResponse = await _createShareHandler.Handle(new CreateShareLinkCommand(_userId, _file.Id), CancellationToken.None);
        Assert.NotEqual(token, newShareResponse.ShareToken);
    }
}







