using Microsoft.EntityFrameworkCore;
using System;
using System.Activities.Runtime.DurableInstancing.Entities;
using System.Collections.Generic;
using System.Text;

namespace System.Activities.Runtime.DurableInstancing.Memory
{
    public class MemoryContext : DbContext
    {
        public MemoryContext(DbContextOptions<MemoryContext> options)
           : base(options)
        {
        }

        public MemoryContext()
           : base(new DbContextOptions<MemoryContext>())
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //base.OnConfiguring(optionsBuilder);
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("workflows");
            }

        }

        public DbSet<CorrelationEntity> Correlations { get; set; }

        public DbSet<InstanceDataEntity> Instances { get; set; }

        public DbSet<InstanceMetadataEntity> Metadata { get; set; }
    }
}
