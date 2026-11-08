using PovPlus;

internal static partial class Program
{
    private static void TestRuntimeLifecycle()
    {
        var gate = new DeferredInitialization();
        var loggedIn = false;
        var modelReady = false;
        var failBodyHook = true;
        var positionHooks = 0;
        var bodyHooks = 0;
        var attempts = 0;
        var reports = 0;
        var cleanupCount = 0;
        void Cleanup() { positionHooks = bodyHooks = 0; cleanupCount++; }
        void Initialize()
        {
            attempts++;
            Check(!gate.Ready, "Initialization must not publish ready before all core hooks exist");
            positionHooks++;
            if (failBodyHook) throw new InvalidOperationException("Delayed body hook");
            bodyHooks++;
        }
        bool Tick(long time) => gate.Ensure(time, () => loggedIn && modelReady, Initialize, Cleanup, _ => reports++);
        Check(!Tick(100) && attempts == 0 && !gate.HadFailure, "Title screen waits without constructing runtime hooks");
        loggedIn = true;
        Check(!Tick(200) && attempts == 0, "Login waits for model allocation without giving up");
        modelReady = true;
        Check(!Tick(300) && !gate.Ready && gate.HadFailure && cleanupCount == 1 && positionHooks == 0,
            "Partial camera initialization is cleaned up when body hook fails");
        Check(!Tick(1299) && attempts == 1 && reports == 1, "Retry backoff does not rebuild hooks each frame");
        Check(!Tick(1300) && attempts == 2 && reports == 1 && cleanupCount == 2,
            "Persistent failure retries without log spam or surviving partial hooks");
        failBodyHook = false;
        Check(Tick(2300) && gate.Ready && !gate.HadFailure && positionHooks == 1 && bodyHooks == 1,
            "Delayed startup recovers automatically without toggling or reloading plugin");
        for (var t = 2301; t < 2450; t++) Check(Tick(t), "Ready runtime remains ready across ordinary frames");
        Check(attempts == 3 && positionHooks == 1 && bodyHooks == 1, "No duplicate hook allocation after success");
        gate.Reset(Cleanup);
        Check(!gate.Ready && !gate.HadFailure && positionHooks == 0 && bodyHooks == 0, "Camera replacement cleans previous ownership");
        Check(Tick(2450) && attempts == 4, "New world camera reinitializes without inherited retry delay");
        gate.Reset(Cleanup);
        loggedIn = false;
        Check(!Tick(2460) && positionHooks == 0, "Logout leaves hooks released until a player is available");
        gate.Reset(Cleanup);
        Check(!gate.Ready && positionHooks == 0, "Unloading before login tolerates absent runtime state");

        var brokenReadiness = new DeferredInitialization();
        Exception? reported = null;
        Check(!brokenReadiness.Ensure(0, () => throw new InvalidOperationException("Resolve"),
            () => throw new Exception("Must not initialize"), () => throw new ArgumentException("Cleanup"), ex => reported = ex),
            "Readiness resolution failure remains recoverable even if cleanup reports an error");
        Check(reported is AggregateException ae && ae.InnerExceptions.Count == 2, "Report preserves original and cleanup failure");
        Check(brokenReadiness.Ensure(1000, () => true, () => { }, () => { }, _ => { }), "Later resolution succeeds without a reload");
    }

    private static void TestNearObjectScope()
    {
        const nint camera = 10, eye = 20;
        Check(NearObjectDistanceScope.Filter(0.01f, camera, eye) == 0.01f, "Normal distance calls remain unchanged");
        using (new NearObjectDistanceScope(true, camera, eye))
        {
            Check(NearObjectDistanceScope.Filter(0.01f, camera, eye) == float.MaxValue,
                "Eligible proximity comparison sees object beyond its hiding radius");
            Check(NearObjectDistanceScope.Filter(0, camera, eye) == float.MaxValue, "Exactly coincident object also bypasses proximity hiding");
            Check(NearObjectDistanceScope.Filter(1, 11, eye) == 1 && NearObjectDistanceScope.Filter(1, camera, 21) == 1,
                "Other camera and unrelated distance reference point remain unchanged");
            Check(float.IsNaN(NearObjectDistanceScope.Filter(float.NaN, camera, eye)) &&
                float.IsPositiveInfinity(NearObjectDistanceScope.Filter(float.PositiveInfinity, camera, eye)) &&
                NearObjectDistanceScope.Filter(-1, camera, eye) == -1, "Invalid or sentinel distances are never reclassified");
            using (new NearObjectDistanceScope(false, camera, eye))
                Check(NearObjectDistanceScope.Filter(1, camera, eye) == 1, "Disabled nested render call cannot inherit enabled scope");
            Check(NearObjectDistanceScope.Filter(1, camera, eye) == float.MaxValue, "Outer render classification resumes after nested call");
            using (new NearObjectDistanceScope(true, 11, 21))
                Check(NearObjectDistanceScope.Filter(1, camera, eye) == 1 && NearObjectDistanceScope.Filter(1, 11, 21) == float.MaxValue,
                    "Nested owner gets only its own classification");
            var otherThreadDistance = -1f;
            var worker = new System.Threading.Thread(() => otherThreadDistance = NearObjectDistanceScope.Filter(1, camera, eye));
            worker.Start(); worker.Join();
            Check(otherThreadDistance == 1, "Other render threads do not inherit scoped distance override");
        }
        Check(NearObjectDistanceScope.Filter(1, camera, eye) == 1, "Distance classification ends immediately with draw predicate");
        try
        {
            using var scope = new NearObjectDistanceScope(true, camera, eye);
            throw new InvalidOperationException("Native wrapper failure");
        }
        catch (InvalidOperationException) { }
        Check(NearObjectDistanceScope.Filter(1, camera, eye) == 1, "Exception cannot leak override into subsequent native calls");
        using (new NearObjectDistanceScope(true, 0, eye))
            Check(NearObjectDistanceScope.Filter(1, 0, eye) == 1, "Null owner never enables override");
        using (new NearObjectDistanceScope(true, camera, 0))
            Check(NearObjectDistanceScope.Filter(1, camera, 0) == 1, "Null position never enables override");
    }
}
