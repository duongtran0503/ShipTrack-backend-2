using Logistics.Api.Domain.Interfaces;

namespace Logistics.Api.Domain.Exceptions
{
    public class AppException : Exception
    {
        public IErrorCode ErrorCode { get; }
        public AppException(IErrorCode err) : base(err.Message)
        {
            ErrorCode = err;
        }
        public AppException(IErrorCode err, string message) : base(message: message)
        {
            ErrorCode = err;
        }
    }

   
}
