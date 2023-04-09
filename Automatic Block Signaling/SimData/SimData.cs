using System;
using System.Collections.Generic;
using System.Linq;
using static dmaTrainABS.GameData.Declarations;

namespace dmaTrainABS
{
    public class SimData
    {
        private static List<SNodeData> nodes = new List<SNodeData>();

        public static List<STrains> Trains { get; set; } = new List<STrains>();
        public static List<SNodeData> Nodes { get => nodes; set { nodes = value; UpdateRequired = true; } }
        public static List<SRailBlocks> Blocks { get; set; } = new List<SRailBlocks>();
        public static bool UpdateRequired { get; set; } = false;
        public static List<ushort> GreenLights { get; set; } = new List<ushort>();
        public static List<SWaitingList> WaitingList { get; set; } = new List<SWaitingList>();

        public static void InitData()
        {
            Trains = new List<STrains>();
            Nodes = new List<SNodeData>();
            Blocks = new List<SRailBlocks>();
            UpdateRequired = false;
            WaitingList = new List<SWaitingList>();
            GreenLights = new List<ushort>();
        }

        public static void UpdateBlock(ushort blockId, ushort trainId)
        {
            var block = Blocks.FirstOrDefault(x => x.BlockId == blockId);
            if (block == null) return;
            block.BlockedBy = trainId;
        }

        public static void AddNode(ushort nodeID, List<ushort> segments, bool debugMode = false)
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
            catch (Exception ex) { if (debugMode) DOP.Show(ex.Message + Environment.NewLine + ex.StackTrace, DOP.MessageType.Error); }
        }

        public static void RemoveNode(ushort nodeID, bool debugMode = false)
        {
            try
            {
                if (nodeID == 0) return;
                if (!Nodes.IsValid()) return;

                Nodes.RemoveAll(x => x.NodeID == nodeID);

                CheckNodes();
            }
            catch (Exception ex) { if (debugMode) DOP.Show(ex.Message + Environment.NewLine + ex.StackTrace, DOP.MessageType.Error); }
        }

        public static void CheckNodes(bool debugMode = false)
        {
            try
            {
                foreach (var node in Nodes)
                {
                    NetNode netNode = node.NodeID.ToNode();
                    NetNode.Flags flags = netNode.m_flags;
                    if (flags.CheckFlags(NetNode.Flags.Created | NetNode.Flags.Junction | NetNode.Flags.TrafficLights, NetNode.Flags.LevelCrossing | NetNode.Flags.Untouchable))
                    { /* valid node... do nothing */ }
                    else { node.NodeID = 0; }
                }
                Nodes.RemoveAll(x => x.NodeID == 0);
            }
            catch (Exception ex) { if (debugMode) DOP.Show(ex.Message + Environment.NewLine + ex.StackTrace, DOP.MessageType.Error); }
        }

    }
}
