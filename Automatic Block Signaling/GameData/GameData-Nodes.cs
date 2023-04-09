using System.Collections.Generic;

namespace dmaTrainABS.XMLData
{
    public partial class SaveGame
    {
        // --- NodeData ---
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
        public partial class NodeData
        {
            [System.Xml.Serialization.XmlElementAttribute("SNodeData")]
            public List<NodeDataSNodeData> SNodeData { get; set; }
        }

        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class NodeDataSNodeData
        {
            public ushort NodeID { get; set; }

            [System.Xml.Serialization.XmlArrayItemAttribute("SSegments", IsNullable = false)]
            public List<NodeDataSNodeDataSSegments> Segments { get; set; }
        }

        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class NodeDataSNodeDataSSegments
        {
            public ushort SegmentID { get; set; }
            public ushort LockedBy { get; set; }
            public bool GreenState { get; set; }
        }

    }
}
