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
                string txt = "=== DATA LOADED ===";string NL = Environment.NewLine;
                txt +=NL+ "Blocks: " + SimData.Blocks.Count + ", Nodes: " + SimData.Nodes.Count + ", Trains: " + SimData.Trains.Count;
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, txt);
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.T))
            {
                if (_processed) return; _processed = true;

                TrainData.ShowTrains(15305);
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
