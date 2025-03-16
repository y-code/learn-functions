using Microsoft.Extensions.Logging;

namespace Ycode.Functions;

public interface IMessageGenerator
{
    string Generate(string name);
}

public class MessageGenerator(
    ILogger<MessageGenerator> logger
) : IMessageGenerator
{
    public string Generate(string name)
        => $"Welcome to Azure Functions, {name}!";
}