using System;
using System.Collections.Generic;
using System.Linq;

namespace IMS.Api.Application.Common.Models;

/// <summary>
/// Represents a paginated result set
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize, int totalPages)
    {
        Items = (items ?? Enumerable.Empty<T>()).ToList().AsReadOnly();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = totalPages;
    }

    // Convenience ctor that calculates totalPages
    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        : this(items, totalCount, pageNumber, pageSize, CalculateTotalPages(totalCount, pageSize))
    {
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        if (pageSize <= 0) return 0;
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }

    public static PagedResult<T> Empty(int pageNumber, int pageSize) =>
        new(Enumerable.Empty<T>(), 0, pageNumber, pageSize);
}