using System;
using System.Collections.Generic;
using System.Text;

namespace System.Activities.Runtime.Core
{
    public class WorkflowInstanceContext
    {
        public object Request { get; set; }

        public object Response { get; set; }

        internal bool responsed { get; set; }
    }

    public class WorkflowInstanceContext<TRequest, TResponse> : WorkflowInstanceContext
    {
        public new TRequest Request { get; set; }

        public new TResponse Response { get; set; }

        
    }
}
