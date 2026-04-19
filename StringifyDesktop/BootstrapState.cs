using StringifyDesktop.Services;

namespace StringifyDesktop;

internal static class BootstrapState
{
    private static SingleInstanceService? singleInstance;

    public static void Initialize(SingleInstanceService instance)
    {
        singleInstance = instance;
    }

    public static SingleInstanceService RequireSingleInstance()
    {
        return singleInstance ?? throw new InvalidOperationException("Single instance bootstrap was not initialized.");
    }
}
