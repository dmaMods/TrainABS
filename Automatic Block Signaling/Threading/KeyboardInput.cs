using UnityEngine;

namespace dmaTrainABS
{
    public class KeyboardInput
    {
        private static bool _processed = false;
        public static bool _nodeSel = false;
        public static NodeSelector buildTool = null;

        public static void CheckInput()
        {
            if (TrainABSModData.AllGreenLights.IsPressed())
            {
                if (_processed) return; _processed = true;

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = true; }

                TrafficLights.SetTrafficLights(SimData.Nodes);
                DOP.Show("All nodes GREEN!");
            }

            else if (TrainABSModData.AllRedLights.IsPressed())
            {
                if (_processed) return; _processed = true;

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }

                SimData.GreenLights.Clear();

                TrafficLights.SetTrafficLights(SimData.Nodes);
                DOP.Show("All nodes RED!");
            }

            else if (TrainABSModData.NetReload.IsPressed())
            {
                SimData.CheckNodes();
                BlockData.LoadNetwork();
                TrainData.LoadTrains();

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }
                TrafficLights.SetTrafficLights(SimData.Nodes);

                DOP.ShowInfo(true);
            }

            else if (TrainABSModData.ModShortcut.IsPressed())
            {
                if (_processed) return; _processed = true;

                if (_nodeSel)
                {
                    _nodeSel = false;
                    DOP.Show("Node Tool Deactivated");
                    if (buildTool != null) { NodeSelector.Destroy(buildTool); buildTool = null; ToolsModifierControl.SetTool<DefaultTool>(); }
                }
                else
                {
                    _nodeSel = true;
                    DOP.Show("Node Tool Activated");
                    buildTool = ToolsModifierControl.toolController.gameObject.GetComponent<NodeSelector>();
                    if (buildTool == null) { buildTool = ToolsModifierControl.toolController.gameObject.AddComponent<NodeSelector>(); }
                }
            }

            //else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && 
            //    Input.GetKey(KeyCode.P))
            //{
            //    if (_processed) return; _processed = true;

            //    //PrefabChanges.UpdateNode(28737);
            //    //PrefabChanges.UpdateTrucks();

            //}

            //else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && 
            //    Input.GetKey(KeyCode.C))
            //{
            //    if (_processed) return; _processed = true;

            //    //SimData.InitData();
            //    //DOP.Show("All data CLEARED!");

            //    //TrafficManager.ClearTraffic();
            //    //TrafficManager.UpdateTraffic(0, true);
            //    //DOP.Show("Trains CLEARED!");
            //}
#if DEBUG
            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                Input.GetKey(KeyCode.S))
            {
                if (_processed) return; _processed = true;

                DOP.ShowInfo(true);
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                Input.GetKey(KeyCode.T))
            {
                if (_processed) return; _processed = true;

                TrainData.ShowTrains(true);
                TrafficManager.UpdateTraffic(0, true);
                TrafficLights.SetTrafficLights(SimData.Nodes);
            }
#endif
            //else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.U))
            //{
            //    if (_processed) return; _processed = true;

            //    //TrafficManager.ClearTraffic();
            //    TrafficManager.UpdateTraffic(0, true);
            //}
#if DEBUG
            else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)) && Input.GetKey(KeyCode.L))
            {
                if (_processed) return; _processed = true;

                SimData.InitData();
                DataManager.Load();
                SimData.CheckNodes();
                if (!SimData.Blocks.IsValid()) BlockData.LoadNetwork(debugMode: true);
                if (!SimData.Trains.IsValid()) TrainData.LoadTrains();

                foreach (var node in SimData.Nodes)
                    foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }
                TrafficLights.SetTrafficLights(SimData.Nodes);
                DOP.ShowInfo(true);
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                Input.GetKey(KeyCode.B))
            {
                if (_processed) return; _processed = true;

                BlockData.LoadNetwork(true);
                DOP.ShowInfo(true);
                BlockData.ShowBlocks(true);
            }

            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                Input.GetKey(KeyCode.D))
            {
                if (_processed) return; _processed = true;

                DOP.ShowDebug();
            }
#endif
            else
            {
                _processed = false;
            }
        }

    }
}
