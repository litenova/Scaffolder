using Microsoft.Build.Locator;

namespace Scaffolder.Specifications.Utilities;

/// <summary>
/// Handles MSBuild initialization for Roslyn analysis.
/// Ensures MSBuild is registered only once during application lifetime.
/// </summary>
public static class MSBuildInitializer
{
    private static bool _isInitialized;
    private static readonly Lock Lock = new();

    /// <summary>
    /// Registers MSBuild if not already registered.
    /// Thread-safe and idempotent - subsequent calls will return immediately.
    /// </summary>
    public static void Initialize()
    {
        // Quick check without lock
        if (_isInitialized)
        {
            return;
        }

        lock (Lock)
        {
            // Double-check after acquiring lock
            if (_isInitialized)
            {
                return;
            }

            try
            {
                MSBuildLocator.RegisterDefaults();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize MSBuild. Ensure .NET SDK is properly installed.", ex);
            }
        }
    }
}