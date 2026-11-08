using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using Hypostasis.Dalamud;
using PovPlus.Windows;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;

namespace PovPlus;

public class Plugin : IDalamudPlugin, IDisposable
{
	private const string CommandName = "/pov";

	internal static Plugin P;

	public readonly WindowSystem WindowSystem = new WindowSystem("PovPlus");

	private TickManager tickManager = new TickManager();
    private readonly DeferredInitialization initialization = new();
    private FirstPersonHeadVisibility? headVisibility;
    private NearSelfVfx? selfVfx;
    private NearObjectVisibility? nearObjects;
    private bool nearObjectsUnavailable;
    internal string NearObjectsStatus => nearObjectsUnavailable ? L.T("近处物体保留不可用，请查看 Dalamud 日志。") : "";
    private FirstPersonMouseLook? mouseLook;
    private bool mouseLookUnavailable, pluginTextInput;
    internal bool MouseLookUiPaused => MainWindow.IsOpen || pluginTextInput;
    internal bool MouseLookActive => mouseLook?.Active == true;
    internal string MouseLookStatus => mouseLookUnavailable || mouseLook?.Failed == true
        ? L.T("鼠标直控/固定姿势保护不可用，请查看 Dalamud 日志。") : MainWindow.IsOpen ? L.T("设置窗口打开时，自动释放鼠标。") : "";
    private bool vfxUnavailable;
    private bool initialized => initialization.Ready;
    internal string RuntimeStatus => initialized ? "" : initialization.HadFailure
        ? L.T("初始化暂未完成，正在自动重试；详情见 Dalamud 日志。") : L.T("等待角色与相机加载完成。");
    private bool disposed;
    private CommandInfo? povCommand;
    internal string HeadHidingStatus => headVisibility == null || headVisibility.Failed ? L.T("头部隐藏不可用，请查看 Dalamud 日志。") : "";
    internal string HeadShadowStatus => headVisibility?.ShadowStatus ?? "";
    internal string VfxStatus => vfxUnavailable || selfVfx?.Failed == true ? L.T("自身特效减弱不可用，请查看 Dalamud 日志。") : "";

	public bool tfactive = false;

	public Configuration Configuration { get; init; }

	private MainWindow MainWindow { get; init; }

	public Hook<CameraBase.Delegates.ShouldDrawGameObject>? ExecuteEmoteHook2 { get; private set; }

	public unsafe Plugin(IDalamudPluginInterface pluginInterface)
	{
		P = this;
		pluginInterface.Create<Service>(Array.Empty<object>());
		pluginInterface.Create<DalamudApi>(Array.Empty<object>());
		Configuration = (Service.PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();
		MainWindow = new MainWindow(this);
		WindowSystem.AddWindow(MainWindow);
		povCommand = new CommandInfo(OnCommand)
		{
			HelpMessage = L.T("打开 POV+ 设置；/pov toggle 切换启用状态")
		};
        Service.CommandManager.AddHandler("/pov", povCommand);
        Service.PluginInterface.LanguageChanged += OnLanguageChanged;
		Service.PluginInterface.UiBuilder.Draw += DrawUi;
		Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
		Service.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        // Do not resolve world-camera hooks at the title screen. This callback
        // remains registered even if the first initialization attempt fails.
        Service.Framework.Update += Tick;
	}

	public unsafe bool ShouldDrawGameObjectDetour(CameraBase* thisPtr, GameObject* gameObject, Vector3* sceneCameraPos, Vector3* lookAtVector)
	{
        var localPlayer = Service.ObjectTable.LocalPlayer;
        if (initialized && POVCameraFunctionality.IsEnabled && localPlayer != null && gameObject == (GameObject*)localPlayer.Address)
            return true;
        return nearObjects != null
            ? nearObjects.Draw(ExecuteEmoteHook2!.Original, thisPtr, gameObject, sceneCameraPos, lookAtVector)
            : ExecuteEmoteHook2!.Original(thisPtr, gameObject, sceneCameraPos, lookAtVector);
	}

	private void Tick(object framework)
	{
        if (disposed) return;
        if (initialized && !POVCameraFunctionality.RuntimeCameraMatches()) initialization.Reset(CleanupRuntime);
        if (!initialization.Ensure(Environment.TickCount64, RuntimeAvailable, Initialize, CleanupRuntime,
            ex => Service.Log.Error(ex, "POV+ runtime initialization failed; retrying after login readiness."))) return;
        headVisibility?.Restore();
		tickManager.TickUpdate();
        POVCameraFunctionality.RestoreNearPlaneIfInactive();
        POVCameraFunctionality.ResetLookDownLeanIfInactive();
        headVisibility?.UpdateState();
        if (Configuration.FirstPersonReduceSelfVfx && selfVfx == null && !vfxUnavailable)
        {
            try { selfVfx = new NearSelfVfx(); }
            catch (Exception ex) { vfxUnavailable = true; Service.Log.Error(ex, "POV+ could not initialize self VFX attenuation."); }
        }
        selfVfx?.UpdateState();
        if (Configuration.FirstPersonKeepNearObjects && nearObjects == null && !nearObjectsUnavailable)
        {
            try { nearObjects = new NearObjectVisibility(); }
            catch (Exception ex) { nearObjectsUnavailable = true; Service.Log.Error(ex, "POV+ could not initialize near-object visibility."); }
        }
        // Shared native input hooks also protect ordinary RMB while sitting;
        // do not tie their initialization to the optional direct-mouse toggle.
        if (Configuration.Setting_ModEnabled && mouseLook == null && !mouseLookUnavailable)
        {
            try { mouseLook = new FirstPersonMouseLook(); }
            catch (Exception ex) { mouseLookUnavailable = true; Service.Log.Error(ex, "POV+ could not initialize mouse-look."); }
        }
        mouseLook?.Update();
        if (!Configuration.Setting_ModEnabled || !Configuration.FirstPersonLimitLook) FirstPersonLimits.Reset();
	}

	public void Dispose()
	{
        disposed = true;
        Service.PluginInterface.LanguageChanged -= OnLanguageChanged;
        Service.Framework.Update -= Tick;
        initialization.Reset(CleanupRuntime);
		Service.PluginInterface.UiBuilder.Draw -= DrawUi;
		Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
		Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
		WindowSystem.RemoveAllWindows();
		MainWindow.Dispose();
		Service.CommandManager.RemoveHandler("/pov");
	}

    private void CleanupRuntime()
    {
        nearObjects?.Dispose();
        nearObjects = null;
        nearObjectsUnavailable = false;
        mouseLook?.Dispose();
        mouseLook = null;
        headVisibility?.Dispose();
        headVisibility = null;
        FirstPersonLimits.Dispose();
        selfVfx?.Dispose();
        selfVfx = null;
        POVCameraFunctionality.Dispose();
		ExecuteEmoteHook2?.Disable();
		ExecuteEmoteHook2?.Dispose();
        ExecuteEmoteHook2 = null;
        mouseLookUnavailable = vfxUnavailable = false;
        tickManager = new TickManager();
    }

	private void OnCommand(string command, string args)
	{
		string text = args.ToLowerInvariant();
		string text2 = text;
		if (text2 == null || text2.Length != 0)
		{
			if (text2 == "toggle")
			{
				P.Configuration.Setting_ModEnabled = !P.Configuration.Setting_ModEnabled;
                Configuration.Save();
			}
		}
		else
		{
			MainWindow.Toggle();
		}
	}

	public void ToggleConfigUi()
	{
		MainWindow.Toggle();
	}

    private void DrawUi()
    {
        pluginTextInput = Dalamud.Bindings.ImGui.ImGui.GetIO().WantTextInput;
        WindowSystem.Draw();
    }

    private void OnLanguageChanged(string language) => RefreshLanguage();
    internal void RefreshLanguage()
    {
        if (povCommand != null) povCommand.HelpMessage = L.T("打开 POV+ 设置；/pov toggle 切换启用状态");
    }

	public void ToggleMainUi()
	{
		MainWindow.Toggle();
	}

    private unsafe bool RuntimeAvailable()
    {
        var player = Service.ObjectTable.LocalPlayer;
        if (player == null) return false;
        var model = player.GetCharacterBase();
        // Do not wait for an evaluated pose: displaying the body may be needed
        // before its animation/skeleton finishes updating in first person.
        if (model == null) return false;
        var manager = Hypostasis.Game.Common.CameraManager;
        if (manager == null || manager->worldCamera == null || manager->worldCamera->vtbl == null ||
            manager->worldCamera->vtbl[16] == 0) return false;
        return ((CameraBase*)manager->worldCamera)->SceneCamera.RenderCamera != null;
    }

    private unsafe void Initialize()
    {
        POVCameraFunctionality.Initialize();
        FirstPersonLimits.Initialize();
        ExecuteEmoteHook2 = Service.GameInteropProvider.HookFromAddress<CameraBase.Delegates.ShouldDrawGameObject>(
            CameraBase.MemberFunctionPointers.ShouldDrawGameObject, ShouldDrawGameObjectDetour);
        try { headVisibility = new FirstPersonHeadVisibility(); }
        catch (Exception ex) { Service.Log.Error(ex, "POV+ could not initialize head hiding."); }
        Service.Log.Information("POV+ runtime initialized after player/model and world camera became ready.");
    }

    internal unsafe hkQsTransformf CameraHeadTransform(hkaPose* pose, int index)
    {
        if (headVisibility != null && headVisibility.TryGetOriginal(pose, index, out var original)) return original;
        return *pose->AccessBoneModelSpace(index, hkaPose.PropagateOrNot.DontPropagate);
    }
}
