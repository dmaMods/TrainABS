using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using static dmaTrainABS.GameData.Declarations;
using static dmaTrainABS.Traffic.BlockDataVars;

namespace dmaTrainABS
{
    public class BlockData
    {
        public static List<Segment> blockSegments = new List<Segment>();
        private static List<Block> blocks = new List<Block>();
        private static List<Node> blockNodes = new List<Node>();

        private static readonly NetNode[] NetNodes = Singleton<NetManager>.instance.m_nodes.m_buffer;

        public static void LoadNetwork(bool debugMode = false)
        {
            if (!SimData.Nodes.IsValid()) return;

            blockSegments = new List<Segment>();
            blocks = new List<Block>();
            blockNodes = new List<Node>();

            ushort nodeId = (ushort)SimData.Nodes.FirstOrDefault()?.NodeID;
            if (nodeId == 0) return;

            Loop3:
            blockNodes.Add(new Node
            {
                NodeId = nodeId,
                Processed = false,
                Segments = NetNodes[nodeId].CountSegments(),
                HaveLights = NetNodes[nodeId].m_flags.IsFlagSet(NetNode.Flags.TrafficLights),
                IsJunction = NetNodes[nodeId].IsValidJunction()
            });

            try
            {
            Loop1:
                NetNode netNode = NetNodes[nodeId];
                List<ushort> nodeSegs = GetNodeSegments(nodeId);
                //DOP.Show("Node " + nodeId + ", Segments (" + nodeSegs.Count + "/" + netNode.CountSegments() + "): " + string.Join(", ", nodeSegs.Select(x => x.ToString()).ToArray()));
                foreach (ushort seg in nodeSegs)
                {
                    NetSegment segment = seg.ToSegment();
                    if (!(segment.Info.m_connectGroup == NetInfo.ConnectGroup.SingleTrain ||
                          segment.Info.m_connectGroup == NetInfo.ConnectGroup.DoubleTrain ||
                          segment.Info.m_connectGroup == NetInfo.ConnectGroup.TrainStation)) continue;
                    //{
                    //    DOP.Show("Segment " + seg + " SKIPPED. Conn.Group: " + segment.Info.m_connectGroup);
                    //    continue;
                    //}
                    ushort sNode = segment.m_startNode;
                    ushort eNode = segment.m_endNode;
                    //DOP.Show("Node Id: " + nodeId + ", Segment: " + seg + ", S.Node: " + sNode + ", E.Node: " + eNode);
                    if (sNode != nodeId && !blockNodes.Any(x => x.NodeId == sNode) && NetNodes[sNode].IsValid())
                        blockNodes.Add(new Node
                        {
                            NodeId = sNode,
                            Processed = false,
                            Segments = NetNodes[sNode].CountSegments(),
                            HaveLights = NetNodes[sNode].m_flags.IsFlagSet(NetNode.Flags.TrafficLights),
                            IsJunction = NetNodes[sNode].IsValidJunction()
                        });
                    if (eNode != nodeId && !blockNodes.Any(x => x.NodeId == eNode) && NetNodes[eNode].IsValid())
                        blockNodes.Add(new Node
                        {
                            NodeId = eNode,
                            Processed = false,
                            Segments = NetNodes[eNode].CountSegments(),
                            HaveLights = NetNodes[eNode].m_flags.IsFlagSet(NetNode.Flags.TrafficLights),
                            IsJunction = NetNodes[eNode].IsValidJunction()
                        });
                    bool endSegment = segment.m_flags.IsFlagSet(NetSegment.Flags.End) || segment.m_flags.IsFlagSet(NetSegment.Flags.TrafficEnd);
                    bool inverted = segment.m_flags.IsFlagSet(NetSegment.Flags.Invert);
                    if (!blockSegments.Any(x => x.SegmentId == seg) &&
                            (segment.Info.m_connectGroup.IsFlagSet(NetInfo.ConnectGroup.SingleTrain) ||
                             segment.Info.m_connectGroup.IsFlagSet(NetInfo.ConnectGroup.DoubleTrain) ||
                             segment.Info.m_connectGroup.IsFlagSet(NetInfo.ConnectGroup.TrainStation)))
                        blockSegments.Add(new Segment
                        {
                            SegmentId = seg,
                            StartNode = sNode,
                            EndNode = eNode,
                            EndSegment = endSegment,
                            Inverted = inverted,
                            BlockId = 0,
                            Lane = CheckLane(nodeId == sNode ? (byte)0 : (byte)1, inverted)
                        });
                    //DOP.Show("Segment: " + seg + ", S.Node: " + sNode + ", E.Node: " + eNode + ", C.Node: " + nodeId + ", Inverted: " + inverted + ", Lane: " + (nodeId == sNode ? "Y" : "R"));
                    if (endSegment && sNode != nodeId) SetProcessed(sNode);
                    if (endSegment && eNode != nodeId) SetProcessed(eNode);
                    SetProcessed(nodeId);
                }
                if (blockNodes.Any(x => !x.Processed))
                {
                    var next = blockNodes.FirstOrDefault(x => !x.Processed);
                    if (next != null) { nodeId = next.NodeId; goto Loop1; }
                }

                foreach (var node in SimData.Nodes)
                    if (!blockNodes.Any(x => x.NodeId == node.NodeID)) { nodeId = node.NodeID; goto Loop3; }

                blocks = CalculateBlocks();
                SimData.Blocks = CreateRailwayBlocks();
                SimData.UpdateRequired = false;
                if (debugMode) DOP.Show("Temp. Blocks created: " + blocks.Count + ", Railway Blocks: " + SimData.Blocks.Count);
            }
            catch (Exception ex) { DOP.Show(ex.Message + Environment.NewLine + ex.StackTrace, DOP.MessageType.Error); }
        }

        private static byte CheckLane(byte lane, bool inverted)
        {
            if (inverted) return lane == 0 ? (byte)1 : (byte)0;
            return lane;
        }

        private static List<SRailBlocks> CreateRailwayBlocks()
        {
            List<SRailBlocks> rblocks = new List<SRailBlocks>();
            foreach (var block in blocks)
            {
                rblocks.Add(new SRailBlocks
                {
                    BlockId = block.BlockId,
                    StartNode = block.StartNode,
                    EndNode = block.EndNode,
                    BlockedBy = 0,
                    Segments = blockSegments.Where(x => x.BlockId == block.BlockId).Select(x => x.SegmentId).ToList()
                }); ;
            }
            return rblocks;
        }

        private static void SetProcessed(ushort nodeId)
        {
            var node = blockNodes.FirstOrDefault(x => x.NodeId == nodeId);
            if (node != null) node.Processed = true;
        }

        /// <summary>
        /// BLOCKS CALCULATION ROUTINE
        /// </summary>
        /// <returns>List of all available blocks</returns>
        private static List<Block> CalculateBlocks()
        {
            List<Block> retBlocks = new List<Block>(); ushort BlockId = 1000;
            foreach (var node in blockNodes.Where(x => x.IsJunction))
            {
                var nodeSegments = GetNodeSegments(node.NodeId);
                foreach (var nsegment in nodeSegments)
                {
                    var segment = blockSegments.FirstOrDefault(x => x.SegmentId == nsegment);
                    if (segment == null) continue;
                    if (segment.BlockId == 0) segment.BlockId = BlockId;
                    var block = new Block { BlockId = BlockId, StartNode = node.NodeId, BlockedBy = 0 };
                    block.EndNode = GetBlockSegments(node.NodeId, nsegment, BlockId);
                    retBlocks.Add(block);
                    BlockId++;
                }
            }

            foreach (var block in retBlocks.Where(x => x.EndNode == 0))
            {
                var segments = blockSegments.Where(x => x.BlockId == block.BlockId);
                if (segments.Count() == 1)
                {
                    var seg = segments.First().SegmentId.ToSegment();
                    var sNode = seg.m_startNode; var eNode = seg.m_endNode;
                    block.EndNode = block.StartNode != sNode ? sNode : eNode;
                }
            }

            foreach (var block in retBlocks.Where(x => x.EndNode == 0))
            {
                var otherBlock = retBlocks.FirstOrDefault(x => x.EndNode == block.StartNode && x.BlockedBy == 0);
                if (otherBlock != null)
                {
                    var segments = blockSegments.Where(x => x.BlockId == otherBlock.BlockId);
                    var reverse = Enumerable.Reverse(segments).ToList();
                    foreach (var seg in reverse)
                        blockSegments.Add(new Segment
                        {
                            BlockId = block.BlockId,
                            EndNode = seg.EndNode,
                            EndSegment = seg.EndSegment,
                            SegmentId = seg.SegmentId,
                            StartNode = seg.StartNode,
                            Inverted = seg.Inverted,
                            Lane = seg.Lane != 0 ? (byte)0 : (byte)1// other lane
                        });
                    block.EndNode = otherBlock.StartNode;
                    otherBlock.BlockedBy = block.BlockId;
                    block.BlockedBy = otherBlock.BlockId;
                }
            }

            return retBlocks;
        }

        private static ushort GetBlockSegments(ushort nodeId, ushort segmentId, ushort blockId)
        {
            ushort pNodeId = nodeId;
        Loop2:
            Segment segment = blockSegments.FirstOrDefault(x => x.SegmentId == segmentId);
            if (segment == null) return 0;
            ushort nNodeId = segment.StartNode != pNodeId ? segment.StartNode : segment.EndNode;
            var nodeSegments = GetNodeSegments(nNodeId);
            var csegment = blockSegments.FirstOrDefault(x => x.BlockId == 0 && nodeSegments.Contains(x.SegmentId) && x.StartNode != pNodeId && x.EndNode != pNodeId);
            if (csegment == null) { return 0; }
            if (segment.BlockId == 0) segment.BlockId = blockId;
            var junction = blockNodes.FirstOrDefault(x => x.NodeId == nNodeId);
            if (junction != null)
                if (junction.IsJunction) return nNodeId;
            segmentId = csegment.SegmentId; pNodeId = nNodeId; goto Loop2;
        }

        public static List<ushort> GetNodeSegments(ushort nodeId)
        {
            var netNode = NetNodes[nodeId];
            List<ushort> ret = new List<ushort>();
            for (int i = 0; i < netNode.CountSegments(); i++)
                ret.Add(netNode.GetSegment(i));
            return ret;
        }

        public static void ShowBlocks(bool debugMode)
        {
            if (!debugMode) return;
            string NL = Environment.NewLine;
            string bText = "=== NETWORK ===" + NL;
            bText += "Blocks: " + blocks.Count + ", Nodes: " + blockNodes.Count + ", Junctions: " + blockNodes.Count(x => x.IsJunction) + ", Segments: " + blockSegments.Count;
            DOP.Show(bText);

            bText = "=== JUNCTION NODES ===" + NL; int cdnt = 0;
            foreach (var node in blockNodes.Where(x => x.IsJunction))
            {
                cdnt++;
                bText += "Node " + cdnt.ToString("000") + ": " + node.NodeId + ", Segments: " + node.Segments + ", Flags: " + NetNodes[node.NodeId].m_flags + NL;
            }
            DOP.Show(bText);

            bText = "=== BLOCKS ===" + NL;
            foreach (var block in blocks)
            {
                bText += "Block " + block.BlockId + ", Start Node: " + block.StartNode + ", End Node: " + block.EndNode + ", Segments: " + blockSegments.Count(x => x.BlockId == block.BlockId) +
                    ", First Segment: " + blockSegments.FirstOrDefault(x => x.BlockId == block.BlockId)?.SegmentId +
                    ", Last Segment: " + blockSegments.LastOrDefault(x => x.BlockId == block.BlockId)?.SegmentId + NL;
            }
            DOP.Show(bText);

            bText = "=== SEGMENTS ===" + NL; cdnt = 0;
            foreach (var seg in blockSegments.Where(x => x.BlockId == 1000))
            {
                cdnt++;
                bText += "Segment " + cdnt.ToString("000") + ": " + seg.SegmentId + (seg.Inverted ? " Inverted" : "") + (seg.EndSegment ? " End" : "") +
                 ", Lane: " + seg.Lane + ", Start Node: " + seg.StartNode + ", End Node: " + seg.EndNode + ", BlockId: " + seg.BlockId + NL;
            }
            DOP.Show(bText);
        }

    }

}
