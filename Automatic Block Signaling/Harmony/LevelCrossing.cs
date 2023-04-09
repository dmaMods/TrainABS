//using HarmonyLib;

//namespace dmaTrainABS.Patching
//{
//    [HarmonyPatch(typeof(TrainTrackBaseAI), nameof(TrainTrackBaseAI.LevelCrossingSimulationStep))]
//    public class LevelCrossingSimulationStepPatch
//    {
//        public static bool Prefix(ushort nodeID)
//        {
//            try
//            {
//                DOP.rqNode = nodeID;
//                return true;
//            }
//            catch { }
//            return false;
//        }

//    }
//}
