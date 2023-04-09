using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.UI;
using dmaTrainABS.Patching;
using ICities;
using System;
using System.Reflection;
using UnityEngine;

namespace dmaTrainABS
{
    public class TrainABSModData : IUserMod
    {
        public const string DMAMODS_NAME = "DMA TrainABS";
        public static string DMAMODS_VERSION;

        public static bool _settingsFailed = false;

        public static readonly SavedInputKey ModShortcut = new SavedInputKey("modShortcut", "dmaTrainABS_Config", SavedInputKey.Encode(KeyCode.N, true, true, false), true);
        public static readonly SavedInputKey NetReload = new SavedInputKey("netReload", "dmaTrainABS_Config", SavedInputKey.Encode(KeyCode.U, true, true, false), true);
        public static readonly SavedInputKey AllGreenLights = new SavedInputKey("allGreenLights", "dmaTrainABS_Config", SavedInputKey.Encode(KeyCode.G, true, true, false), true);
        public static readonly SavedInputKey AllRedLights = new SavedInputKey("allRedLights", "dmaTrainABS_Config", SavedInputKey.Encode(KeyCode.R, true, true, false), true);

        public string Name { get { return DMAMODS_NAME; } }

        public string Description
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                DMAMODS_VERSION = "ver." + string.Format("{0}.{1:00}.{2:000}  rev.{3}", version.Major, version.Minor, version.Build, version.Revision);
                return "Train Automatic Block Signaling, " + DMAMODS_VERSION;
            }
        }

        public TrainABSModData()
        {
            try
            {
                if (GameSettings.FindSettingsFileByName("dmaTrainABS_Config") == null)
                {
                    SettingsFile sFiles = new SettingsFile();
                    sFiles.fileName = "dmaTrainABS_Config";
                    SettingsFile[] settingsFiles = new SettingsFile[] { sFiles };
                    GameSettings.AddSettingsFile(settingsFiles);
                }
            }
            catch { _settingsFailed = true; }
        }

        public void OnEnabled()
        {
            try
            {
                HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());

                var version = Assembly.GetExecutingAssembly().GetName().Version;
                DMAMODS_VERSION = "ver." + string.Format("{0}.{1:00}.{2:000}  rev.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
            catch { }
        }

        public static void Load()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            DMAMODS_VERSION = "ver." + string.Format("{0}.{1:00}.{2:000}  rev.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                UIHelper hp = (UIHelper)helper;
                UIScrollablePanel panel = (UIScrollablePanel)hp.self;
                panel.eventVisibilityChanged += VisibilityChanged;

                var version = Assembly.GetExecutingAssembly().GetName().Version;
                string title = "Train Automatic Block Signaling, ver." + string.Format("{0}.{1:00}.{2:000}  rev.{3}", version.Major, version.Minor, version.Build, version.Revision);

                UIHelper group = helper.AddGroup(title) as UIHelper;
                group.AddSpace(10);

                (group.self as UIPanel).gameObject.AddComponent<OptionsKeymapping>();

                group.AddSpace(10);

            }
            catch { }
        }

        private void VisibilityChanged(UIComponent component, bool value) { if (value) { component.eventVisibilityChanged -= VisibilityChanged; } }

    }
}
