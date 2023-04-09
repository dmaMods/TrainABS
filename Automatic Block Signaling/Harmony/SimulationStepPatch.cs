//using HarmonyLib;
//using JetBrains.Annotations;
//using System.Reflection;
//using UnityEngine;

//namespace dmaTrainABS.Patching
//{
//    [UsedImplicitly]
//    [HarmonyPatch]
//    public class SimulationStepPatch
//    {
//        private delegate void TargetDelegate(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos);
//        [UsedImplicitly]
//        public static MethodBase TargetMethod() => TranspilerUtil.DeclaredMethod<TargetDelegate>(typeof(TrainAI), "SimulationStep");

//        [UsedImplicitly]
//        public static void Prefix(TrainAI __instance, ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
//        {
//            try
//            {
//                __instance.SimulationStep(vehicleID, ref data, physicsLodRefPos);
//                DOP.VehicleId = vehicleID;
//            }
//            catch { }
//        }

//    }
//}

