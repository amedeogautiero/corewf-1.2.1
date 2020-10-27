using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Messages
{
    [DataContract]
    public class RequestBase
    { }

    [DataContract]
    public class ResponseBase
    {
        public Risultato Risultato { get; set; } = new Risultato();
    }

    [DataContract]
    public class Risultato
    {
        public bool Esito { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }
}
