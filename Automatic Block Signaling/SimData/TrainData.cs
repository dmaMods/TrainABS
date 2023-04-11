using ColossalFramework;
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
            SimData.Updating = true;
            // CLEAR BLOCKS
            SimData.Blocks.All(c => { c.BlockedBy = 0; return true; });

            // CURRENT POSITION
            foreach (var train in SimData.Trains)
            {
                train.CSegment = PathFinder.GetCurrentPosition(train, train.TrainID.FrontCar(), out PathUnit.Position trainCPos);
                train.CBlock = GetCurrentBlock(train, trainCPos); train.Position = trainCPos;
                SimData.GreenLights.Remove(train.CBlock);
                if (train.CBlock != 0) SimData.UpdateBlock(train.CBlock, train.TrainID);
            }

            // SET NEXT BLOCK
            foreach (var train in SimData.Trains)
            {
                train.NSegment = PathFinder.GetNextPosition(vehicles[train.TrainID.FrontCar()], out PathUnit.Position trainNPos);
                train.NBlock = GetNextBlock(train, train.CSegment, trainNPos);
            }
            SimData.Updating = false;
        }

        private static ushort GetNextBlock(STrains train, List<ushort> cSegment, PathUnit.Position trainPos)
        {
            if (train.NSegment.Count == 0) return 0;

            var vFlags = vehicles[train.TrainID.FrontCar()].m_flags;
            var sFlags = trainPos.m_segment.ToSegment().m_flags;

            var psegments = BlockData.blockSegments.Where(x => x.SegmentId == trainPos.m_segment).Select(x => new { block = x.BlockId, lane = x.Lane, selected = x.Lane == trainPos.m_lane });

            if (psegments.Count() == 0) return 0;
            if (psegments.Count() == 1) return (ushort)psegments.FirstOrDefault()?.block;
            return (ushort)psegments.FirstOrDefault(x => x.selected)?.block;
        }

        private static ushort GetCurrentBlock(STrains train, PathUnit.Position trainPos)
        {
            if (train.CSegment.Count == 0) return 0;

            var vFlags = vehicles[train.TrainID.FrontCar()].m_flags;
            var sFlags = trainPos.m_segment.ToSegment().m_flags;

            var psegments = BlockData.blockSegments.Where(x => x.SegmentId == trainPos.m_segment).Select(x => new { block = x.BlockId, lane = x.Lane, selected = x.Lane == trainPos.m_lane });

            if (psegments.Count() == 0) return 0;
            if (psegments.Count() == 1) return (ushort)psegments.FirstOrDefault()?.block;
            return (ushort)psegments.FirstOrDefault(x => x.selected)?.block;
        }
        #endregion
    }
}
