using QR_Menu.PayTRService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace QR_Menu.PayTRService.Services
{
    public class PayTRAPIService : IPayTRAPIService
    {
        private readonly PayTRAPIClient _apiClient;
        private readonly PayTRConfiguration _configuration;
        private readonly ILogger<PayTRAPIService> _logger;

        public PayTRAPIService(
            PayTRAPIClient apiClient, 
            IOptions<PayTRConfiguration> configuration,
            ILogger<PayTRAPIService> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(TResponse? Response, bool Success, string? ErrorMessage)> PayAsync<TRequest, TResponse>(
            TRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                return (default(TResponse), false, "Request cannot be null");

            _logger.LogInformation("Processing PayTR payment request");

            var url = $"{_configuration.BaseUrl.TrimEnd('/')}/{_configuration.PayEndpoint}";
            return await ExecuteWithRetryAsync<TRequest, TResponse>(
                HttpMethod.Post, url, request, cancellationToken);
        }

        public async Task<(TResponse? Response, bool Success, string? ErrorMessage)> CreateLinkAsync<TRequest, TResponse>(
            TRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                return (default(TResponse), false, "Request cannot be null");

            _logger.LogInformation("Processing PayTR create link request");

            var url = $"{_configuration.BaseUrl.TrimEnd('/')}/{_configuration.CreateLinkEndpoint}";
            return await ExecuteWithRetryAsync<TRequest, TResponse>(
                HttpMethod.Post, url, request, cancellationToken);
        }

        public async Task<(TResponse? Response, bool Success, string? ErrorMessage)> DeleteLinkAsync<TRequest, TResponse>(
            TRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                return (default(TResponse), false, "Request cannot be null");

            _logger.LogInformation("Processing PayTR delete link request");

            var url = $"{_configuration.BaseUrl.TrimEnd('/')}/{_configuration.DeleteLinkEndpoint}";
            return await ExecuteWithRetryAsync<TRequest, TResponse>(
                HttpMethod.Post, url, request, cancellationToken);
        }

        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Performing PayTR health check");
                
                // Simple health check - you can customize this based on PayTR's health endpoint
                var url = _configuration.BaseUrl;
                var (_, statusCode, errorMessage) = await _apiClient.SendRequestAsync<object, string>(
                    HttpMethod.Get, url, new object(), cancellationToken);

                var isHealthy = statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NotFound; // PayTR might return 404 for root
                
                if (isHealthy)
                    _logger.LogDebug("PayTR health check successful");
                else
                    _logger.LogWarning("PayTR health check failed: {StatusCode} - {Error}", statusCode, errorMessage);

                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayTR health check failed with exception");
                return false;
            }
        }

        private async Task<(TResponse? Response, bool Success, string? ErrorMessage)> ExecuteWithRetryAsync<TRequest, TResponse>(
            HttpMethod method, string url, TRequest request, CancellationToken cancellationToken)
        {
            var lastError = string.Empty;
            
            for (int attempt = 1; attempt <= _configuration.MaxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogDebug("PayTR API attempt {Attempt}/{MaxAttempts}", attempt, _configuration.MaxRetryAttempts);

                    var (response, statusCode, errorMessage) = await _apiClient.SendRequestAsync<TRequest, TResponse>(
                        method, url, request, cancellationToken);

                    // Check if the request was successful
                    if (IsSuccessStatusCode(statusCode) && string.IsNullOrEmpty(errorMessage))
                    {
                        _logger.LogInformation("PayTR API request successful on attempt {Attempt}", attempt);
                        return (response, true, null);
                    }

                    lastError = errorMessage ?? $"Request failed with status code: {statusCode}";
                    
                    // Don't retry for client errors (4xx)
                    if ((int)statusCode >= 400 && (int)statusCode < 500)
                    {
                        _logger.LogWarning("PayTR API client error, not retrying: {StatusCode} - {Error}", 
                            statusCode, lastError);
                        break;
                    }

                    _logger.LogWarning("PayTR API attempt {Attempt} failed: {Error}", attempt, lastError);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("PayTR API request cancelled on attempt {Attempt}", attempt);
                    return (default(TResponse), false, "Request was cancelled");
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogError(ex, "PayTR API attempt {Attempt} failed with exception", attempt);
                }

                // Wait before retrying (except on last attempt)
                if (attempt < _configuration.MaxRetryAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(_configuration.RetryDelay.TotalMilliseconds * attempt);
                    _logger.LogDebug("Waiting {Delay}ms before retry", delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            _logger.LogError("PayTR API request failed after {MaxAttempts} attempts. Last error: {Error}", 
                _configuration.MaxRetryAttempts, lastError);
            
            return (default(TResponse), false, $"Request failed after {_configuration.MaxRetryAttempts} attempts: {lastError}");
        }

        private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
        {
            return (int)statusCode >= 200 && (int)statusCode < 300;
        }
    }
} 