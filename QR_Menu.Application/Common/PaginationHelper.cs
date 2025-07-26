using System.Linq.Expressions;

namespace QR_Menu.Application.Common;

public static class PaginationHelper
{
    public static async Task<object> CreatePaginatedResponseAsync<T>(
        Func<int, int, Task<(List<T> Data, int TotalCount)>> dataProvider,
        int? pageNumber,
        int? pageSize,
        string successMessageTR,
        string successMessageEN,
        string notFoundMessageTR = "Veri bulunamadı",
        string notFoundMessageEN = "Data not found")
    {
        // Validate pagination parameters
        var (validPageNumber, validPageSize, shouldPaginate) = ValidatePaginationParameters(pageNumber, pageSize);

        // Get data
        var (data, totalCount) = await dataProvider(validPageNumber, validPageSize);

        // Handle empty data - return empty array instead of 404
        if (data == null || !data.Any())
        {
            if (!shouldPaginate)
            {
                // When both parameters are null - return ResponsBase with empty array
                return ResponsBase.Create(successMessageTR, successMessageEN, "200", new List<T>());
            }
            else
            {
                // When pagination parameters are provided - return paginated structure with empty data
                var paginatedData = new
                {
                    totalCount = 0,
                    pageSize = validPageSize,
                    currentPage = validPageNumber,
                    totalPages = 0,
                    hasNextPage = false,
                    hasPreviousPage = false,
                    data = new List<T>()
                };
                return paginatedData;
            }
        }

        // Return response based on pagination
        if (!shouldPaginate)
        {
            // When both parameters are null - return ResponsBase with wrapper messages
            return ResponsBase.Create(successMessageTR, successMessageEN, "200", data);
        }
        else
        {
            // When pagination parameters are provided - return ONLY the data object
            var totalPages = (int)Math.Ceiling((double)totalCount / validPageSize);
            var hasNextPage = validPageNumber < totalPages;
            var hasPreviousPage = validPageNumber > 1;

            var paginatedData = new
            {
                totalCount = totalCount,
                pageSize = validPageSize,
                currentPage = validPageNumber,
                totalPages = totalPages,
                hasNextPage = hasNextPage,
                hasPreviousPage = hasPreviousPage,
                data = data
            };

            // Return ONLY the data object, no ResponsBase wrapper
            return paginatedData;
        }
    }

    private static (int pageNumber, int pageSize, bool shouldPaginate) ValidatePaginationParameters(int? pageNumber, int? pageSize)
    {
        // If both parameters are null, return ResponsBase with wrapper messages
        var shouldPaginate = pageNumber.HasValue || pageSize.HasValue;

        if (!shouldPaginate)
        {
            return (1, int.MaxValue, false);
        }

        // Set default values: pageNumber = 1, pageSize = 10
        var validPageNumber = pageNumber ?? 1;
        var validPageSize = pageSize ?? 10;

        // Validate and set limits
        validPageNumber = Math.Max(1, validPageNumber);
        validPageSize = Math.Min(100, Math.Max(1, validPageSize)); // Cap at 100 items per page

        return (validPageNumber, validPageSize, true);
    }
}

// DTO for pagination parameters
public class PaginationParameters
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    
    public bool ShouldPaginate => PageNumber.HasValue || PageSize.HasValue;
} 