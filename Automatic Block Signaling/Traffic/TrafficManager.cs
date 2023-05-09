using ColossalFramework;
using dmaTrainABS.GameData;
using dmaTrainABS.Traffic;
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
            if (SimData.Updating) { SimData.ProcessS++; return; }
            if (!SimData.Nodes.IsValid()) return;

            SimData.Updating = true; SimData.ProcessD++;
            Helpers.ClearConfused(); SimData.CheckNodes();

            try
            {
                if (SimData.UpdateRequired) BlockData.LoadNetwork();
                TrainData.LoadTrains();
                int Cnt = SimData.WaitingList.Count == 0 ? 1000 : SimData.WaitingList.Max(x => x.ProcessId) + 1;

                TrainData.ProcessTrains();
                foreach (var train in SimData.Trains)
                {
                    var frontCar = vehicles[train.Key.FrontCar()];
                    if (frontCar.m_flags2.IsFlagSet(Vehicle.Flags2.Yielding) && SimData.Nodes.Any(x => x.NodeID == train.Value.NodeID) &&
                        train.Value.NodeID != 0 && train.Value.SignalBlock == 0)
                    {
                        SimData.WaitingList.AddNew(new Declarations.SWaitingList
                        {
                            NodeId = train.Value.NodeID,
                            ProcessId = ++Cnt,
                            TrainId = train.Key,
                            BlockId = train.Value.NBlock,
                            frameIndex = frameIndex
                        });
                    }
                }

                var WList = SimData.WaitingList.Where(x => !x.Processed).OrderBy(c => c.frameIndex).ThenBy(f => f.ProcessId).ThenBy(n => n.NodeId);
                foreach (var wl in WList)
                {
                    if (!SimData.Trains.ContainsKey(wl.TrainId)) { ClearProcessId(wl.ProcessId); continue; }
                    var train = SimData.Trains[wl.TrainId];
                    var frontCar = vehicles[wl.TrainId.FrontCar()];
                    if (CanProceed(train, out bool blockIsFree))
                    {
                        if (train.CSegment.Count() != 0 && blockIsFree)
                        {
                            var node = SimData.Nodes.FirstOrDefault(x => x.NodeID == train.NodeID);
                            if (node == null) { ClearProcessId(wl.ProcessId); continue; };
                            var segment = node.Segments.FirstOrDefault(x => x.LockedBy == 0 && x.SegmentID == train.CSegment.FirstOrDefault() && !x.GreenState);
                            if (segment == null) { ClearProcessId(wl.ProcessId); continue; };
                            segment.LockedBy = wl.TrainId;
                            segment.GreenState = true;
                            ClearProcessId(wl.ProcessId);
                            train.SignalNode = train.NodeID;
                            train.SignalBlock = train.NBlock;
                            train.GreenLight = true;
                            SimData.GreenLights.AddNew(train.NBlock, wl.TrainId);
                            foreach (var cblock in train.CBlock)
                                SimData.UpdateBlock(cblock, wl.TrainId);
                            SimData.UpdateBlock(train.NBlock, wl.TrainId);
                        }
                    }
                }
                SimData.WaitingList.RemoveAll(x => x.Processed);

                foreach (var train in SimData.Trains)
                {
                    var frontCar = vehicles[train.Key.FrontCar()];
                    if (frontCar.m_flags2.IsFlagSet(Vehicle.Flags2.Yielding)) continue;
                    var lastCar = vehicles[train.Key.LastCar()];
                    if (train.Value.CBlock.Contains(train.Value.SignalBlock))
                    {
                        var node = SimData.Nodes.FirstOrDefault(x => x.NodeID == train.Value.SignalNode);
                        if (node == null) continue;
                        var segment = node.Segments.FirstOrDefault(x => x.LockedBy == train.Key && x.GreenState);
                        if (segment == null) continue;
                        segment.LockedBy = 0;
                        segment.GreenState = false;
                        train.Value.SignalNode = 0;
                        train.Value.SignalBlock = 0;
                        train.Value.GreenLight = false;
                        foreach (var cblock in train.Value.CBlock)
                            SimData.GreenLights.Remove(cblock);
                    }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); }
            SimData.Updating = false;
        }

        private static void ClearProcessId(int processId)
        {
            var wl = SimData.WaitingList.FirstOrDefault(x => x.ProcessId == processId);
            if (wl != null) wl.Processed = true;
        }

        internal static bool CanProceed(Declarations.STrains train, out bool blockIsFree)
        {
            if (train.NBlock == 0 && train.CSegment.OutOfArea()) { blockIsFree = true; return true; }
            if (train.NBlock == 0) { blockIsFree = true; return false; }
            if (train.NSegment[0].ToSegment().m_flags.IsFlagSet(NetSegment.Flags.End)) { blockIsFree = true; return true; }
            var block = SimData.Blocks[train.NBlock];
            blockIsFree = !SimData.GreenLights.Any(x => x.BlockId == train.NBlock) &&
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
