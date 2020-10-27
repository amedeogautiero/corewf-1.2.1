using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace System.Activities.Runtime.Core
{
    public interface IStoreCorrelation
    {
        WorkflowCorrelation Correlation { get; set; }

        void Correlate();
    }

    [DataContract]
    public class WorkflowCorrelation
    {
        [DataMember]
        public Guid WorkflowId { get; set; }

        [DataMember]
        public Guid CorrelationId { get; set; }
    }
}
