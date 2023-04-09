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
                {
                    // valid train... do nothing
                }
                else
                {
                    foreach (var node in SimData.Nodes)
                    {
                        node.Segments.Where(x => x.LockedBy == train.TrainID && x.GreenState).All(c => { c.LockedBy = 0; c.GreenState = false; return true; });
                    }
                    train.TrainID = 0;
                }
            }
            SimData.Trains.RemoveAll(x => x.TrainID == 0);
        }
        #endregion

        #region PROCESS TRAINS
        public static void ProcessTrains(bool debugMode = false)
        {
            // CLEAR BLOCKS
            SimData.Blocks.All(c => { c.BlockedBy = 0; return true; });

            // CURRENT POSITION
            foreach (var train in SimData.Trains)
            {
                //debugMode = (train.TrainID == 6000 || train.TrainID == 11158);

                //var vehicle = vehicles[train.TrainID];
                //bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
                //bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
                //ushort frontVehicleId = isMain && !reversed ? train.TrainID : vehicle.GetLastVehicle(train.TrainID);
                //ushort lastVehicleId = isMain && !reversed ? vehicle.GetLastVehicle(train.TrainID) : train.TrainID;
                //Vehicle frontCar = vehicles[frontVehicleId]; Vehicle lastCar = vehicles[lastVehicleId];

                train.CSegment = PathFinder.GetCurrentPosition(train, train.TrainID.FrontCar(), out PathUnit.Position trainCPos);

                train.CBlock = GetCurrentBlock(train, trainCPos, debugMode); train.Position = trainCPos;
                SimData.GreenLights.Remove(train.CBlock);
                if (train.CBlock != 0) SimData.UpdateBlock(train.CBlock, train.TrainID);
            }

            // SET NEXT BLOCK
            foreach (var train in SimData.Trains)
            {
                //debugMode = (train.TrainID == 6000 || train.TrainID == 11158);

                //var vehicle = vehicles[train.TrainID];
                //bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
                //bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
                //ushort frontVehicleId = isMain && !reversed ? train.TrainID : vehicle.GetLastVehicle(train.TrainID);
                //ushort lastVehicleId = isMain && !reversed ? vehicle.GetLastVehicle(train.TrainID) : train.TrainID;
                //Vehicle frontCar = vehicles[frontVehicleId]; Vehicle lastCar = vehicles[lastVehicleId];

                train.NSegment = PathFinder.GetNextPosition(vehicles[train.TrainID.FrontCar()], out PathUnit.Position trainNPos);

                train.NBlock = GetNextBlock(train, train.CSegment, trainNPos, debugMode);
            }
        }

        private static ushort GetNextBlock(STrains train, List<ushort> cSegment, PathUnit.Position trainPos, bool debugMode)
        {
            //ushort retBlockId = 0;
            if (train.NSegment.Count == 0) return 0;

            //var vehicle = vehicles[train.TrainID];
            //bool invertLane = false;
            //if (cSegment.Count() != 0)
            //{
            //    if (cSegment[0].ToSegment().m_flags.IsFlagSet(NetSegment.Flags.End))
            //    {
            //        var nSeg = train.NSegment[0].ToSegment();
            //        var sNode = nSeg.m_startNode;var eNode = nSeg.m_endNode;
            //        var block = SimData.Blocks.Where(x => x.StartNode == sNode || x.StartNode==eNode);
            //        if (block != null) return (ushort)block.FirstOrDefault()?.BlockId;
            //    }
            //}
            //bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
            //bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
            //ushort frontVehicleId = isMain && !reversed ? train.TrainID : vehicle.GetLastVehicle(train.TrainID);
            //ushort lastVehicleId = isMain && !reversed ? vehicle.GetLastVehicle(train.TrainID) : train.TrainID;
            //Vehicle frontCar = vehicles[frontVehicleId]; Vehicle lastCar = vehicles[lastVehicleId];

            string dirFlags = ""; string laneColor = trainPos.m_lane == 0 ? "RED" : "YELLOW";// trainPos.m_segment.ToSegment().m_startDirection.ToString();
            var vFlags = vehicles[train.TrainID.FrontCar()].m_flags;
            var sFlags = trainPos.m_segment.ToSegment().m_flags;

            dirFlags = Convert.ToBoolean(trainPos.m_lane) + "-" + vFlags.IsFlagSet(Vehicle.Flags.Inverted) + "-" + sFlags.IsFlagSet(NetSegment.Flags.Invert);
            string txt = "";
            var psegments = BlockData.blockSegments.Where(x => x.SegmentId == trainPos.m_segment)
                .Select(x => new { block = x.BlockId, lane = x.Lane, selected = x.Lane == trainPos.m_lane });
            ///* block = x.BlockId, selected = x.Inverted == sFlags.IsFlagSet(NetSegment.Flags.Invert) ^ vFlags.IsFlagSet(Vehicle.Flags.Inverted) });// && x.Inverted==sFlags.IsFlagSet(NetSegment.Flags.Invert)^vFlags.IsFlagSet(Vehicle.Flags.Inverted)*/
            foreach (var segment in psegments)
                txt += "Block: " + segment.block + (segment.selected ? " True" : "") + ", Lane: " + segment.lane + Environment.NewLine;

            if (debugMode)
                DOP.Show("NEXT POSITION --- " + laneColor + Environment.NewLine + "Train #" + train.TrainID + (vFlags.IsFlagSet(Vehicle.Flags.Inverted) ? " Inverted" : "") + (vFlags.IsFlagSet(Vehicle.Flags.Reversed) ? " Reversed" : "")
                    + ", Lane: " + trainPos.m_lane + " > " + dirFlags +
                    ", Segment: " + trainPos.m_segment + (sFlags.IsFlagSet(NetSegment.Flags.Invert) ? " Inverted" : "") +
                    //", Direction: S" + trainPos.m_segment.ToSegment().m_startDirection + " - E" + trainPos.m_segment.ToSegment().m_endDirection +
                    Environment.NewLine + txt);

            if (psegments.Count() == 0) return 0;
            if (psegments.Count() == 1) return (ushort)psegments.FirstOrDefault()?.block;
            return (ushort)psegments.FirstOrDefault(x => x.selected)?.block;
        }

        //private static byte CheckLane(byte mLane, bool vInverted)
        //{
        //    //if (vInverted) return mLane != 0 ? (byte)0 : (byte)1;
        //    return mLane;
        //}

        private static ushort GetCurrentBlock(STrains train, PathUnit.Position trainPos, bool debugMode)
        {
            //ushort retBlockId = 0;
            if (train.CSegment.Count == 0) return 0;

            //var vehicle = vehicles[train.TrainID];
            //bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
            //bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
            //ushort frontVehicleId = isMain && !reversed ? train.TrainID : vehicle.GetLastVehicle(train.TrainID);
            //ushort lastVehicleId = isMain && !reversed ? vehicle.GetLastVehicle(train.TrainID) : train.TrainID;
            //Vehicle frontCar = vehicles[train.TrainID.FrontCar()]; Vehicle lastCar = vehicles[train.TrainID.LastCar()];

            string dirFlags = ""; string laneColor = trainPos.m_lane == 0 ? "RED" : "YELLOW";//= trainPos.m_segment.ToSegment().m_startDirection.ToString();
            var vFlags = vehicles[train.TrainID.FrontCar()].m_flags;
            var sFlags = trainPos.m_segment.ToSegment().m_flags;

            dirFlags = Convert.ToBoolean(trainPos.m_lane) + "-" + vFlags.IsFlagSet(Vehicle.Flags.Inverted) + "-" + sFlags.IsFlagSet(NetSegment.Flags.Invert);
            string txt = "";
            var psegments = BlockData.blockSegments.Where(x => x.SegmentId == trainPos.m_segment)
                .Select(x => new { block = x.BlockId, lane = x.Lane, selected = x.Lane == trainPos.m_lane });
            // /*x.Inverted == sFlags.IsFlagSet(NetSegment.Flags.Invert)*/ });// /*&& x.Inverted==sFlags.IsFlagSet(NetSegment.Flags.Invert)^vFlags.IsFlagSet(Vehicle.Flags.Inverted)*/);
            foreach (var segment in psegments)
                txt += "Block: " + segment.block + (segment.selected ? " True" : "") + ", Lane: " + segment.lane + Environment.NewLine;

            if (debugMode)
                DOP.Show("CURRENT POSITION --- " + laneColor + Environment.NewLine + "Train #" + train.TrainID + (vFlags.IsFlagSet(Vehicle.Flags.Inverted) ? " Inverted" : "") + (vFlags.IsFlagSet(Vehicle.Flags.Reversed) ? " Reversed" : "")
                    + ", Lane: " + trainPos.m_lane + " > " + dirFlags +
                    ", Segment: " + trainPos.m_segment + (sFlags.IsFlagSet(NetSegment.Flags.Invert) ? " Inverted" : "") +
                    //", Direction: S" + trainPos.m_segment.ToSegment().m_startDirection + " - E" + trainPos.m_segment.ToSegment().m_endDirection +
                    Environment.NewLine + txt);

            if (psegments.Count() == 0) return 0;
            if (psegments.Count() == 1) return (ushort)psegments.FirstOrDefault()?.block;
            return (ushort)psegments.FirstOrDefault(x => x.selected)?.block;
        }
        #endregion

        #region DEBUG / SHOW TRAINS
        public static void ShowTrains(bool debugMode = false)
        {
            ProcessTrains(debugMode);
            string NL = Environment.NewLine;
            DOP.Show("=== TRAINS ===");

            foreach (var train in SimData.Trains)
            {
                var vehicle = vehicles[train.TrainID]; bool blockIsFree = false;
                bool reversed = (vehicle.m_flags & Vehicle.Flags.Reversed) != 0;
                bool isMain = (vehicle.m_flags & Vehicle.Flags.TransferToTarget) != 0 || (vehicle.m_flags & Vehicle.Flags.TransferToSource) != 0;
                ushort frontVehicleId = isMain && !reversed ? train.TrainID : vehicle.GetLastVehicle(train.TrainID);
                SRailBlocks cBlock = SimData.Blocks.FirstOrDefault(x => x.BlockId == train.CBlock) ?? new SRailBlocks { BlockId = 0, BlockedBy = 0, StartNode = 0, EndNode = 0 };
                SRailBlocks nBlock = SimData.Blocks.FirstOrDefault(x => x.BlockId == train.NBlock) ?? new SRailBlocks { BlockId = 0, BlockedBy = 0, StartNode = 0, EndNode = 0 };
                var segment = train.CSegment.FirstOrDefault().ToSegment();

                DOP.Show("Train #" + train.TrainID + (frontVehicleId != train.TrainID ? ", Front: " + frontVehicleId : "") +
                         (TrafficManager.CanProceed(train, out bool FreeBlock) ? " (CP)" : "") +
                         ", Lane: " + train.Position.m_lane + NodeSelector.LaneColor(train.Position.m_lane) + ", Segment (" + train.CSegment.Count + "): " + train.CSegment.FirstOrDefault() +
                         ", Next (" + train.NSegment.Count + "): " + train.NSegment.FirstOrDefault() + ", Node: " + train.NodeID + NL +
                            "Signal Node: " + train.SignalNode + ", Green Light: " + (SimData.GreenLights.Contains(train.NBlock) ? "Yes" : "No") +
                            ", Yield Test: " + (frontVehicleId.ToVehicle().m_flags2.IsFlagSet(Vehicle.Flags2.Yielding) && SimData.Nodes.Any(y => y.NodeID == train.NodeID) && train.NodeID != 0 && TrafficManager.CanProceed(train, out blockIsFree) ? "Pass" : "Failed") +
                            ", Segment Test: " + (train.CSegment.Count() != 0 && blockIsFree ? "Pass" : "Failed (" + train.CSegment.Count() + ") " + blockIsFree) +
                            ", Node " + train.NodeID + " Test: " + (SimData.Nodes.FirstOrDefault(x => x.NodeID == train.NodeID) != null ? "Pass" : "Failed") + NL +
                            "Vehicle Flags: " + frontVehicleId.ToVehicle().m_flags + (frontVehicleId.ToVehicle().m_flags2.IsFlagSet(Vehicle.Flags2.Yielding) ? ", Yielding" : "") + NL +
                            "Current Block: " + train.CBlock + ", Node: " + cBlock.StartNode + ", " + (cBlock.Blocked ? "Blocked By: " + cBlock.BlockedBy : "Free") + NL +
                            "Next Block: " + nBlock.BlockId + ", Node: " + nBlock.StartNode + ", " + (nBlock.Blocked ? "Blocked By: " + nBlock.BlockedBy : "Free"));
            }
        }
        #endregion
    }
}
