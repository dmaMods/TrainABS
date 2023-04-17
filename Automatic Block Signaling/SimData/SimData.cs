using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static dmaTrainABS.GameData.Declarations;

namespace dmaTrainABS
{
    public class SimData
    {
        private static List<SNodeData> nodes = new List<SNodeData>();

        public static List<STrains> Trains { get; set; } = new List<STrains>();
        public static List<SNodeData> Nodes { get => nodes; set { nodes = value; UpdateRequired = true; } }
        public static Dictionary<ushort, SRailBlocks> Blocks { get; set; } = new Dictionary<ushort, SRailBlocks>();
        public static bool UpdateRequired { get; set; } = false;
        public static List<ushort> GreenLights { get; set; } = new List<ushort>();
        public static List<SWaitingList> WaitingList { get; set; } = new List<SWaitingList>();
        public static bool Updating { get; set; } = false;

        public static void InitData(bool InitAll = true)
        {
            if (InitAll)
                Nodes = new List<SNodeData>();
            Blocks = new Dictionary<ushort, SRailBlocks>();
            Trains = new List<STrains>();
            UpdateRequired = false;
            WaitingList = new List<SWaitingList>();
            GreenLights = new List<ushort>();
        }

        public static void UpdateBlock(ushort blockId, ushort trainId)
        {
            if (Blocks.ContainsKey(blockId))
                Blocks[blockId].BlockedBy = trainId;
        }

        public static void AddNode(ushort nodeID, List<ushort> segments)
        {
            try
            {
                if (nodeID == 0) return;
                if (!Nodes.IsValid()) Nodes = new List<SNodeData>();
                List<SSegments> nodeSegments = new List<SSegments>();
                foreach (var seg in segments)
                    nodeSegments.Add(new SSegments { SegmentID = seg });

                if (!Nodes.Any(x => x.NodeID == nodeID))
                {
                    Nodes.Add(new SNodeData { NodeID = nodeID, Segments = nodeSegments });
                }
                CheckNodes();
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        public static void RemoveNode(ushort nodeID)
        {
            try
            {
                if (nodeID == 0) return;
                if (!Nodes.IsValid()) return;

                Nodes.RemoveAll(x => x.NodeID == nodeID);

                CheckNodes();
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        public static void CheckNodes()
        {
            try
            {
                foreach (var node in Nodes)
                {
                    NetNode netNode = node.NodeID.ToNode();
                    NetNode.Flags flags = netNode.m_flags;
                    if (flags.CheckFlags(NetNode.Flags.Created | NetNode.Flags.Junction | NetNode.Flags.TrafficLights, NetNode.Flags.LevelCrossing | NetNode.Flags.Untouchable))
                    { /* valid node... do nothing */ }
                    else
                    {
                        WaitingList.RemoveAll(x => x.NodeId == node.NodeID);
                        node.NodeID = 0;
                    }
                }
                Nodes.RemoveAll(x => x.NodeID == 0);
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

    }
}
