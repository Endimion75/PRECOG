using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataModels
{
    [DataContract]
    public class GrowthVariableMetaData
    {
        [DataMember]
        public List<Intercept> Lag { get; set; }
        [DataMember]
        public List<GrowthMeasurement> Rate { get; set; }
        [DataMember]
        public YieldAnchors Yield { get; set; }

        [DataMember]
        public List<Intercept> RawLag { get; set; }
        [DataMember]
        public List<GrowthMeasurement> RawRate { get; set; }
        [DataMember]
        public YieldAnchors RawYield { get; set; }

        public GrowthVariableMetaData()
        {
            Lag = new List<Intercept>();
            Rate = new List<GrowthMeasurement>();
            Yield = new YieldAnchors();

            RawLag = new List<Intercept>();
            RawRate = new List<GrowthMeasurement>();
            RawYield = new YieldAnchors();
        }
    }
}
