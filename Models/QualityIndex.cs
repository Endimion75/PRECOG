using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataModels
{

    [DataContract]
    public class QualityIndex
    {
        [DataMember]
        public float R2 { get; set; }

        [DataMember]
        public float R2Worst { get; set; }
        
        [DataMember]
        public int R2Peaks { get; set; }
        
        [DataMember]
        public float PointDifference { get; set; }

        [DataMember]
        public short Flags { get; set; }

        [DataMember]
        public string FlagDetails { get; set; }

        [DataMember]
        public bool HasFlags { get; set; }
    }
}
