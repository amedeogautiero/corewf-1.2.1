using System;
using System.Collections.Generic;
using System.Text;

namespace System.Activities
{
    public class WorkflowDefinition
    {
        public Guid InstanceCorrelation { get; set; }

        public System.Activities.Activity Workflow { get; set; }
    }
}
