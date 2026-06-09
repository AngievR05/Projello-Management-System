using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

[Authorize]
public sealed class TeamNotificationHub : Hub
{
    public override Task OnConnectedAsync()
    {
        var uid = Context.UserIdentifier ?? "null";
        Console.WriteLine($"[TeamHub] Connected: ConnectionId={Context.ConnectionId} UserIdentifier={uid}");
        foreach (var c in Context.User?.Claims ?? Enumerable.Empty<Claim>())
            Console.WriteLine($"[TeamHub] Claim: {c.Type} = {c.Value}");
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine(
            $"[TeamHub] Disconnected: ConnectionId={Context.ConnectionId} UserIdentifier={Context.UserIdentifier} Ex={ex?.Message}"
        );

        await base.OnDisconnectedAsync(ex);
    }
}