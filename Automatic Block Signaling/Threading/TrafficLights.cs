using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using static dmaTrainABS.GameData.Declarations;

namespace dmaTrainABS
{
    public class TrafficLights
    {

        private static NetNode[] Nodes = Singleton<NetManager>.instance.m_nodes.m_buffer;
        private static NetSegment[] Segments = Singleton<NetManager>.instance.m_segments.m_buffer;

        public static void SetTrafficLights(List<SNodeData> NodeData, bool forceUpdate = false)
        {
            try
            {
                if (NodeData.IsValid())
                {
                    foreach (SNodeData nodeData in NodeData)
                    {
                        NetNode node = Nodes[nodeData.NodeID];

                        NetNode.Flags flags = node.m_flags;
                        flags |= NetNode.Flags.TrafficLights | NetNode.Flags.CustomTrafficLights | NetNode.Flags.Junction;
                        Nodes[nodeData.NodeID].m_flags = flags;

                        if (flags.IsFlagSet(NetNode.Flags.Original))
                        {// node is out of area
                            for (int i = 0; i < node.CountSegments(); ++i)
                            {
                                ushort SegmentId = node.GetSegment(i);
                                if (SegmentId == 0) continue;
                                if (SegmentId.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Original))
                                {
                                    if (nodeData.Segments.Any(x => x.SegmentID == SegmentId && x.GreenState)) SetGreenState(SegmentId);
                                    if (nodeData.Segments.Any(x => x.SegmentID == SegmentId && !x.GreenState)) SetRedState(SegmentId);
                                }
                                else
                                {
                                    SetGreenState(SegmentId);
                                }
                            }
                        }
                        else
                        {// in-game node
                            for (int i = 0; i < node.CountSegments(); ++i)
                            {
                                ushort SegmentId = node.GetSegment(i); var greenState = nodeData.Segments.FirstOrDefault(x => x.SegmentID == SegmentId)?.GreenState;
                                if (SegmentId == 0) continue;
                                if (nodeData.Segments.Any(x => x.SegmentID == SegmentId && x.GreenState)) SetGreenState(SegmentId);
                                if (nodeData.Segments.Any(x => x.SegmentID == SegmentId && !x.GreenState)) SetRedState(SegmentId);
                            }
                        }

                        if (forceUpdate)
                            NetManager.instance.UpdateNode(nodeData.NodeID);
                    }
                }
            }
            catch (Exception ex) { DOP.Show(ex.Message + Environment.NewLine + ex.StackTrace, DOP.MessageType.Error); }
        }

        internal static void SetRedState(ushort SegmentId)
        {
            //          R E D                            3210321032103210
            Segments[SegmentId].m_trafficLightState0 = 0b0010001000100010;
            Segments[SegmentId].m_trafficLightState1 = 0b0010001000100010;
        }

        internal static void SetGreenState(ushort SegmentId)
        {
            //          G R E E N                        3210321032103210
            Segments[SegmentId].m_trafficLightState0 = 0b0000000000000000;
            Segments[SegmentId].m_trafficLightState1 = 0b0000000000000000;
        }

        internal static void ClearGreen(STrains train)
        {// fix for extra long trains
            var node = SimData.Nodes.FirstOrDefault(x => x.NodeID == train.SignalNode);
            if (node == null) return;
            var segment = node.Segments.FirstOrDefault(x => x.LockedBy == train.TrainID && x.GreenState);
            if (segment == null) return;
            segment.LockedBy = 0;
            segment.GreenState = false;
            train.SignalNode = 0;
        }

    }
}
