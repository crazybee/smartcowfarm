using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SmartCowFarm.Functions.Functions;

public class SignalRFunctions
{
    [Function("negotiate")]
    public static SignalRConnectionInfo Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "negotiate")] HttpRequest req,
        [SignalRConnectionInfoInput(HubName = "cowfarm")] SignalRConnectionInfo connectionInfo)
    {
        return connectionInfo;
    }

    [Function("broadcastUpdate")]
    [SignalROutput(HubName = "cowfarm")]
    public static async Task<SignalRMessageAction> BroadcastUpdate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "broadcastUpdate")] HttpRequest req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();

        return new SignalRMessageAction("cowUpdate")
        {
            Arguments = [body]
        };
    }
}
