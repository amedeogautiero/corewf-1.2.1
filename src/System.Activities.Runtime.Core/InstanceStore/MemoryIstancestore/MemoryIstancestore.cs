using System;
using System.Activities.DurableInstancing;
using System.Activities.Runtime.Core.DurableInstancing.Entities;
using System.Activities.Runtime.DurableInstancing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Activities.Runtime.Core.DurableInstancing.Memory
{
    public class MemoryIstancestore : BaseIstancestore
    {
        private static IstancestoreDelegates getDelegates()
        {
            IstancestoreDelegates delegates = new IstancestoreDelegates()
            {
                 CreateWorkflowOwner = null,
                 SaveWorkflow = null,
                 LoadWorkflow = null,
                 DeleteWorkflowOwner = null,
                 ToPersist = (cl, id, im) => 
                 {
                     return true;
                 }
            };

            return delegates;
        }

        public MemoryIstancestore():base()
        {
            base.ToPersist = (serialized) =>
            {
                Guid cid = this.Correlation.CorrelationId;
                CorrelationEntity ce = new CorrelationEntity()
                {
                    CorrelationId = cid,
                    WorkflowId = this.Correlation.WorkflowId
                };
                InstanceDataEntity id = new InstanceDataEntity()
                {
                    Id = cid,
                    Serialized = serialized.SerializedInstanceData,
                };
                InstanceMetadataEntity im = new InstanceMetadataEntity()
                {
                    Id = cid,
                    Serialized = serialized.SerializedInstanceMetadata,
                };

                using (MemoryContext dbContext = new MemoryContext())
                {
                    if (dbContext.Correlations.FirstOrDefault(c => c.CorrelationId == cid) == null)
                    { 
                        dbContext.Correlations.Add(ce);
                    }

                    var _id = dbContext.Instances.FirstOrDefault(c => c.Id == cid);
                    if (_id == null)
                    {
                        dbContext.Instances.Add(id);
                    }
                    else
                    {
                        _id.Serialized = id.Serialized;
                    }

                    var _im = dbContext.Metadata.FirstOrDefault(c => c.Id == cid);
                    if (_im == null)
                    {
                        dbContext.Metadata.Add(im);
                    }
                    else
                    {
                        _im.Serialized = im.Serialized;
                    }

                    dbContext.SaveChanges();
                }

                return true;
            };

            base.ToLoad = () =>
            {
                Serialized serialized = new Serialized();
                using (MemoryContext dbContext = new MemoryContext())
                {
                    //dbContext.Correlations.Add(ce);
                    serialized.SerializedInstanceData = dbContext.Instances.FirstOrDefault(id => id.Id == Correlation.CorrelationId).Serialized;
                    serialized.SerializedInstanceMetadata = dbContext.Metadata.FirstOrDefault(id => id.Id == Correlation.CorrelationId).Serialized;
                }

                return serialized;
            };

            base.ToDelete = () =>
            {
                Guid cid = this.Correlation.CorrelationId;
                using (MemoryContext dbContext = new MemoryContext())
                {
                    var _im = dbContext.Metadata.FirstOrDefault(c => c.Id == cid);
                    if (_im != null)
                    {
                        dbContext.Metadata.Remove(_im);
                    }
                    var _id = dbContext.Instances.FirstOrDefault(c => c.Id == cid);
                    if (_id != null)
                    {
                        dbContext.Instances.Remove(_id);
                    }
                    var ce = dbContext.Correlations.FirstOrDefault(c => c.CorrelationId == cid);
                    if (ce != null)
                    {
                        dbContext.Correlations.Remove(ce);
                    }
                    dbContext.SaveChanges();
                }
                return true;
            };

            base.ToCorrelate = () =>
            {
                if (this.Correlation.WorkflowId == Guid.Empty)
                {
                    Guid cid = this.Correlation.CorrelationId;
                    using (MemoryContext dbContext = new MemoryContext())
                    {
                        var ce = dbContext.Correlations.FirstOrDefault(c => c.CorrelationId == cid);
                        if (ce != null)
                        {
                            this.Correlation.WorkflowId = ce.WorkflowId;
                        }
                    }
                }
            };
        }

        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                return base.BeginTryCommand(context, command, timeout, callback, state);
            }
            catch (Exception e)
            {
                return new TypedCompletedAsyncResult<Exception>(e, callback, state);
            }
        }
    }
}
