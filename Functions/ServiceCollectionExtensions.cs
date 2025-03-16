using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Ycode.Functions;

public static partial class ServiceCollectionExtensions
{
    private static IHttpStandardResiliencePipelineBuilder AddYcodeStandard1ResilienceHandler(this IHttpClientBuilder builder)
        => builder.AddStandardResilienceHandler(options => {
            options.Retry.DelayGenerator = ctx => new ValueTask<TimeSpan?>(ctx switch {
                { Outcome.Result: null } => null,
                { Outcome.Result.StatusCode: System.Net.HttpStatusCode.TooManyRequests } => TimeSpan.FromSeconds(10d),
                { Outcome.Result.StatusCode: System.Net.HttpStatusCode.ServiceUnavailable } => TimeSpan.FromSeconds(30d),
                _ => null,
            });
        });
}
