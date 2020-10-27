using System;
using System.Collections.Generic;
using System.Text;

namespace System.Activities.Runtime.Core.DurableInstancing.Entities
{
    public class InstanceDataEntity
    {
        [ComponentModel.DataAnnotations.Key]
        public Guid Id { get; set; }

        public string Serialized { get; set; }
    }
}
