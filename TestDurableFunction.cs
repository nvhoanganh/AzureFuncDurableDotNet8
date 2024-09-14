using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace AnthonyDemo
{
    [DurableTask(nameof(SayHelloClass))]
    public class SayHelloClass : TaskActivity<string, string>
    {
        private readonly ILogger<SayHelloClass> logger;

        public SayHelloClass(ILogger<SayHelloClass> logger)
        {
            this.logger = logger;
        }

        public override async Task<string> RunAsync(TaskActivityContext context, string input)
        {
            logger.LogInformation($"Saying {input} back");
            await Task.Delay(200);
            return $"Hello {input} via Class!";
        }
    }


    public static class TestDurableFunction
    {
        [Function(nameof(TestDurableFunction))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(TestDurableFunction));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            // Replace name and input with values relevant for your Durable Functions Activity
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // this shown as executed in the console but the activity function is not executed
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHelloClass), "There"));
            // this shown as executed in the console but the activity function is not executed
            outputs.Add(await context.CallSayHelloClassAsync("Blah"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }

        [Function("TestDurableFunction_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("TestDurableFunction_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(TestDurableFunction));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
