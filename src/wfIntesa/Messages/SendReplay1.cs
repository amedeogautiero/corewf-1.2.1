using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Messages
{
    public class SendReplay1Request : RequestBase
    {
        public int Dividend { get; set; }

        public int Divisor { get; set; }
    }
    public class SendReplay1Response : ResponseBase
    {
        public int Quotient { get; set; }

        public decimal Result { get; set; }

        public int Remainder { get; set; }
    }
}
