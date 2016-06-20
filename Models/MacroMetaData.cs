using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataModels
{
    [DataContract]
    public class YieldAnchors
    {
        [DataMember]
        public List<GrowthMeasurement> LowestPoints { get; set; }
        [DataMember]
        public List<GrowthMeasurement> HighestPoints { get; set; }
        
        public YieldAnchors()
        {
            LowestPoints = new List<GrowthMeasurement>();
            HighestPoints = new List<GrowthMeasurement>();
        }
    }

    [DataContract]
    public class Intercept
    {
        [DataMember]
        public List<GrowthMeasurement> SlopePoints { get; set; }
        [DataMember]
        public List<GrowthMeasurement> GroundPoints { get; set; }

        [DataMember]
        public double InterceptTime { get; set; }
        [DataMember]
        public double InterceptOD { get; set; }

        public Intercept()
        {
            SlopePoints = new List<GrowthMeasurement>();
            GroundPoints = new List<GrowthMeasurement>();
        }

    }

    [DataContract]
    public class MacroRateData
    {
        [DataMember]
        public List<GrowthMeasurement> RateSlopeAnchors { get; set; }
        [DataMember]
        public double GT { get; set; }

        public MacroRateData()
        {
            RateSlopeAnchors = new List<GrowthMeasurement>();
        }
    }

    [DataContract]
    public class MacroYieldData
    {
        [DataMember]
        public YieldAnchors YieldAnchors { get; set; }
        [DataMember]
        public double Yield { get; set; }

        public MacroYieldData()
        {
            YieldAnchors = new YieldAnchors();
        }
    }

    [DataContract]
    public class MacroLagData
    {
        [DataMember]
        public List<Intercept> InterceptStretchs { get; set; }
        [DataMember]
        public double Lag { get; set; }

        public MacroLagData()
        {
            InterceptStretchs = new List<Intercept>();
        }
    }


}


