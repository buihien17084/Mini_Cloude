using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Application.Common.Models;

    public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalFiles { get; }
    public int TotalPages { get; }
    public int Page { get; }
    public int PageSize { get; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int totalFiles, int page, int pageSize)
    {
        Items = items;
        TotalFiles = totalFiles;
        Page = page;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalFiles / pageSize) : 0;
    }
}



