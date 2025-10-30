using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Common
{
    public class PaginationRequest
    {
        private int _page = 1;
        private int _pageSize = 10;
        private const int MaxPageSize = 50;

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 50")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : (value > MaxPageSize ? MaxPageSize : value);
        }

        public int Skip => (Page - 1) * PageSize;
    }

    public class PaginationResponse
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        public PaginationResponse(int currentPage, int pageSize, int totalItems)
        {
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalItems = totalItems;
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            HasNextPage = currentPage < TotalPages;
            HasPreviousPage = currentPage > 1;
        }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public PaginationResponse Pagination { get; set; }

        public PaginatedResult(List<T> items, int currentPage, int pageSize, int totalItems)
        {
            Items = items;
            Pagination = new PaginationResponse(currentPage, pageSize, totalItems);
        }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
