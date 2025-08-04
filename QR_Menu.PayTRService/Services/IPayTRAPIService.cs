using System.Net;

namespace QR_Menu.PayTRService.Services
{
    
    public interface IPayTRAPIService
    {
       
        Task<(TResponse? Response, bool Success, string? ErrorMessage)> PayAsync<TRequest, TResponse>(
            TRequest request, CancellationToken cancellationToken = default);
        
     
        Task<(TResponse? Response, bool Success, string? ErrorMessage)> CreateLinkAsync<TRequest, TResponse>(
            TRequest request, CancellationToken cancellationToken = default);
        
    
        Task<(TResponse? Response, bool Success, string? ErrorMessage)> DeleteLinkAsync<TRequest, TResponse>(
            TRequest request, CancellationToken cancellationToken = default);
   
        Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
    }
} 