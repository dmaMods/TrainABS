using ColossalFramework;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

        public static void LoadNetwork()
        {
            if (!SimData.Nodes.IsValid()) return;

            SimData.ProcessN++;
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
                foreach (ushort seg in nodeSegs)
                {
                    NetSegment segment = seg.ToSegment();
                    if (!(segment.Info.m_connectGroup == NetInfo.ConnectGroup.SingleTrain ||
                          segment.Info.m_connectGroup == NetInfo.ConnectGroup.DoubleTrain ||
                          segment.Info.m_connectGroup == NetInfo.ConnectGroup.TrainStation)) continue;
                    //if (segment.m_flags.IsFlagSet(NetSegment.Flags.Original)) continue;
                    ushort sNode = segment.m_startNode;
                    ushort eNode = segment.m_endNode;
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
                    bool endSegment = segment.m_flags.IsFlagSet(NetSegment.Flags.End);
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
                            Lane = 2,
                            OutOfArea = segment.m_flags.IsFlagSet(NetSegment.Flags.Original)
                        });
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

                blocks = CalculateBlocks(); blockSegments.RemoveAll(x => x.OutOfArea);
                SimData.Blocks = CreateRailwayBlocks();
                SimData.UpdateRequired = false;
            }
            catch (Exception ex) { Debug.LogException(ex); }
            if (IsDebugMode)
            {
                string txt = "[TrainABS] Network updated. Blocks: " + blocks.Count + ", Nodes: " + blockNodes.Count + ", Junctions: " + blockNodes.Count(x => x.IsJunction) + ", Segments: " + blockSegments.Count;

                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, txt);
            }
        }

        private static Dictionary<ushort, SRailBlocks> CreateRailwayBlocks()
        {
            Dictionary<ushort, SRailBlocks> rblocks = new Dictionary<ushort, SRailBlocks>();
            foreach (var block in blocks)
            {
                rblocks.Add(
                    block.BlockId,
                    new SRailBlocks
                    {
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

            ReorderSegments(retBlocks);
            OrderBlocks(retBlocks);

            foreach (var block in retBlocks)
                if (!blockSegments.Any(x => x.BlockId == block.BlockId)) block.BlockId = 0;

            retBlocks.RemoveAll(x => x.BlockId == 0);

            var blockAdd = retBlocks.Max(x => x.BlockId) + 1000;
            List<Segment> newSegments = new List<Segment>();
            foreach (var segment in blockSegments)
            {
                var blockId = (ushort)(segment.BlockId + blockAdd);
                newSegments.Add(new Segment
                {
                    BlockId = blockId,
                    EndNode = segment.EndNode,
                    EndSegment = segment.EndSegment,
                    Inverted = segment.Inverted,
                    Lane = segment.Lane.RevertLane(),
                    Processed = false,
                    SegmentId = segment.SegmentId,
                    StartNode = segment.StartNode,
                    OutOfArea = segment.OutOfArea
                });
                retBlocks.AddNew(new Block { BlockId = blockId, BlockedBy = 0 });
            }
            blockSegments.AddRange(newSegments);

            retBlocks.All(c => { c.BlockedBy = 0; return true; });

            return retBlocks;
        }

        private static void ReorderSegments(List<Block> Blocks)
        {
            List<Segment> newSegments = new List<Segment>();
            foreach (var block in Blocks)
            {
                var sNode = block.StartNode;
                var bSegments = blockSegments.Where(x => x.BlockId == block.BlockId);
                if (bSegments.Count() == 0) continue;
                Loop1:
                var bSegment = bSegments.FirstOrDefault(x => (x.StartNode == sNode || x.EndNode == sNode) && !x.Processed);
                if (bSegment == null) continue;
                bSegment.Processed = true;
                newSegments.Add(bSegment);
                sNode = bSegment.StartNode == sNode ? bSegment.EndNode : bSegment.StartNode;
                goto Loop1;
            }
            blockSegments = newSegments;
        }

        private static void OrderBlocks(List<Block> Blocks)
        {
            foreach (var block in Blocks)
            {
                var sNode = block.StartNode;
                var bSegments = blockSegments.Where(x => x.BlockId == block.BlockId);
                if (bSegments.Count() == 0) continue;
                foreach (var segment in bSegments)
                {
                    if (!segment.Inverted)
                    {
                        if (segment.StartNode == sNode) { segment.Lane = 1u; sNode = segment.EndNode; }
                        else { segment.Lane = 0; sNode = segment.StartNode; }
                    }
                    else
                    {
                        if (segment.StartNode == sNode) { segment.Lane = 0; sNode = segment.EndNode; }
                        else { segment.Lane = 1u; sNode = segment.StartNode; }
                    }
                }
            }
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

        public static void ShowBlocks()
        {
            string NL = Environment.NewLine;
            string bText = "=== NETWORK ===" + NL;
            bText += "Blocks: " + blocks.Count + ", Nodes: " + blockNodes.Count + ", Junctions: " + blockNodes.Count(x => x.IsJunction) + ", Segments: " + blockSegments.Count;
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, bText);

            //bText = "=== JUNCTION NODES ===" + NL; int cdnt = 0;
            //foreach (var node in blockNodes.Where(x => x.IsJunction))
            //{
            //    cdnt++;
            //    bText += "Node " + cdnt.ToString("000") + ": " + node.NodeId + ", Segments: " + node.Segments + ", Flags: " + NetNodes[node.NodeId].m_flags + NL;
            //}
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, bText);

            //bText = "=== RAILWAY BLOCKS ===" + NL;
            //foreach (var block in SimData.Blocks)
            //{
            //    bText += "Block " + block.Key + ", Start Node: " + block.Value.StartNode + ", End Node: " + block.Value.EndNode + ", Segments: " + blockSegments.Count(x => x.BlockId == block.Key) +
            //        ", First Segment: " + blockSegments.FirstOrDefault(x => x.BlockId == block.Key)?.SegmentId +
            //        ", Last Segment: " + blockSegments.LastOrDefault(x => x.BlockId == block.Key)?.SegmentId + NL;
            //}
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, bText);

            //bText = "=== SEGMENTS ===" + NL; cdnt = 0;
            //foreach (var seg in blockSegments.Where(x => x.BlockId == 1000))
            //{
            //    cdnt++;
            //    bText += "Segment " + cdnt.ToString("000") + ": " + seg.SegmentId + (seg.Inverted ? " Inverted" : "") + (seg.EndSegment ? " End" : "") +
            //     ", Lane: " + seg.Lane + ", Start Node: " + seg.StartNode + ", End Node: " + seg.EndNode + ", BlockId: " + seg.BlockId + NL;
            //}
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, bText);
        }

    }
}
