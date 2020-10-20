using System;
using System.Collections.Generic;
using System.Text;

namespace System.Activities.Runtime.DurableInstancing.Entities
{
    public class InstanceMetadataEntity
    {
        [ComponentModel.DataAnnotations.Key]
        public Guid Id { get; set; }

        public string Serialized { get; set; }
    }
}
