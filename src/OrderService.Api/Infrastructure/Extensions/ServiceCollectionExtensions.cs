using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using OrderService.Api.Infrastructure.Contracts.Dto;
using OrderService.Api.Infrastructure.ErrorHandling;

namespace OrderService.Api.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring SmcCloud API conventions in the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SmcCloud API conventions to the service collection.
    /// </summary>
    public static IServiceCollection AddOrderServiceControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                // CamelCase serialization
                opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                // Convert enums to strings using camelCase
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                var errorMsg = "Invalid input model:\n " + string.Join("\n ", errors);

                return new BadRequestObjectResult(ErrorResponse.BadRequest(errorMsg));
            });

        services.AddExceptionHandler<ApiExceptionHandler>();

        return services;
    }
}
