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
                SimData.InitData(); DataManager.Load();
                if (!SimData.Blocks.IsValid()) BlockData.LoadNetwork();
                if (!SimData.Trains.IsValid()) TrainData.LoadTrains();
                SimData.CheckNodes();
                TrafficManager.UpdateTraffic(0);
                TrafficLights.SetTrafficLights(SimData.Nodes);
            }
        }

    }
}
