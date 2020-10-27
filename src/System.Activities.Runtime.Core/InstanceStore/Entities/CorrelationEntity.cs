using System;
using System.Collections.Generic;
using System.Text;

namespace System.Activities.Runtime.Core.DurableInstancing.Entities
{
    public class CorrelationEntity
    {
        public Guid WorkflowId { get; set; }

        [ComponentModel.DataAnnotations.Key]
        public Guid CorrelationId { get; set; }
    }
}
