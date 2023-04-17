using ColossalFramework;
using ColossalFramework.Math;
using dmaTrainABS.Traffic;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static dmaTrainABS.GameData.Declarations;

namespace dmaTrainABS
{
    public static class NetExtensions
    {
        private static readonly NetNode[] nodes = Singleton<NetManager>.instance.m_nodes.m_buffer;
        private static readonly NetSegment[] segments = Singleton<NetManager>.instance.m_segments.m_buffer;
        private static readonly Vehicle[] vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

        internal static ref NetNode ToNode(this ushort nodeId) => ref nodes[nodeId];
        internal static bool IsJunction(this ref NetNode netNode) => netNode.m_flags.IsFlagSet(NetNode.Flags.Junction);
        internal static bool HaveLights(this ref NetNode netNode) => netNode.m_flags.IsFlagSet(NetNode.Flags.TrafficLights);
        internal static bool IsMiddle(this ref NetNode netNode) => netNode.m_flags.IsFlagSet(NetNode.Flags.Middle);

        public static ref Vehicle ToVehicle(this uint vehicleId) => ref vehicles[vehicleId];

        public static bool IsValid(this NetNode node)
        {
            if (node.Info == null) return false;
            return node.m_flags.IsFlagSet(NetNode.Flags.Created) && !node.m_flags.IsFlagSet(NetNode.Flags.Deleted) &&
                  (node.Info.m_connectGroup == NetInfo.ConnectGroup.SingleTrain ||
                   node.Info.m_connectGroup == NetInfo.ConnectGroup.DoubleTrain ||
                   node.Info.m_connectGroup == NetInfo.ConnectGroup.TrainStation);
        }

        public static bool IsValidJunction(this NetNode node)
        {
            NetNode.Flags flags = node.m_flags;
            if (flags.IsFlagSet(NetNode.Flags.Junction))
            {
                if (flags.IsFlagSet(NetNode.Flags.LevelCrossing)) return false;
                if (flags.IsFlagSet(NetNode.Flags.TrafficLights)) return true;
                if (node.CountSegments() <= 2) return flags.IsFlagSet(NetNode.Flags.Untouchable);
                return true;
            }
            return false;
        }

        public static ushort FrontCar(this ushort trainId)
        {
            var vehicle = vehicles[trainId];
            bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
            bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
            return isMain && !reversed ? trainId : vehicle.GetLastVehicle(trainId);
        }

        public static ushort LastCar(this ushort trainId)
        {
            var vehicle = vehicles[trainId];
            bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
            bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
            return isMain && !reversed ? vehicle.GetLastVehicle(trainId) : trainId;
        }

        public static bool IsValid(this List<SNodeData> nodeData)
        {
            if (nodeData == null) return false;
            if (nodeData.Count == 0) return false;
            return true;
        }

        public static bool IsValid(this List<STrains> sTrains)
        {
            if (sTrains == null) return false;
            if (sTrains.Count == 0) return false;
            return true;
        }

        public static bool OutOfArea(this List<ushort> segments)
        {
            if (segments.Count() == 0) return false;
            var segment = segments.FirstOrDefault().ToSegment();
            return segment.m_flags.IsFlagSet(NetSegment.Flags.Original);
        }

        public static List<SWaitingList> AddNew(this List<SWaitingList> list, SWaitingList item)
        {
            if (!list.Any(x => x.NodeId == item.NodeId && x.TrainId == item.TrainId)) list.Add(item);
            return list;
        }

        public static List<ushort> AddNew(this List<ushort> list, ushort item)
        {
            if (!list.Any(x => x == item)) list.Add(item);
            return list;
        }

        public static List<PathUnit.Position> AddNew(this List<PathUnit.Position> list, PathUnit.Position item)
        {
            if (!list.Any(x => x.m_segment == item.m_segment)) list.Add(item);
            return list;
        }

        public static List<BlockDataVars.Block> AddNew(this List<BlockDataVars.Block> list, BlockDataVars.Block item)
        {
            if (!list.Any(x => x.BlockId == item.BlockId)) list.Add(item);
            return list;
        }

        public static bool IsValid(this Dictionary<ushort, SRailBlocks> sBlocks)
        {
            if (sBlocks == null) return false;
            if (sBlocks.Count == 0) return false;
            return true;
        }

        internal static bool IsUnderground(this ref NetNode netNode) =>
            netNode.m_flags.IsFlagSet(NetNode.Flags.Underground);

        internal static bool CheckFlags(this NetNode.Flags value, NetNode.Flags required, NetNode.Flags forbidden = 0) =>
            (value & (required | forbidden)) == required;

        internal static bool CheckFlags(this NetSegment.Flags value, NetSegment.Flags required, NetSegment.Flags forbidden = 0) =>
            (value & (required | forbidden)) == required;

        internal static bool CheckFlags(this NetLane.Flags value, NetLane.Flags required, NetLane.Flags forbidden = 0) =>
            (value & (required | forbidden)) == required;

        internal static bool CheckFlags(this NetInfo.Direction value, NetInfo.Direction required, NetInfo.Direction forbidden = 0) =>
            (value & (required | forbidden)) == required;

        internal static NetSegment GetSegment(this ushort SegmentId) => segments[SegmentId];

        public static ref NetSegment ToSegment(this ushort segmentId) => ref segments[segmentId];

        public static Vehicle ToVehicle(this ushort vehicleId) => vehicles[vehicleId];

        public static NetInfo.Node[] GetNodes(this ushort segmentId) => segments[segmentId].Info.m_nodes;

        public static uint RevertLane(this uint lane) => lane == 0 ? 1u : 0;

        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        public static Bezier3 Flat(this Bezier3 bezier) => new Bezier3() { a = bezier.a.Flat(), b = bezier.b.Flat(), c = bezier.c.Flat(), d = bezier.d.Flat() };

    }

}
