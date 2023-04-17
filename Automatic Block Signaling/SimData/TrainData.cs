using ColossalFramework;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using static dmaTrainABS.GameData.Declarations;

namespace dmaTrainABS
{
    public class TrainData
    {
        private static Vehicle[] vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

        #region LOAD TRAINS
        public static void LoadTrains()
        {
            if (!SimData.Trains.IsValid()) SimData.Trains = new List<STrains>();

            for (ushort v = 1; v < vehicles.Length; v++)
            {
                Vehicle vehicle = vehicles[v];
                if (vehicle.m_flags.IsFlagSet(Vehicle.Flags.Created) && vehicle.Info != null && !vehicle.m_flags.IsFlagSet(Vehicle.Flags.WaitingSpace))
                {
                    if (vehicle.Info.m_vehicleType == VehicleInfo.VehicleType.Train)
                    {
                        bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
                        bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
                        ushort frontVehicleId = isMain && !reversed ? v : vehicle.GetLastVehicle(v);

                        if (!SimData.Trains.Any(x => x.TrainID == v) && isMain)
                        {
                            var train = new STrains
                            {
                                TrainID = v,
                                Trailers = GetTrailers(v),
                                SignalNode = 0
                            };
                            train.FirstVehicle = vehicle.GetFirstVehicle(v);
                            train.LastVehicle = vehicle.GetLastVehicle(v);
                            SimData.Trains.Add(train);
                        }
                        else
                        {
                            if (isMain)
                            {
                                var train = SimData.Trains.FirstOrDefault(x => x.TrainID == v);
                                if (train == null) continue;
                                train.FirstVehicle = vehicle.GetFirstVehicle(v);
                                train.LastVehicle = vehicle.GetLastVehicle(v);
                            }
                        }
                    }
                }
            }
            CheckTrains();
        }

        private static List<ushort> GetTrailers(ushort cTrain)
        {
            List<ushort> trailers = new List<ushort>();
            Vehicle[] Buffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

            while (cTrain != 0)
            {
                Vehicle vehicle = Buffer[cTrain];
                cTrain = vehicle.m_trailingVehicle;
                if (cTrain != 0) trailers.Add(cTrain);
            }
            return trailers;
        }

        public static void CheckTrains()
        {
            foreach (var train in SimData.Trains)
            {
                Vehicle vehicle = vehicles[train.TrainID];
                if (vehicle.m_flags.IsFlagSet(Vehicle.Flags.Created) && vehicle.Info != null && !vehicle.m_flags.IsFlagSet(Vehicle.Flags.WaitingSpace))
                { /* valid train... do nothing */ }
                else
                {
                    foreach (var node in SimData.Nodes)
                    {
                        node.Segments.Where(x => x.LockedBy == train.TrainID && x.GreenState).All(c => { c.LockedBy = 0; c.GreenState = false; return true; });
                    }
                    SimData.WaitingList.RemoveAll(x => x.TrainId == train.TrainID);
                    train.TrainID = 0;
                }
            }
            SimData.Trains.RemoveAll(x => x.TrainID == 0);
        }
        #endregion

        #region PROCESS TRAINS
        public static void ProcessTrains()
        {
            try
            {
                SimData.Updating = true;

                // CURRENT POSITION
                foreach (var train in SimData.Trains)
                {
                    List<PathUnit.Position> positions = PathFinder.GetCurrentPosition(train, train.TrainID.FrontCar());
                    if (positions.Count() == 0) continue;
                    train.CBlock = GetCurrentBlocks(train, positions); train.Position = positions[0];
                    foreach (var cblock in train.CBlock)
                    {
                        SimData.GreenLights.Remove(cblock);
                        SimData.UpdateBlock(cblock, train.TrainID);
                    }
                }

                // SET NEXT BLOCK
                foreach (var train in SimData.Trains)
                {
                    train.NSegment = PathFinder.GetNextPosition(vehicles[train.TrainID.FrontCar()], out PathUnit.Position trainNPos);
                    train.NBlock = GetNextBlock(train, trainNPos);
                }

                // SET FREE BLOCKS
                List<ushort> OccupiedBlocks = new List<ushort>();
                foreach (var train in SimData.Trains)
                    foreach (var block in train.CBlock)
                        OccupiedBlocks.AddNew(block);
                SimData.Blocks.Where(x => !OccupiedBlocks.Contains(x.Key)).All(c => { c.Value.BlockedBy = 0; return true; });

                SimData.Updating = false;
            }
            catch (Exception ex) { DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ex.Message + Environment.NewLine + ex.StackTrace); }
        }

        private static ushort GetNextBlock(STrains train, PathUnit.Position trainPos)
        {
            try
            {
                return (ushort)BlockData.blockSegments.FirstOrDefault(x => x.SegmentId == trainPos.m_segment && x.Lane == trainPos.m_lane)?.BlockId;
            }
            catch { return 0; }
        }

        private static List<ushort> GetCurrentBlocks(STrains train, List<PathUnit.Position> trainPositions)
        {
            List<ushort> ret = new List<ushort>();

            foreach (var trainPos in trainPositions)
                ret.AddRange(BlockData.blockSegments.Where(x => x.SegmentId == trainPos.m_segment && x.Lane == trainPos.m_lane).Select(x => x.BlockId).ToList());

            return ret.Distinct().ToList();
        }
        #endregion

        #region DEBUG / SHOW TRAINS
        public static void ShowTrains(ushort trainId)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "=== TRAIN (" + trainId + ") DEBUG ===");

            CheckTrains(); DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "CheckTrains... Pass");
            ProcessTrains(); DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "ProcessTrains... Pass");

            string NL = Environment.NewLine; string txt = "";
            try
            {
                foreach (var train in SimData.Trains.Where(x => x.TrainID == trainId))
                {
                    var vehicle = vehicles[train.TrainID]; bool blockIsFree = false;
                    bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
                    bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
                    ushort frontVehicleId = isMain && !reversed ? train.TrainID : vehicle.GetLastVehicle(train.TrainID);
                    var nBlock = SimData.Blocks.ContainsKey(train.NBlock) ? SimData.Blocks[train.NBlock] : new SRailBlocks();
                    var segment = train.CSegment.FirstOrDefault().ToSegment();

                    txt += "Train #" + train.TrainID + (frontVehicleId != train.TrainID ? ", Front: " + frontVehicleId : "") +
                             (TrafficManager.CanProceed(train, out bool FreeBlock) ? " (CP)" : "") +
                             ", Lane: " + train.Position.m_lane + NodeSelector.LaneColor(train.Position.m_lane) + ", Segment (" + train.CSegment.Count + "): " + train.CSegment.FirstOrDefault() +
                             ", Next (" + train.NSegment.Count + "): " + train.NSegment.FirstOrDefault() + ", Node: " + train.NodeID + NL +
                                "Signal Node: " + train.SignalNode + ", Green Light: " + (SimData.GreenLights.Contains(train.NBlock) ? "Yes" : "No") +
                                ", Yield Test: " + (frontVehicleId.ToVehicle().m_flags2.IsFlagSet(Vehicle.Flags2.Yielding) && SimData.Nodes.Any(y => y.NodeID == train.NodeID) && train.NodeID != 0 && TrafficManager.CanProceed(train, out blockIsFree) ? "Pass" : "Failed") +
                                ", Segment Test: " + (train.CSegment.Count() != 0 && blockIsFree ? "Pass" : "Failed (" + train.CSegment.Count() + ") " + blockIsFree) +
                                ", Node " + train.NodeID + " Test: " + (SimData.Nodes.FirstOrDefault(x => x.NodeID == train.NodeID) != null ? "Pass" : "Failed") + NL +
                                "Vehicle Flags: " + frontVehicleId.ToVehicle().m_flags + (frontVehicleId.ToVehicle().m_flags2.IsFlagSet(Vehicle.Flags2.Yielding) ? ", Yielding" : "") + NL +
                                "Current Blocks: " + string.Join(", ", train.CBlock.Select(x => x.ToString()).ToArray()) + NL +
                                "Next Block: " + (train.NBlock != 0 ? train.NBlock + ", Node: " + nBlock.StartNode + ", " + (nBlock.Blocked ? "Blocked By: " + nBlock.BlockedBy : "Free") : "None");
                }
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, txt);
            }
            catch (Exception ex) { DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ex.Message + NL + ex.StackTrace); }
        }
        #endregion
    }
}
