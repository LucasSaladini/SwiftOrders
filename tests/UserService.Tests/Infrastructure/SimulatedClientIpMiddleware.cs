using System.Net;
using Microsoft.AspNetCore.Http;

namespace UserService.Tests.Infrastructure;

public sealed class SimulatedClientIpMiddleware
{
  private readonly RequestDelegate _next;

  public SimulatedClientIpMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var ipHeader = context.Request.Headers["X-Test-Client-IP"].FirstOrDefault();

    if (IPAddress.TryParse(ipHeader, out var ipAddress))
    {
      context.Connection.RemoteIpAddress = ipAddress;
    }

    await _next(context);
  }
}