using System.Net;

namespace QR_Menu.PayTRService.Services
{
    public interface IPayTRAPIService
    {
        Task<(TResponse, HttpStatusCode)> Pay<TRequest, TResponse>(TRequest request);
        Task<(TResponse, HttpStatusCode)> CreateLink<TRequest, TResponse>(TRequest request);
        Task<(TResponse, HttpStatusCode)> DeleteLink<TRequest, TResponse>(TRequest request);
    }
}
