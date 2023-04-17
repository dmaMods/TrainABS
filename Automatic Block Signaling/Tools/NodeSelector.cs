using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace dmaTrainABS
{
    public class NodeSelector : ToolBase
    {

        private NetManager netManager = Singleton<NetManager>.instance;
        private SimulationManager simulationManager = Singleton<SimulationManager>.instance;

        private NetTool.ControlPoint controlPoint;
        private NetTool.ControlPoint cachedControlPoint;
        private ToolErrors buildErrors;
        private ToolErrors cachedErrors;
        private Ray mouseRay;
        private float mouseRayLength;
        private bool mouseRayValid;
        private NetInfo preFab;
        private ushort selectedNodeId, selectedSegmentId;
        private object cacheLock;

        public static ToolErrors dbgError { get; set; }
        public static ushort selNode { get; set; }
        public static ushort selSegment { get; set; }
        public static ushort dbgNode { get; set; }
        public static ushort dbgSegment { get; set; }
        public NodeAction Action { get; private set; }

        protected override void Awake()
        {
            m_toolController = FindObjectOfType<ToolController>();
            cacheLock = new object();
        }

        private void GetNetAtCursor()
        {
            Vector3 mousePosition = Input.mousePosition;
            RaycastInput input = new RaycastInput(Camera.main.ScreenPointToRay(mousePosition), Camera.main.farClipPlane);
            RaycastOutput output;

            input.m_netService = new RaycastService(ItemClass.Service.Road, ItemClass.SubService.None, ItemClass.Layer.Default);
            input.m_netService.m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels;
            input.m_netService.m_service = ItemClass.Service.PublicTransport;
            input.m_netService.m_subService = ItemClass.SubService.PublicTransportTrain;
            input.m_ignoreNodeFlags = NetNode.Flags.Untouchable | NetNode.Flags.LevelCrossing | NetNode.Flags.Original;
            input.m_ignoreTerrain = true;
            input.m_ignoreSegmentFlags = NetSegment.Flags.None;

            if (RayCast(input, out output))
            {
                selectedNodeId = output.m_netNode;
                selectedSegmentId = output.m_netSegment;
            }
            else
            {
                selectedNodeId = 0;
                selectedSegmentId = 0;
            }
            selNode = selectedNodeId;
            selSegment = selectedSegmentId;
        }

        protected override void OnToolGUI(Event e)
        {
            bool isInsideUI = this.m_toolController.IsInsideUI;
            if (e.type == EventType.MouseDown && !isInsideUI)
            {
                if (e.button == 0)
                {
                    if (this.cachedErrors == ToolErrors.None && (selectedNodeId != 0 || selectedSegmentId != 0))
                    {
                        if (Action == NodeAction.AddNode)
                            simulationManager.AddAction(this.AddTrafficLights());
                        else if (Action == NodeAction.RemoveNode)
                            simulationManager.AddAction(this.RemoveTrafficLights());
                        else if (Action == NodeAction.InsertNode)
                            simulationManager.AddAction(this.CreateStraightNode());
                        else if (Action == NodeAction.Info)
                        {
                            simulationManager.AddAction(this.ShowInfo());
                            var segments = BlockData.blockSegments.Where(x => x.SegmentId == selectedSegmentId);
                            string block = ""; string laneInfo = ""; string NL = Environment.NewLine;
                            foreach (var seg in segments)
                                block += "Block: " + seg.BlockId + ", Lane: " + seg.Lane + LaneColor(seg.Lane) + NL;
                            NetNode netNode = netManager.m_nodes.m_buffer[selectedNodeId];
                            NetSegment netSegment = netManager.m_segments.m_buffer[selectedSegmentId];
                            var netLane = NetManager.instance.m_lanes.m_buffer[netSegment.m_lanes];

                            laneInfo += "m_lanes: " + netSegment.m_lanes + ", Flags: " + netLane.m_flags + ", Next Lane: " + netLane.m_nextLane;
                            var sNode = selectedSegmentId.ToSegment().m_startNode;
                            var eNode = selectedSegmentId.ToSegment().m_endNode;

                            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
                                "Start Node ID: " + sNode + ", Flags: " + sNode.ToNode().m_flags + NL +
                                "End Node ID: " + eNode + ", Flags: " + eNode.ToNode().m_flags + NL +
                                "Segment: " + selectedSegmentId + ", Flags: " + netSegment.m_flags + NL +
                                block + Environment.NewLine + laneInfo);
                        }
                    }
                }
            }
        }

        internal static string LaneColor(uint lane)
        {
            if (lane == 0) return " (G)";
            else return " (R)";
        }

        private IEnumerator ShowInfo()
        {
            yield return null;
        }

        private IEnumerator RemoveTrafficLights()
        {
            var node = netManager.m_nodes.m_buffer[selectedNodeId];
            NetNode.Flags flags = node.m_flags;
            flags &= ~NetNode.Flags.TrafficLights;
            flags &= ~NetNode.Flags.CustomTrafficLights;
            netManager.m_nodes.m_buffer[selectedNodeId].m_flags = flags;

            SimData.RemoveNode(selectedNodeId);

            yield return null;
        }

        private IEnumerator AddTrafficLights()
        {
            try
            {
                if (CanBuild(selectedSegmentId, selectedNodeId))
                {
                    var node = netManager.m_nodes.m_buffer[selectedNodeId];
                    NetNode.Flags flags = node.m_flags;
                    flags |= NetNode.Flags.CustomTrafficLights;
                    flags |= NetNode.Flags.TrafficLights;
                    flags |= NetNode.Flags.Junction;
                    netManager.m_nodes.m_buffer[selectedNodeId].m_flags = flags;

                    for (int i = 0; i < 8; ++i)
                    {
                        ushort SegmentId = node.GetSegment(i);
                        netManager.m_segments.m_buffer[SegmentId].m_trafficLightState0 = 0b0010001000100010;// Red;
                        netManager.m_segments.m_buffer[SegmentId].m_trafficLightState1 = 0b0010001000100010;// Red;
                    }

                    List<ushort> segments = new List<ushort>();
                    if (node.m_segment0 != 0) segments.Add(node.m_segment0);
                    if (node.m_segment1 != 0) segments.Add(node.m_segment1);
                    if (node.m_segment2 != 0) segments.Add(node.m_segment2);
                    if (node.m_segment3 != 0) segments.Add(node.m_segment3);
                    if (node.m_segment4 != 0) segments.Add(node.m_segment4);
                    if (node.m_segment5 != 0) segments.Add(node.m_segment5);
                    if (node.m_segment6 != 0) segments.Add(node.m_segment6);
                    if (node.m_segment7 != 0) segments.Add(node.m_segment7);

                    SimData.AddNode(selectedNodeId, segments);
                }
            }
            catch { }
            yield return null;
        }

        override protected void OnToolUpdate()
        {
            while (!Monitor.TryEnter(this.cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
            try
            {
                this.cachedControlPoint = this.controlPoint;
                this.cachedErrors = this.buildErrors;
            }
            finally
            {
                Monitor.Exit(this.cacheLock);
            }

            base.OnToolUpdate();

            GetNetAtCursor();

            if (selectedSegmentId != 0)
            {
                NetSegment segment = netManager.m_segments.m_buffer[selectedSegmentId];
                preFab = PrefabCollection<NetInfo>.GetPrefab(segment.m_infoIndex);
            }
            else
            {
                preFab = null;
            }
        }

        protected override void OnToolLateUpdate()
        {
            if (preFab == null)
            {
                return;
            }
            Vector3 mousePosition = Input.mousePosition;
            this.mouseRay = Camera.main.ScreenPointToRay(mousePosition);
            this.mouseRayLength = Camera.main.farClipPlane;
            this.mouseRayValid = (!this.m_toolController.IsInsideUI && Cursor.visible);
            ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.NormalPower);
        }

        override public void SimulationStep()
        {
            ServiceTypeGuide optionsNotUsed = Singleton<NetManager>.instance.m_optionsNotUsed;
            if (optionsNotUsed != null && !optionsNotUsed.m_disabled)
            {
                optionsNotUsed.Disable();
            }

            Vector3 position = this.controlPoint.m_position;
            bool failed = false;

            NetTool.ControlPoint controlPoint = default(NetTool.ControlPoint);
            NetNode.Flags ignoreNodeFlags;
            NetSegment.Flags ignoreSegmentFlags;

            ignoreNodeFlags = NetNode.Flags.None;
            ignoreSegmentFlags = NetSegment.Flags.None;

            Building.Flags ignoreBuildingFlags = Building.Flags.All;

            if (preFab != null)
            {
                if (this.mouseRayValid && NetTool.MakeControlPoint(this.mouseRay, this.mouseRayLength, preFab, false,
                    ignoreNodeFlags, ignoreSegmentFlags, ignoreBuildingFlags, 0, false, out controlPoint))
                {
                    if (controlPoint.m_node == 0 && controlPoint.m_segment == 0 && !controlPoint.m_outside)
                    {
                        controlPoint.m_position.y = NetSegment.SampleTerrainHeight(preFab, controlPoint.m_position, false, 0) + controlPoint.m_elevation;
                    }
                }
                else
                {
                    failed = true;
                }
            }
            this.controlPoint = controlPoint;

            this.m_toolController.ClearColliding();

            ToolErrors toolErrors = ToolErrors.None;
            if (failed) { toolErrors |= ToolErrors.RaycastFailed; }
            while (!Monitor.TryEnter(this.cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
            try { this.buildErrors = toolErrors; }
            finally { Monitor.Exit(this.cacheLock); }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (KeyboardInput._nodeSel)
            {
                if (!this.m_toolController.IsInsideUI && Cursor.visible && (selectedNodeId != 0 || selectedSegmentId != 0))
                {
                    NetTool.ControlPoint controlPoint = this.cachedControlPoint;

                    BuildingInfo buildingInfo;
                    Vector3 vector;
                    Vector3 vector2;
                    int num3;

                    if (preFab != null)
                    {
                        preFab.m_netAI.CheckBuildPosition(false, false, true, true, ref controlPoint, ref controlPoint, ref controlPoint, out buildingInfo, out vector, out vector2, out num3);

                        Color colour = Color.cyan; bool baseCircle = selectedNodeId == 0;
                        var node = NetManager.instance.m_nodes.m_buffer[selectedNodeId];
                        NetNode.Flags flags = node.m_flags; Action = NodeAction.Default;

                        if (selectedNodeId != 0)
                        {
                            if (SimData.Nodes.Any(x => x.NodeID == selectedNodeId))
                            {
                                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                                {
                                    colour = Color.magenta; Action = NodeAction.Info;
                                }
                                else
                                {
                                    colour = Color.red; Action = NodeAction.RemoveNode;
                                }
                            }
                            else
                            {
                                if (flags.IsFlagSet(NetNode.Flags.Junction) && !flags.IsFlagSet(NetNode.Flags.LevelCrossing)/* && !flags.IsFlagSet(NetNode.Flags.Untouchable)*/)
                                {
                                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                                    {
                                        colour = Color.magenta; Action = NodeAction.Info;
                                    }
                                    else
                                    {
                                        colour = Color.green; Action = NodeAction.AddNode;
                                    }
                                }
                                else
                                {
                                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                                    {
                                        colour = Color.yellow; Action = NodeAction.InsertNode;
                                    }
                                    else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                                    {
                                        colour = Color.magenta; Action = NodeAction.Info;
                                    }
                                    else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                    {
                                        colour = Color.gray; Action = NodeAction.RemoveNode;
                                    }
                                }
                            }
                        }
                        else // NODE = 0
                        {
                            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                colour = Color.yellow; Action = NodeAction.InsertNode; baseCircle = false;
                            }
                            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                            {
                                colour = Color.magenta; Action = NodeAction.Info; baseCircle = false;
                            }
                        }

                        ToolManager toolManager = Singleton<ToolManager>.instance;
                        toolManager.m_drawCallData.m_overlayCalls++;
                        if (baseCircle)
                            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, Color.white, this.controlPoint.m_position, preFab.m_halfWidth * 1.25f, -1f, 1280f, false, false);
                        else
                            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, colour, this.controlPoint.m_position, preFab.m_halfWidth * 2f, -1f, 1280f, false, false);
                    }
                }
            }

            if (KeyboardInput._showBlocks)
            {
                if (!this.m_toolController.IsInsideUI && Cursor.visible && (selectedNodeId != 0 || selectedSegmentId != 0))
                {
                    NetTool.ControlPoint controlPoint = this.cachedControlPoint;
                    BuildingInfo buildingInfo; Vector3 vector; Vector3 vector2; int num3;

                    if (preFab != null)
                    {
                        preFab.m_netAI.CheckBuildPosition(false, false, true, true, ref controlPoint, ref controlPoint, ref controlPoint, out buildingInfo, out vector, out vector2, out num3);

                        Color colour = Color.cyan; bool baseCircle = selectedNodeId == 0;
                        var node = NetManager.instance.m_nodes.m_buffer[selectedNodeId];
                        NetNode.Flags flags = node.m_flags; Action = NodeAction.Default;

                        ToolManager toolManager = Singleton<ToolManager>.instance;
                        toolManager.m_drawCallData.m_overlayCalls++;

                        DrawBlock(selectedSegmentId, cameraInfo);
                    }
                }
            }
        }

        private void DrawBlock(ushort segmentId, RenderManager.CameraInfo cameraInfo)
        {
            if (segmentId == 0) return;

            var blocks = BlockData.blockSegments.Where(x => x.SegmentId == segmentId).Select(x => x.BlockId).ToList();
            if (blocks.Count == 0) return;
            var freeBlocks = new Dictionary<ushort, bool>();
            var fBlocks = SimData.Blocks.Where(x => blocks.Contains(x.Key)).ToList();
            foreach (var block in fBlocks)
                freeBlocks.Add(block.Key, block.Value.Blocked);

            var segments = BlockData.blockSegments.Where(x => blocks.Contains(x.BlockId)); Color colour = Color.cyan;

            foreach (var segment in segments)
            {
                var netSegment = segment.SegmentId.ToSegment();
                netSegment.GetClosestPositionAndDirection(controlPoint.m_position, out Vector3 pos, out Vector3 dir);
                var node1 = netSegment.m_startNode.ToNode(); var node2 = netSegment.m_endNode.ToNode();

                var startPos = netSegment.m_startNode.ToNode().m_position;
                var startDir = netSegment.m_startDirection;
                var endPos = netSegment.m_endNode.ToNode().m_position;
                var endDir = netSegment.m_endDirection;
                var bezier = new Bezier3();

                if (netSegment.GetClosestLane((int)segment.Lane, NetInfo.LaneType.Vehicle, VehicleInfo.VehicleType.Train, VehicleInfo.VehicleCategory.Trains, out int laneIndex, out uint laneId))
                {
                    var netLane = NetManager.instance.m_lanes.m_buffer[laneId];
                    bezier = netLane.m_bezier;
                    colour = freeBlocks[segment.BlockId] ? Color.red : Color.green;
                }
                else
                {
                    NetSegment.CalculateMiddlePoints(startPos, startDir, endPos, endDir, true, true, out Vector3 midPos1, out Vector3 midPos2);
                    bezier = new Bezier3 { a = startPos, b = midPos1, c = midPos2, d = endPos };
                    colour = Color.white;
                }
                Singleton<RenderManager>.instance.OverlayEffect.DrawBezier(cameraInfo, colour, bezier.Flat(), preFab.m_halfWidth / 2.5f, 0, 0, -1f, 1280f, false, true);

                NetSegment.CalculateMiddlePoints(startPos, startDir, endPos, endDir, true, true, out Vector3 midPos1a, out Vector3 midPos2a);
                bezier = new Bezier3 { a = startPos, b = midPos1a, c = midPos2a, d = endPos };
                colour = Color.gray;
                Singleton<RenderManager>.instance.OverlayEffect.DrawBezier(cameraInfo, colour, bezier.Flat(), preFab.m_halfWidth * 2.1f, 0, 0, -1f, 1280f, false, true);

            }
        }

        public enum NodeAction
        {
            AddNode = 1,
            RemoveNode = 2,
            InsertNode = 3,
            Info = 4,
            Default = 0
        }

        private bool CanBuild(ushort m_currentSegmentID, ushort m_currentNodeID)
        {
            if (m_currentNodeID == 0) return false;
            if (SimData.Nodes.Any(x => x.NodeID == m_currentNodeID)) return false;

            NetNode node = netManager.m_nodes.m_buffer[m_currentNodeID];
            NetNode.Flags flags = node.m_flags;
            if ((flags.IsFlagSet(NetNode.Flags.TrafficLights) | flags.IsFlagSet(NetNode.Flags.Junction)) && !flags.IsFlagSet(NetNode.Flags.LevelCrossing))
            {
                NetSegment segment = netManager.m_segments.m_buffer[m_currentSegmentID];
                NetInfo info = segment.Info;
                if ((info.m_connectGroup & NetInfo.ConnectGroup.SingleTrain) != 0 || (info.m_connectGroup & NetInfo.ConnectGroup.DoubleTrain) != 0)
                    return true;
                return false;
            }
            return false;
        }

        private IEnumerator CreateStraightNode()
        {
            ushort newSegment, newNode;
            int cost, productionRate;

            ToolErrors errors = NetTool.CreateNode(controlPoint.m_segment.GetSegment().Info, controlPoint, controlPoint, controlPoint, NetTool.m_nodePositionsSimulation, 0, true, false, true, false, false, false, 0, out newNode, out newSegment, out cost, out productionRate);
            if (errors != ToolErrors.None) { dbgError = errors; yield return null; }

            if (newNode == 0)
            {
                NetTool.CreateNode(controlPoint.m_segment.GetSegment().Info, controlPoint, controlPoint, controlPoint, NetTool.m_nodePositionsSimulation, 0, false, false, true, false, false, false, 0, out newNode, out newSegment, out cost, out productionRate);
            }
            dbgNode = newNode; dbgSegment = newSegment;

            if (newNode != 0)
            {
                selectedSegmentId = newSegment; selectedNodeId = newNode;
            }
            else
            {
                selectedSegmentId = newSegment; selectedNodeId = newNode;
            }

            if (selectedNodeId != 0 || selectedSegmentId != 0)
            {
                if (selectedNodeId == 0)
                {
                    NetSegment nSeg = netManager.m_segments.m_buffer[selectedSegmentId];
                    selectedNodeId = nSeg.m_endNode;
                }
                var netNode = netManager.m_nodes.m_buffer[selectedNodeId];
                NetNode.Flags flags = netNode.m_flags;
                flags &= ~NetNode.Flags.Middle;
                flags |= (NetNode.Flags.TrafficLights | NetNode.Flags.CustomTrafficLights | NetNode.Flags.Junction);
                netManager.m_nodes.m_buffer[selectedNodeId].m_flags = flags;

                List<ushort> segments = new List<ushort>();
                if (netNode.m_segment0 != 0) segments.Add(netNode.m_segment0);
                if (netNode.m_segment1 != 0) segments.Add(netNode.m_segment1);
                if (netNode.m_segment2 != 0) segments.Add(netNode.m_segment2);
                if (netNode.m_segment3 != 0) segments.Add(netNode.m_segment3);
                if (netNode.m_segment4 != 0) segments.Add(netNode.m_segment4);
                if (netNode.m_segment5 != 0) segments.Add(netNode.m_segment5);
                if (netNode.m_segment6 != 0) segments.Add(netNode.m_segment6);
                if (netNode.m_segment7 != 0) segments.Add(netNode.m_segment7);
                SimData.AddNode(selectedNodeId, segments);
                TrafficLights.SetTrafficLights(SimData.Nodes);
            }

            yield return null;
        }

        protected override void OnEnable()
        {
            m_toolController.ClearColliding();
            buildErrors = ToolErrors.Pending;
            cachedErrors = ToolErrors.Pending;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            KeyboardInput._nodeSel = false;
            if (KeyboardInput.buildTool != null) Destroy(KeyboardInput.buildTool);
            KeyboardInput.buildTool = null;
            ToolCursor = null;
            buildErrors = ToolErrors.Pending;
            cachedErrors = ToolErrors.Pending;
            mouseRayValid = false;
            base.OnDisable();
        }

        public static bool IsActiveTool => KeyboardInput.buildTool != null && (KeyboardInput._nodeSel || KeyboardInput._showBlocks);
        internal static void DisableTool()
        {
            KeyboardInput._nodeSel = false; KeyboardInput._showBlocks = false;
            if (KeyboardInput.buildTool != null) { Destroy(KeyboardInput.buildTool); KeyboardInput.buildTool = null; ToolsModifierControl.SetTool<DefaultTool>(); }
        }

    }
}
