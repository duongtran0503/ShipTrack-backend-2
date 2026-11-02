namespace Logistics.Api.Domain.Interfaces
{
    public interface IErrorCode
    {


        int StatusCode { get; }
    string Message { get; }
    bool Success { get; }
}
}
