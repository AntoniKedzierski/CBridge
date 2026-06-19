using Microsoft.Extensions.DependencyInjection;

namespace Bridgemate.Services;

public static class ServiceHelper {
    public static T GetService<T>() where T : notnull
        => IPlatformApplication.Current!.Services.GetRequiredService<T>();
}
