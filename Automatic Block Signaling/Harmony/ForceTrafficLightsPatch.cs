using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using JetBrains.Annotations;

namespace dmaTrainABS.Patching
{

    [UsedImplicitly]
    [HarmonyPatch]
    public class ForceTrafficLightsPatch
    {
        private delegate void TargetDelegate(ushort vehicleID, ref Vehicle vehicleData, bool reserveSpace);
        [UsedImplicitly]
        public static MethodBase TargetMethod() => TranspilerUtil.DeclaredMethod<TargetDelegate>(typeof(TrainAI), "ForceTrafficLights");

        [UsedImplicitly]
        public static void Prefix(TrainAI __instance,
                                  ushort vehicleID,
                                  ref Vehicle vehicleData,
                                  bool reserveSpace)
        {

            uint path = vehicleData.m_path;
            if (path != 0)
            {
                NetManager instance = Singleton<NetManager>.instance;
                PathManager manager2 = Singleton<PathManager>.instance;
                byte pathPositionIndex = vehicleData.m_pathPositionIndex;
                if (pathPositionIndex == 0xff)
                {
                    pathPositionIndex = 0;
                }
                pathPositionIndex = (byte)(pathPositionIndex >> 1);
                for (int i = 0; i < 6; i++)
                {
                    PathUnit.Position position;
                    if (!manager2.m_pathUnits.m_buffer[path].GetPosition(pathPositionIndex, out position))
                    {
                        return;
                    }
                    if (reserveSpace && ((i >= 1) && (i <= 2)))
                    {
                        uint laneID = PathManager.GetLaneID(position);
                        if (laneID != 0)
                        {
                            reserveSpace = instance.m_lanes.m_buffer[laneID].ReserveSpace(__instance.m_info.m_generatedInfo.m_size.z, vehicleID);
                        }
                    }
                    ForceTrafficLights(position);
                    byte num1 = (byte)(pathPositionIndex + 1);
                    if ((pathPositionIndex = num1) >= manager2.m_pathUnits.m_buffer[path].m_positionCount)
                    {
                        path = manager2.m_pathUnits.m_buffer[path].m_nextPathUnit;
                        pathPositionIndex = 0;
                        if (path == 0)
                        {
                            return;
                        }
                    }
                }
            }
        }

        private static void ForceTrafficLights(PathUnit.Position position)
        {
            return;
        }

    }
}
