using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ycode.Functions;

public record RunResponse
(
    [property: HttpResult]
    IActionResult HttpResponse,
    [property: ServiceBusOutput("myqueue", ServiceBusEntityType.Queue, Connection = "ServiceBusConnection")]
    string? Message = null
);

public class HttpToServiceBus(
    IMessageGenerator messageGenerator,
    ILogger<HttpToServiceBus> logger)
{
    [Function("HttpToServiceBus")]
    // [ServiceBusOutput("myqueue", ServiceBusEntityType.Queue, Connection = "ServiceBusConnection")]
    public RunResponse Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        var name = req.Query["name"].FirstOrDefault();
        if (string.IsNullOrEmpty(name))
        {
            return new RunResponse(
                HttpResponse: new BadRequestObjectResult("The 'name' query parameter is required.")
            );
        }

        logger.LogInformation("C# HTTP trigger function processed a request.");
        var message = messageGenerator.Generate(name);
        return new RunResponse(
            HttpResponse: new JsonResult(new { message }),
            Message: message);
    }
}
