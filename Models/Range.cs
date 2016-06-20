using System.Runtime.Serialization;

namespace DataModels
{
    [DataContract]
    public class Range
    {
        [DataMember]
        public GrowthRangeType Type { get; set; }

        [DataMember]
        public float Max { get; set; }

        [DataMember]
        public float Min { get; set; }

        public Range(GrowthRangeType type, float max, float min)
        {
            Type = type;
            Max = max;
            Min = min;
        }

        public void SetMaxOnlyIfGreater(float value)
        {
            if(value > Max)
                Max = value;
        }

        public void SetMinOnlyIfSmaller(float value)
        {
            if (value < Min)
                Min = value;
        }
    }
}
