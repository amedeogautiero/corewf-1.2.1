using System;
using System.Collections.Generic;
using System.Text;

namespace System.Activities
{
    public interface IStoreCorrelation
    {
        WorkflowCorrelation Correlation { get; set; }
    }

    public class WorkflowCorrelation
    {
        public Guid WorkflowId { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
