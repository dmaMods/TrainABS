using ColossalFramework;
using ColossalFramework.Plugins;
using dmaTrainABS.GameData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dmaTrainABS
{
    public class PathFinder
    {
        private static readonly PathManager pathManager = Singleton<PathManager>.instance;

        public static List<PathUnit.Position> GetCurrentPosition(ushort trainId, ushort frontVehicleId)
        {
            var train = SimData.Trains[trainId];
            try
            {
                List<PathUnit.Position> positions = new List<PathUnit.Position>(); ushort nodeId = 0; ushort dummy = 0;
                train.CSegment = new List<ushort>();
                train.CSegment.Add(TrainPosition(frontVehicleId.ToVehicle(), ref nodeId, out PathUnit.Position fPosition));
                train.NodeID = nodeId; positions.Add(fPosition);

                foreach (ushort trailer in train.Trailers.Where(x => x != frontVehicleId))
                {
                    train.CSegment.AddNew(TrainPosition(trailer.ToVehicle(), ref dummy, out PathUnit.Position tPosition));
                    positions.AddNew(tPosition);
                }

                if (trainId != frontVehicleId)
                {
                    train.CSegment.AddNew(TrainPosition(trainId.ToVehicle(), ref dummy, out PathUnit.Position tPosition));
                    positions.AddNew(tPosition);
                }
                return positions;
            }
            catch { return new List<PathUnit.Position>(); }
        }

        public static List<ushort> GetNextPosition(Vehicle vehicle, out PathUnit.Position Position)
        {
            List<ushort> segments = new List<ushort>();
            uint pathUnitId = vehicle.m_path;
            PathUnit pathUnit = pathManager.m_pathUnits.m_buffer[pathUnitId];

            byte pathPosIndex = vehicle.m_pathPositionIndex;
            if (pathPosIndex == 255) pathPosIndex = 0; pathPosIndex = (byte)(pathPosIndex >> 1);
            pathUnit.GetNextPosition(pathPosIndex, out PathUnit.Position nextPos);
            segments.Add(nextPos.m_segment); Position = nextPos;

            return segments;
        }

        public static ushort TrainPosition(Vehicle vehicle, ref ushort nodeId, out PathUnit.Position Position)
        {
            uint pathUnitId = vehicle.m_path;
            PathUnit pathUnit = pathManager.m_pathUnits.m_buffer[pathUnitId];

            byte pathPosIndex = vehicle.m_pathPositionIndex;
            if (pathPosIndex == 255) pathPosIndex = 0; pathPosIndex = (byte)(pathPosIndex >> 1);
            pathUnit.GetPosition(pathPosIndex, out PathUnit.Position curPos); Position = curPos;

            NetSegment pSegment = curPos.m_segment.ToSegment();
            nodeId = curPos.m_offset < 128
                         ? pSegment.m_startNode
                         : pSegment.m_endNode;

            return curPos.m_segment;
        }

    }
}
