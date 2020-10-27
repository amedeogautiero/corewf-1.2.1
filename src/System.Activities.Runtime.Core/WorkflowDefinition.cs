using System;
using System.Collections.Generic;
using System.Text;

namespace System.Activities.Runtime.Core
{
    public class WorkflowDefinition
    {
        //public Guid InstanceCorrelation { get; set; }

        public WorkflowCorrelation Correlation { get; set; } = new WorkflowCorrelation() { CorrelationId = Guid.Empty, WorkflowId = Guid.Empty };

        public System.Activities.Activity Workflow { get; set; }
    }
}
