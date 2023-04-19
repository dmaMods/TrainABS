using ColossalFramework;
using ColossalFramework.Plugins;
using dmaTrainABS.Patching;
using static dmaTrainABS.GameData.Declarations;
using ICities;
using System;

namespace dmaTrainABS
{
    public class TrainABSLoader : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.LoadMap || mode == LoadMode.NewMap)
            {
                Patcher.ForcePatch();

                SimData.Updating = true; SimData.InitData();
                DataManager.Load();
                if (IsDebugMode)
                {
                    string txt = "[TrainABS] LevelLoaded. Nodes: " + SimData.Nodes.Count;
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, txt);
                }
                SimData.CheckNodes();

                BlockData.LoadNetwork();
                TrainData.LoadTrains();

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }

                var currentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                SimData.Updating = false; TrafficManager.UpdateTraffic(currentFrame);
                TrafficLights.SetTrafficLights(SimData.Nodes);
            }
        }

    }
}
