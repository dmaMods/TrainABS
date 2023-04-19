﻿using System.Collections.Generic;

namespace dmaTrainABS.GameData
{
    public class Declarations
    {
#if DEBUG
        public static bool IsDebugMode = true;
#else
        public static bool IsDebugMode = false;
#endif

        public class SWaitingList
        {
            public ushort NodeId { get; set; }
            public int ProcessId { get; set; }
            public ushort TrainId { get; set; }
            public bool Processed { get; set; }
        }

        public class SGreenList
        {
            public ushort BlockId { get; set; }
            public ushort TrainId { get; set; }
        }

        public class SNodeData
        {
            public ushort NodeID { get; set; }
            public List<SSegments> Segments { get; set; }
        }

        public class SSegments
        {
            public ushort SegmentID { get; set; }
            public ushort LockedBy { get; set; }
            public bool GreenState { get; set; }
        }

        public class STrains
        {
            public ushort TrainID { get; set; } = 0;
            public List<ushort> Trailers { get; set; } = new List<ushort>();
            public ushort NodeID { get; set; } = 0;
            public List<ushort> CSegment { get; set; } = new List<ushort>();
            public List<ushort> NSegment { get; set; } = new List<ushort>();
            public ushort SignalNode { get; set; } = 0;
            public ushort FirstVehicle { get; set; } = 0;
            public ushort LastVehicle { get; set; } = 0;
            public List<ushort> CBlock { get; set; } = new List<ushort>();
            public ushort NBlock { get; set; } = 0;
            public PathUnit.Position Position { get; internal set; }
        }

        public class SRailBlocks
        {
            public ushort BlockedBy { get; set; }
            public List<ushort> Segments { get; set; }
            public bool Blocked { get => BlockedBy != 0; }
        }

        public class Segment
        {
            public ushort SegmentId { get; set; }
            public ushort StartNode { get; set; }
            public ushort EndNode { get; set; }
            public bool EndSegment { get; set; }
            public uint Lane { get; set; }
            public bool Inverted { get; set; }
            public ushort BlockId { get; set; }
            public bool Processed { get; set; }
        }

    }
}
