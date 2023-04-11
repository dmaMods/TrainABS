using ColossalFramework;
using ColossalFramework.Plugins;
using System;
using UnityEngine;

namespace dmaTrainABS
{
    public class KeyboardInput
    {
        private static bool _processed = false;
        public static bool _nodeSel = false;
        public static bool _showBlocks = false;
        public static NodeSelector buildTool = null;

        public static void CheckInput()
        {
            if (TrainABSModData.AllGreenLights.IsPressed())
            {
                if (_processed) return; _processed = true;

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = true; }

                TrafficLights.SetTrafficLights(SimData.Nodes);
            }

            else if (TrainABSModData.AllRedLights.IsPressed())
            {
                if (_processed) return; _processed = true;

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }

                SimData.GreenLights.Clear();

                TrafficLights.SetTrafficLights(SimData.Nodes);
            }

            else if (TrainABSModData.NetReload.IsPressed())
            {
                SimData.Updating = true; SimData.InitData(false);

                SimData.CheckNodes();

                BlockData.LoadNetwork();
                TrainData.LoadTrains();

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }

                var currentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                SimData.Updating = false; TrafficManager.UpdateTraffic(currentFrame);
                TrafficLights.SetTrafficLights(SimData.Nodes);
            }

            else if (TrainABSModData.ModShortcut.IsPressed())
            {
                if (_processed) return; _processed = true;

                if (_nodeSel)
                {
                    _nodeSel = false; _showBlocks = false;
                    if (buildTool != null) { NodeSelector.Destroy(buildTool); buildTool = null; ToolsModifierControl.SetTool<DefaultTool>(); }
                }
                else
                {
                    _nodeSel = true; _showBlocks = false;
                    buildTool = ToolsModifierControl.toolController.gameObject.GetComponent<NodeSelector>();
                    if (buildTool == null) { buildTool = ToolsModifierControl.toolController.gameObject.AddComponent<NodeSelector>(); }
                }
            }
#if DEBUG
            else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)) && Input.GetKey(KeyCode.L))
            {
                if (_processed) return; _processed = true;

                SimData.Updating = true; SimData.InitData();
                DataManager.Load();
                SimData.CheckNodes();

                BlockData.LoadNetwork();
                TrainData.LoadTrains();

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }

                var currentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                SimData.Updating = false; TrafficManager.UpdateTraffic(currentFrame);
                TrafficLights.SetTrafficLights(SimData.Nodes);
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.D))
            {
                if (_processed) return; _processed = true;

                string nl = Environment.NewLine; string txt = "=== PATH TEST ===" + nl;
                var pathManager = Singleton<PathManager>.instance;
                var node1 = NetManager.instance.m_nodes.m_buffer[30799];
                var node2 = NetManager.instance.m_nodes.m_buffer[5356];
                var startPos = new PathUnit.Position { m_segment = 19101 };
                var endPos = new PathUnit.Position { m_segment = 16513 };
                if (pathManager.CreatePath(out uint unit, ref SimulationManager.instance.m_randomizer, 
                    SimulationManager.instance.m_currentBuildIndex, startPos, endPos, NetInfo.LaneType.Vehicle, VehicleInfo.VehicleType.Train, VehicleInfo.VehicleCategory.All, 25))
                {
                    txt += "Path Unit: " + unit + nl;
                    var pathUnit = pathManager.m_pathUnits.m_buffer[unit];
                    for (int f = 0; f < pathUnit.m_positionCount; f++)
                    {
                        var pathPos1 = pathUnit.GetPosition(f);
                        txt +="Pos: "+f.ToString("00")+ ", Segment: " + pathPos1.m_segment + ", Lane: " + pathPos1.m_lane + ", Offset: " + pathPos1.m_offset+nl;
                    }
                }
                else
                {
                    txt += "Unable to create path.";
                }
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, txt);
            }
#endif
            else if (TrainABSModData.ShowBlocks.IsPressed())
            {
                if (_processed) return; _processed = true;
#if DEBUG
                BlockData.ShowBlocks();
#endif
                if (_showBlocks)
                {
                    _showBlocks = false; _nodeSel = false;
                    if (buildTool != null) { NodeSelector.Destroy(buildTool); buildTool = null; ToolsModifierControl.SetTool<DefaultTool>(); }
                }
                else
                {
                    _showBlocks = true; _nodeSel = false;
                    buildTool = ToolsModifierControl.toolController.gameObject.GetComponent<NodeSelector>();
                    if (buildTool == null) { buildTool = ToolsModifierControl.toolController.gameObject.AddComponent<NodeSelector>(); }
                }
            }

            else
            {
                _processed = false;
            }
        }

    }
}
