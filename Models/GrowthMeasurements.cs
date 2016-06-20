using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataModels
{
    [DataContract]
    public class GrowthMeasurements
    {
        [DataMember]
        public Dictionary<DataType, List<GrowthMeasurement>> Measurements { get; set; }

        [DataMember]
        public Dictionary<DataType, GrowthVariableMetaData> VariableMetaDatas { get; set; }

        //public GrowthVariableMetaData VariableMetaData { get; set; }

        [DataMember]
        public bool IsFaulty { get; set; }

        public GrowthMeasurements(DataType dataType, List<GrowthMeasurement> measurements)
        {
            Measurements = new Dictionary<DataType, List<GrowthMeasurement>>();
            Measurements.Add(dataType, measurements);
            VariableMetaDatas = new Dictionary<DataType, GrowthVariableMetaData>();
            IsFaulty = false;
        }

        public GrowthMeasurements()
        {
            Measurements = new Dictionary<DataType, List<GrowthMeasurement>>();
            VariableMetaDatas = new Dictionary<DataType, GrowthVariableMetaData>();
            IsFaulty = false;
        }

        public List<GrowthMeasurement> GetMeasurements(DataType dataType)
        {
            if (Measurements.ContainsKey(dataType))
                return Measurements[dataType];
            return null;
        }

        public void SetMeasurements(List<GrowthMeasurement> growthMeasurements, DataType dataType)
        {
            if (Measurements.ContainsKey(dataType))
                Measurements[dataType] = growthMeasurements;
            else
                Measurements.Add(dataType, growthMeasurements);
        }

        public void SetMetaData(DataType dataType, GrowthVariableMetaData metaData)
        {
            if (VariableMetaDatas.ContainsKey(dataType))
                VariableMetaDatas[dataType] = metaData;
            else
                VariableMetaDatas.Add(dataType, metaData);
        }
    }
}
