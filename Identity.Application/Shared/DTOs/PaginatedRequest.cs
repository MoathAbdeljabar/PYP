

namespace MyApp.Application.Shared.DTOs;
    public class PaginatedRequest
    {
        public short PageNumber { get; set; } = 1;
        public short PageSize { get; set; } = 10;


    public void validateInput()
    {
        if (PageSize <= 0 || PageSize > 100)
            PageSize = 10;


        if (PageNumber <= 0)
            PageNumber = 1;

    }
}

