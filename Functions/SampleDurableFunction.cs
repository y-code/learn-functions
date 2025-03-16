using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ycode.Functions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpClientForSampleDurableFunction(this IServiceCollection services) {
        services.AddHttpClient<SampleDurableFunction>()
            .AddYcodeStandard1ResilienceHandler();
        return services;
    }
}

public class SampleDurableFunction(IConfiguration config, HttpClient httpClient, ILogger<SampleDurableFunction> logger)
{
    public record RunOrchestratorRequest(
        string Name,
        Guid TemplateId
    );

    public record ComposeEmailDataRequest(
        AppUser User,
        string Xsl
    );

    [Function(nameof(SampleDurableFunction))]
    public async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext orchestration,
        FunctionContext execution,
        CancellationToken cancel)
    {
        logger.LogInformation("Saying hello.");

        var req = orchestration.GetInput<RunOrchestratorRequest>();
        if (req == null)
        {
            throw new ArgumentException("Failed to get RunOrchestratorRequest from the input.");
        }

        var getUser = orchestration.CallActivityAsync<AppUser>(nameof(GetUser), req.Name);
        var getTemplate = orchestration.CallActivityAsync<string>(nameof(GetXmlDocTemplate), req.TemplateId);
        await Task.WhenAll([
            Task.Run(() => getUser, cancel),
            Task.Run(() => getTemplate, cancel),
        ]);

        var user = await getUser;
        var template = await getTemplate;

        var polygonApiKey = config.GetValue<string>("PolygonApiKey");
        var dReq = new DurableHttpRequest(HttpMethod.Post,
            new($"https://api.polygon.io/v3/reference/tickers/AAPL?apiKey={polygonApiKey}"));
        var res = await orchestration.CallHttpAsync(dReq);

        var xml = await orchestration.CallActivityAsync<string>(nameof(ComposeEmailData),
            new ComposeEmailDataRequest(user, template));

        return xml;
    }

    [Function(nameof(SayHello))]
    public string SayHello(
        [ActivityTrigger] string? name,
        FunctionContext executionContext)
    {
        logger.LogInformation("Saying hello to {name}.", name);
        return $"Hello {name}!";
    }

    [Function(nameof(GetUser))]
    public AppUser GetUser(
        [ActivityTrigger] string name,
        FunctionContext executionContext)
    {
        // get the user information from data store
        return new AppUser { Name = name };
    }

    [Function(nameof(ComposeEmailData))]
    public async Task<string> ComposeEmailData(
        [ActivityTrigger] ComposeEmailDataRequest req,
        FunctionContext executionContext)
    {
        var xslStream = new MemoryStream(Encoding.UTF8.GetBytes(req.Xsl), writable: false);
        var xslt = new XslCompiledTransform();
        using (var inReader = XmlReader.Create(xslStream, new XmlReaderSettings { CloseInput = true }))
        {
            xslt.Load(inReader);
        }

        var dataDoc = $$"""
            <?xml version="1.0" encoding="UTF-8"?>
            <user>
                <name>{{req.User.Name}}</name>
            </user>
            """;
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(dataDoc), writable: false);
        var outStream = new MemoryStream();
        using (var dataReader = XmlReader.Create(dataStream, new XmlReaderSettings { CloseInput = true }))
        using (var writer = XmlWriter.Create(outStream, new XmlWriterSettings { CloseOutput = false }))
        {
            xslt.Transform(dataReader, writer);
        }

        outStream.Position = 0;
        // var reader = XmlReader.Create(outStream, new XmlReaderSettings { CloseInput = true });
        // var xml = await reader.ReadElementContentAsStringAsync();
        // return xml;
        using var reader = new StreamReader(outStream);
        var xml = await reader.ReadToEndAsync();
        return xml;
    }

    [Function(nameof(GetXmlDocTemplate))]
    public string GetXmlDocTemplate(
        [ActivityTrigger] string id,
        FunctionContext context)
    {
        // get the registered XML doc template
        return """
            <?xml version="1.0" encoding="UTF-8"?>
            <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
                <xsl:output method="xml" indent="yes" encoding="UTF-8" />
                <xsl:template match="/">
                    <email>
                        <subject>Sample</subject>
                        <body><![CDATA[
            Hi ${userName},
            
            Test
            TEST
            tesT]]>
                        </body>
                        <variables>
                            <variable>
                                <key>userName</key>
                                <value><xsl:value-of select="user/name" /></value>
                            </variable>
                        </variables>
                    </email>
                </xsl:template>
            </xsl:stylesheet>
            """;
    }

    [Function($"{nameof(SampleDurableFunction)}_HttpStart")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext,
        CancellationToken cancel)
    {
        var name = req.Query["name"];
        if (string.IsNullOrEmpty(name))
        {
            var res = HttpResponseData.CreateResponse(req);
            res.StatusCode = System.Net.HttpStatusCode.BadRequest;
            res.Body = new MemoryStream(Encoding.UTF8.GetBytes("The query parameter 'name' is required."));
            return res;
        }
        var templateId = Guid.NewGuid();

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(SampleDurableFunction), new RunOrchestratorRequest(name, templateId), cancel);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
