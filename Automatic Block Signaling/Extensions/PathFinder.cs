using ColossalFramework;
using dmaTrainABS.GameData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dmaTrainABS
{
    public class PathFinder
    {
        private static readonly PathManager pathManager = Singleton<PathManager>.instance;

        internal static List<ushort> GetCurrentPosition(Declarations.STrains train, ushort frontVehicleId, out PathUnit.Position Position)
        {
            List<ushort> segments = new List<ushort>(); ushort nodeId = 0; ushort dummy = 0;
            segments.Add(TrainPosition(frontVehicleId.ToVehicle(), ref nodeId, out PathUnit.Position fPosition));
            train.NodeID = nodeId; Position = fPosition;

            foreach (ushort trailer in train.Trailers.Where(x => x != frontVehicleId))
            {
                segments.AddNew(TrainPosition(trailer.ToVehicle(), ref dummy, out PathUnit.Position tPosition));
            }

            if (train.TrainID != frontVehicleId)
                segments.AddNew(TrainPosition(train.TrainID.ToVehicle(), ref dummy, out PathUnit.Position tPosition));

            return segments;
        }

        internal static List<ushort> GetNextPosition(Vehicle vehicle,  out PathUnit.Position Position)
        {
            List<ushort> segments = new List<ushort>();
            uint pathUnitId = vehicle.m_path;
            PathUnit pathUnit = pathManager.m_pathUnits.m_buffer[pathUnitId];

            byte pathPosIndex = vehicle.m_pathPositionIndex;
            if (pathPosIndex == 255) pathPosIndex = 0; pathPosIndex = (byte)(pathPosIndex >> 1);
            pathUnit.GetPosition(pathPosIndex, out PathUnit.Position curPos);
            Position = curPos; bool posSet = false;

            if (pathPosIndex + 1 < pathUnit.m_positionCount)
            {
                for (int f = pathPosIndex + 1; f < pathUnit.m_positionCount; f++)
                {
                    pathUnit.GetPosition(f, out PathUnit.Position nextPos);
                    segments.Add(nextPos.m_segment);
                    if (!posSet) { Position = nextPos; break; }
                }
            }
            else
            {
                if (pathUnit.m_nextPathUnit != 0)
                {
                    pathUnit = pathManager.m_pathUnits.m_buffer[pathUnit.m_nextPathUnit];
                    for (int f = 0; f < pathUnit.m_positionCount; f++)
                    {
                        pathUnit.GetPosition(f, out PathUnit.Position nextPos);
                        segments.Add(nextPos.m_segment);
                        if (!posSet) { Position = nextPos; break; }
                    }
                }
            }

            return segments;
        }

        internal static ushort TrainPosition(Vehicle vehicle, ref ushort nodeId, out PathUnit.Position Position)
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
