using HarmonyLib;
using JetBrains.Annotations;
using static RoadBaseAI;

namespace dmaTrainABS.Patching
{
    [HarmonyPatch(typeof(RoadBaseAI), "SetTrafficLightState")]
    [UsedImplicitly]
    public class SetTrafficLightStatePatch {
        /// <summary>
        /// Prevents vanilla code from updating traffic light visuals for custom traffic lights.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(ushort nodeID,
                                  ref NetSegment segmentData,
                                  uint frame,
                                  TrafficLightState vehicleLightState,
                                  TrafficLightState pedestrianLightState,
                                  bool vehicles,
                                  bool pedestrians) {
            return true;
            //return !SavedGameOptions.Instance.timedLightsEnabled
            //       || !TrafficLightSimulationManager
            //                    .Instance
            //                    .TrafficLightSimulations[nodeID]
            //                    .IsSimulationRunning();
        }
    }
}