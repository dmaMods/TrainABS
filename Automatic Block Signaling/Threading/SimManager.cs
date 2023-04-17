using ColossalFramework;
using ICities;

namespace dmaTrainABS
{
    public class SimManager : ThreadingExtensionBase
    {
        public override void OnBeforeSimulationTick()
        {
            base.OnBeforeSimulationTick();
        }

        public override void OnBeforeSimulationFrame()
        {
            base.OnBeforeSimulationFrame();
        }

        public override void OnAfterSimulationTick()
        {
            base.OnAfterSimulationTick();

        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
            KeyboardInput.CheckInput();
        }

        public override void OnAfterSimulationFrame()
        {
            base.OnAfterSimulationFrame();
            if (!SimData.Nodes.IsValid()) return;
            var currentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
            if (currentFrame % 15 == 0) TrafficManager.UpdateTraffic(currentFrame);
            TrafficLights.SetTrafficLights(SimData.Nodes);
        }

    }
}
