using System.Collections.Generic;

namespace dmaTrainABS.Traffic
{
    public class BlockDataVars
    {
        public class Block
        {
            //public string Ident { get; set; }
            public ushort BlockId { get; set; }
            public ushort BlockedBy { get; set; }
            //public byte Lane { get; set; }
            //public bool IsJunction { get; set; }
            public bool Blocked { get => BlockedBy != 0; }
            public ushort StartNode { get; set; }
            public ushort EndNode { get; set; }
            public List<ushort> Segments { get; set; }
        }

        public class Node
        {
            public ushort NodeId { get; set; }
            public int Segments { get; set; }
            public bool IsJunction { get; set; }
            public bool HaveLights { get; set; }
            public bool Processed { get; set; }
            public byte Counter { get; set; }
        }

    }
}
