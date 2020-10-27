using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace wfIntesa.Messages
{
    [DataContract]
    public class SFSystemCalculatorStartRequest : RequestBase
    {
    }

    [DataContract]
    public class SFSystemCalculatorStartResponse : ResponseBase
    {
        [DataMember]
        public bool Started { get; set; }
    }

    [DataContract]
    public class SFSystemCalculatorOpRequest : RequestBase
    {
        [DataMember]
        public int Number { get; set; }

        [DataMember]
        public string Op { get; set; }
    }

    [DataContract]
    public class SFSystemCalculatorOpResponse : ResponseBase
    {
        [DataMember]
        public bool Executed { get; set; }
    }

    [DataContract]
    public class SFSystemCalculatorEndRequest : RequestBase
    {
    }

    [DataContract]
    public class SFSystemCalculatorEndResponse : ResponseBase
    {
        [DataMember]
        public decimal Result { get; set; }
    }
}
