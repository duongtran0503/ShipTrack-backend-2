namespace Logistics.Api.Domain.DTOs.Requests;

public class PaginationAndFilterRequest
{
    public int? PageNumber { get; set; }    
    public int? PageSize { get; set; }    
    public string? SearchTerm { get; set; }

}