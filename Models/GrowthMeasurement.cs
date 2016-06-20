using System.Runtime.Serialization;

namespace DataModels
{
    [DataContract]
    public class GrowthMeasurement
    {
        [DataMember]
        public float Time { get; set; }
        [DataMember]
        public float OD { get; set; }


        public GrowthMeasurement()
        {
        }

        public GrowthMeasurement(float time, float od)
        {
            Time = time;
            OD = od;
        }
    }
}
