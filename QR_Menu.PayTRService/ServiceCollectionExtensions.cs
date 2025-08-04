using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QR_Menu.PayTRService.Services;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace QR_Menu.PayTRService
{
    public static class ServiceCollectionExtensions
    {
       
        public static IServiceCollection AddPayTRServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Configure PayTR settings
            services.Configure<PayTRConfiguration>(configuration.GetSection("PayTR"));

            // Validate configuration
            var payTRConfig = configuration.GetSection("PayTR").Get<PayTRConfiguration>();
            if (payTRConfig == null || !payTRConfig.IsValid())
            {
                var errors = payTRConfig?.GetValidationErrors() ?? new List<string> { "PayTR configuration section is missing" };
                throw new InvalidOperationException($"PayTR configuration is invalid: {string.Join(", ", errors)}");
            }

            // Register HttpClient with factory for PayTRAPIClient
            services.AddHttpClient<PayTRAPIClient>(client =>
            {
                client.BaseAddress = new Uri(payTRConfig.BaseUrl);
                client.Timeout = payTRConfig.RequestTimeout;
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "QR-Menu-PayTR-Client/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Register PayTR services
            services.AddScoped<IPayTRSecurityService, PayTRSecurityService>();
            services.AddScoped<IPayTRAPIService, PayTRAPIService>();

            return services;
        }

        /// <summary>
        /// Adds PayTR services with custom configuration
        /// </summary>
        public static IServiceCollection AddPayTRServices(
            this IServiceCollection services,
            Action<PayTRConfiguration> configureOptions)
        {
            services.Configure(configureOptions);

            var tempConfig = new PayTRConfiguration();
            configureOptions(tempConfig);

            // Validate configuration
            if (!tempConfig.IsValid())
            {
                var errors = tempConfig.GetValidationErrors();
                throw new InvalidOperationException($"PayTR configuration is invalid: {string.Join(", ", errors)}");
            }

            // Register HttpClient with factory for PayTRAPIClient
            services.AddHttpClient<PayTRAPIClient>(client =>
            {
                client.BaseAddress = new Uri(tempConfig.BaseUrl);
                client.Timeout = tempConfig.RequestTimeout;
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "QR-Menu-PayTR-Client/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Register PayTR services
            services.AddScoped<IPayTRSecurityService, PayTRSecurityService>();
            services.AddScoped<IPayTRAPIService, PayTRAPIService>();

            return services;
        }

     
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // HttpRequestException and 5XX, 408 status codes
                .OrResult(msg => !msg.IsSuccessStatusCode && (int)msg.StatusCode >= 500)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var logger = context.GetLogger();
                        if (outcome.Exception != null)
                        {
                            logger?.LogWarning("PayTR HTTP retry {RetryCount} after {Delay}ms due to: {Exception}",
                                retryCount, timespan.TotalMilliseconds, outcome.Exception.Message);
                        }
                        else
                        {
                            logger?.LogWarning("PayTR HTTP retry {RetryCount} after {Delay}ms due to status: {StatusCode}",
                                retryCount, timespan.TotalMilliseconds, outcome.Result?.StatusCode);
                        }
                    });
        }

        /// <summary>
        /// Creates a circuit breaker policy to handle cascading failures
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, duration) =>
                    {
                        // Log circuit breaker opening
                    },
                    onReset: () =>
                    {
                        // Log circuit breaker closing
                    });
        }
    }

    /// <summary>
    /// Extension methods for logging context
    /// </summary>
    internal static class ContextExtensions
    {
        private const string LoggerKey = "ILogger";

        public static Context WithLogger(this Context context, ILogger logger)
        {
            context[LoggerKey] = logger;
            return context;
        }

        public static ILogger? GetLogger(this Context context)
        {
            return context.TryGetValue(LoggerKey, out var logger) ? logger as ILogger : null;
        }
    }
} 