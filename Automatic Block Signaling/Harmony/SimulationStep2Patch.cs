//using HarmonyLib;
//using JetBrains.Annotations;
//using System.Reflection;
//using UnityEngine;

//namespace dmaTrainABS.Patching
//{
//    [UsedImplicitly]
//    [HarmonyPatch]
//    public class SimulationStep2Patch
//    {
//        private delegate void TargetDelegate(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics);
//        [UsedImplicitly]
//        public static MethodBase TargetMethod() => TranspilerUtil.DeclaredMethod<TargetDelegate>(typeof(TrainAI), "SimulationStep");

//        [UsedImplicitly]
//        public static void Prefix(TrainAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
//        {
//            try
//            {
//                __instance.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
//                DOP.VehicleId = vehicleID;
//            }
//            catch { }
//        }

//    }
//}

