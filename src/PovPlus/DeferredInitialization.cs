using System;

namespace PovPlus;

// Keep the framework subscription alive while login/native objects are pending.
internal sealed class DeferredInitialization
{
    internal bool Ready { get; private set; }
    internal bool HadFailure { get; private set; }
    private long retryAt;

    internal bool Ensure(long now, Func<bool> available, Action initialize, Action cleanup, Action<Exception> report)
    {
        if (Ready) return true;
        if (now < retryAt) return false;
        try
        {
            if (!available()) return false;
            initialize();
            Ready = true;
            HadFailure = false;
            return true;
        }
        catch (Exception ex)
        {
            try { cleanup(); }
            catch (Exception cleanupError) { ex = new AggregateException(ex, cleanupError); }
            retryAt = now + 1000;
            if (!HadFailure) report(ex);
            HadFailure = true;
            return false;
        }
    }

    internal void Reset(Action cleanup)
    {
        Ready = HadFailure = false;
        retryAt = 0;
        cleanup();
    }
}
