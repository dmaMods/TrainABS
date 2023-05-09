using ColossalFramework;
using ColossalFramework.Plugins;
using static dmaTrainABS.GameData.Declarations;
using UnityEngine;
using dmaTrainABS.Traffic;

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
                if (IsDebugMode)
                {
                    string txt = "[TrainABS] Data loaded. Nodes: " + SimData.Nodes.Count;
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, txt);
                }
                SimData.CheckNodes();

                BlockData.LoadNetwork();
                TrainData.LoadTrains();

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }

                var currentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                SimData.Updating = false; TrafficManager.UpdateTraffic(currentFrame);
                TrafficLights.SetTrafficLights(SimData.Nodes);
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.T))
            {
                if (_processed) return; _processed = true;

                TrainData.ShowTrains();
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.D))
            {
                if (_processed) return; _processed = true;

                SimData.ShowStats();
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[TrainABS] Clear " + Helpers.ClearConfused() + " confused cars.");
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.K))
            {
                if (_processed) return; _processed = true;

                SimData.GreenLights.Clear();
                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[TrainABS] Green list cleared.");
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
