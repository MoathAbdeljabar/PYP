namespace MyApp.Application.Cart.DTOs.Responses;
    public class CartItemsResponse
    {
    public List<CartProductResponse> CartProducts = new List<CartProductResponse>();   
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int TotalPages { get; set; }
    public int RemovedItemsCount { get; set; } = 0;

}

