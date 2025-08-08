using QR_Menu.PayTRService.Models;
using System.Net;

namespace QR_Menu.PayTRService.Services
{
    public class PayTRAPIService : IPayTRAPIService
    {
        private readonly PayTRAPIClient _apiClient;

        public PayTRAPIService(PayTRAPIClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<(TResponse, HttpStatusCode)> CreateLink<TRequest, TResponse>(TRequest request)
        {
            return await _apiClient.SendRequest<TRequest, TResponse>
                (
                HttpMethod.Post,
                PayTREndpoint.CreateLink,
                request,
                null
                );
        }

        public async Task<(TResponse, HttpStatusCode)> DeleteLink<TRequest, TResponse>(TRequest request)
        {
            return await _apiClient.SendRequest<TRequest, TResponse>
               (
               HttpMethod.Post,
               PayTREndpoint.DeleteLink,
               request,
               null
               );
        }

        public async Task<(TResponse, HttpStatusCode)> Pay<TRequest, TResponse>(TRequest request)
        {
            return await _apiClient.SendRequest<TRequest, TResponse>
                (
                HttpMethod.Post,
                PayTREndpoint.Pay,
                request,
                null
                );
        }
    }
}