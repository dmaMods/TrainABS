using ColossalFramework;
using dmaTrainABS.Patching;
using ICities;

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
                SimData.Updating = true;
                
                SimData.InitData(); DataManager.Load();
                
                SimData.CheckNodes();
                
                BlockData.LoadNetwork();
                TrainData.LoadTrains();

                var currentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                SimData.Updating = false; TrafficManager.UpdateTraffic(currentFrame);

                TrafficLights.SetTrafficLights(SimData.Nodes);
            }
        }

    }
}
