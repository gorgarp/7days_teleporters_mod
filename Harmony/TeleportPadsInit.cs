using System.Reflection;
using HarmonyLib;

public class TeleportPadsInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        Log.Out("[TeleportPads] Loading mod...");
        var harmony = new HarmonyLib.Harmony("com.greg.teleportpads");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        var _ = TeleportPadManager.Instance;
        Log.Out("[TeleportPads] Mod loaded successfully.");
    }
}
