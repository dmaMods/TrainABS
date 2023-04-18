using ColossalFramework;
using dmaTrainABS.GameData;
using System;
using System.Linq;
using UnityEngine;

namespace dmaTrainABS
{
    public class TrafficManager
    {
        private static Vehicle[] vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

        public static void UpdateTraffic(uint frameIndex)
        {
            if (SimData.Updating) return;
            SimData.Updating = true;
            try
            {
                if (SimData.UpdateRequired) BlockData.LoadNetwork();
                TrainData.LoadTrains();
                int Cnt = SimData.WaitingList.Count == 0 ? 1000 : SimData.WaitingList.Max(x => x.ProcessId) + 1;

                TrainData.ProcessTrains();
                foreach (var train in SimData.Trains)
                {
                    var frontCar = vehicles[train.TrainID.FrontCar()];
                    if (frontCar.m_flags2.IsFlagSet(Vehicle.Flags2.Yielding) && SimData.Nodes.Any(y => y.NodeID == train.NodeID) && train.NodeID != 0 && train.SignalNode == 0)
                    {
                        SimData.WaitingList.AddNew(new Declarations.SWaitingList
                        {
                            NodeId = train.NodeID,
                            ProcessId = ++Cnt,
                            TrainId = train.TrainID
                        });
                    }
                    else
                    {
                        if (train.SignalNode != 0)
                        {
                            var lastCar = vehicles[train.TrainID.LastCar()];
                            ushort nodeId = 0; ushort cSegment = PathFinder.TrainPosition(lastCar, ref nodeId, out PathUnit.Position position);
                            if (train.SignalNode == nodeId)
                            {
                                var node = SimData.Nodes.FirstOrDefault(x => x.NodeID == train.SignalNode);
                                if (node == null) continue;
                                var segment = node.Segments.FirstOrDefault(x => x.LockedBy == train.TrainID && x.GreenState);
                                if (segment == null) continue;
                                segment.LockedBy = 0;
                                segment.GreenState = false;
                                train.SignalNode = 0;
                                foreach (var cblock in train.CBlock)
                                    SimData.GreenLights.Remove(cblock);
                            }
                        }
                    }
                }

                var WList = SimData.WaitingList.Where(x => !x.Processed).OrderBy(c => c.NodeId).ThenBy(n => n.ProcessId);
                foreach (var wl in WList)
                {
                    var train = SimData.Trains.FirstOrDefault(x => x.TrainID == wl.TrainId);
                    if (train == null) { ClearProcessId(wl.ProcessId); continue; }
                    var frontCar = vehicles[train.TrainID.FrontCar()];
                    if (CanProceed(train, out bool blockIsFree))
                    {
                        if (train.CSegment.Count() != 0 && blockIsFree)
                        {
                            var node = SimData.Nodes.FirstOrDefault(x => x.NodeID == train.NodeID);
                            if (node == null) continue;
                            var segment = node.Segments.FirstOrDefault(x => x.LockedBy == 0 && x.SegmentID == train.CSegment.FirstOrDefault() && !x.GreenState);
                            if (segment == null) continue;
                            TrafficLights.ClearGreen(train);
                            segment.LockedBy = train.TrainID;
                            segment.GreenState = true;
                            ClearProcessId(wl.ProcessId);
                            train.SignalNode = train.NodeID;
                            SimData.GreenLights.AddNew(train.NBlock);
                            foreach (var cblock in train.CBlock)
                                SimData.UpdateBlock(cblock, train.TrainID);
                            SimData.UpdateBlock(train.NBlock, train.TrainID);
                        }
                    }
                }
                SimData.WaitingList.RemoveAll(x => x.Processed);
            }
            catch (Exception ex) { Debug.LogException(ex); }
            SimData.Updating = false;
        }

        private static void ClearProcessId(int processId)
        {
            foreach (var wl in SimData.WaitingList.Where(x => x.ProcessId == processId))
                wl.Processed = true;
        }

        internal static bool CanProceed(Declarations.STrains train, out bool blockIsFree)
        {
            if (train.NBlock == 0 && train.CSegment.OutOfArea()) { blockIsFree = true; return true; }
            if (train.NBlock == 0) { blockIsFree = true; return false; }
            if (train.NSegment[0].ToSegment().m_flags.IsFlagSet(NetSegment.Flags.End)) { blockIsFree = true; return true; }
            var block = SimData.Blocks[train.NBlock];
            blockIsFree = !SimData.GreenLights.Contains(train.NBlock) &&
                (!block.Blocked || train.NSegment.OutOfArea());
            return blockIsFree;
        }

        internal static void ClearTraffic()
        {
            for (ushort f = 0; f < vehicles.Length; f++)
            {
                var vehicle = vehicles[f];
                if (vehicle.m_flags.IsFlagSet(Vehicle.Flags.Created) && vehicle.Info != null && vehicle.Info.m_vehicleType == VehicleInfo.VehicleType.Train)
                {
                    Singleton<VehicleManager>.instance.ReleaseVehicle(f);
                }
            }

            foreach (var node in SimData.Nodes)
                foreach (var seg in node.Segments) { seg.LockedBy = 0; seg.GreenState = false; }

            SimData.GreenLights.Clear();

            TrafficLights.SetTrafficLights(SimData.Nodes);
        }

    }
}
