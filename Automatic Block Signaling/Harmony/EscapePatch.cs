using HarmonyLib;

namespace dmaTrainABS.Patching
{
    [HarmonyPatch(typeof(GameKeyShortcuts), "Escape")]
    public static class Escape
    {
        public static bool Prefix()
        {
            if (NodeSelector.IsActiveTool)
            {
                NodeSelector.DisableTool();
                return false;
            }
            return true;
        }

    }
}