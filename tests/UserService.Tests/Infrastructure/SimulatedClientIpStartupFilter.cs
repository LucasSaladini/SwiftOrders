using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace UserService.Tests.Infrastructure;

public sealed class SimulatedClientIpStartupFilter : IStartupFilter
{
  public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
  {
    return app =>
    {
      app.UseMiddleware<SimulatedClientIpMiddleware>();
      next(app);
    };
  }
}