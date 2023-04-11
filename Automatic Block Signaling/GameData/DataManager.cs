using ICities;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Xml.Serialization;
using System;
using static dmaTrainABS.XMLData.SaveGame;
using static dmaTrainABS.GameData.Declarations;

namespace dmaTrainABS
{
    public class DataManager : SerializableDataExtensionBase
    {
        private const string DATA_ID = "DMA_TRAINABS";
        private static WriteData SaveGameData;

        private static ISerializableData SerializableData => SimulationManager.instance.m_SerializableDataWrapper;
        public const uint DataVersion = 2;

        public override void OnCreated(ISerializableData serializableData) { base.OnCreated(serializableData); }

        public override void OnLoadData() => Load();

        public static void Load()
        {
            var memoryStream = new MemoryStream();

            try
            {
                byte[] data = SerializableData.LoadData(DATA_ID);
                if (data == null) return;

                memoryStream.Write(data, 0, data.Length);
                memoryStream.Position = 0;

                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.AssemblyFormat = System
                                                 .Runtime.Serialization.Formatters
                                                 .FormatterAssemblyStyle.Full;
                SaveGameData = (WriteData)binaryFormatter.Deserialize(memoryStream);

                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NodeData));
                    using (TextReader reader = new StringReader(SaveGameData.NodeData))
                    {
                        NodeData nodeData = (NodeData)serializer.Deserialize(reader);
                        SimData.Nodes.Clear();
                        foreach (var node in nodeData.SNodeData)
                        {
                            List<SSegments> newSegments = new List<SSegments>();
                            foreach (var seg in node.Segments)
                                newSegments.Add(new SSegments
                                {
                                    GreenState = seg.GreenState,
                                    LockedBy = seg.LockedBy,
                                    SegmentID = seg.SegmentID
                                });
                            SimData.Nodes.Add(new SNodeData
                            {
                                NodeID = node.NodeID,
                                Segments = newSegments
                            });
                        }
                    }
                }

                if (SimData.Nodes.IsValid())
                {
                    foreach (var node in SimData.Nodes)
                    {
                        var nodeData = NetManager.instance.m_nodes.m_buffer[node.NodeID];
                        NetNode.Flags flags = nodeData.m_flags;
                        flags |= NetNode.Flags.TrafficLights;
                        flags |= NetNode.Flags.CustomTrafficLights;
                        NetManager.instance.m_nodes.m_buffer[node.NodeID].m_flags = flags;
                    }
                }

            }
            catch { }
            finally { memoryStream.Close(); }
        }

        public override void OnSaveData() => Save();

        public static void Save()
        {
            SaveGameData = new WriteData() { Version = DataVersion };
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            string text = string.Format("DMA Mods  ver.{0}.{1:00}.{2:000}  rev.{3}", version.Major, version.Minor, version.Build, version.Revision);
            SaveGameData.ModVersion = text;

            var serializer = new XmlSerializer(typeof(List<SNodeData>), new XmlRootAttribute("NodeData"));
            using (var stream = new StringWriter())
            {
                serializer.Serialize(stream, SimData.Nodes);
                SaveGameData.NodeData = stream.ToString();
            }

            var binaryFormatter = new BinaryFormatter();
            var memoryStream = new MemoryStream();

            try
            {
                binaryFormatter.Serialize(memoryStream, SaveGameData);
                memoryStream.Position = 0;
                SerializableData.SaveData(DATA_ID, memoryStream.ToArray());
            }
            catch { }
            finally { memoryStream.Close(); }
        }

        public override void OnReleased()
        {
            base.OnReleased();
        }

    }

    [Serializable]
    public class WriteData
    {
        internal string ModVersion;
        internal uint Version;
        internal string NodeData;
    }

}