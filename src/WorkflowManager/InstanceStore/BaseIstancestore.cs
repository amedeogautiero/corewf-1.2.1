using System;
using System.Activities.DurableInstancing;
using System.Activities.Runtime.DurableInstancing.Entities;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    public class BaseIstancestore : InstanceStore, System.Activities.IStoreCorrelation
    {
        private IstancestoreDelegates delegates = null;

        public System.Activities.WorkflowCorrelation Correlation { get; set; }

        public void Correlate()
        {
            if (ToCorrelate != null)
                ToCorrelate();
        }

        public BaseIstancestore()
        {}
        public BaseIstancestore(IstancestoreDelegates delegates)
        {
            this.delegates = delegates;
        }

        public bool KeepInstanceDataAfterCompletion {get;set;}

        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                if (command is SaveWorkflowCommand)
                {
                    if (delegates != null && delegates.SaveWorkflow != null)
                        return new TypedCompletedAsyncResult<bool>(delegates.SaveWorkflow(context, (SaveWorkflowCommand)command), callback, state);
                    return new TypedCompletedAsyncResult<bool>(this.SaveWorkflow(context, (SaveWorkflowCommand)command), callback, state);
                }
                else if (command is LoadWorkflowCommand)
                {
                    if (delegates != null && delegates.LoadWorkflow != null)
                        return new TypedCompletedAsyncResult<bool>(delegates.LoadWorkflow(context, (LoadWorkflowCommand)command), callback, state);
                    return new TypedCompletedAsyncResult<bool>(this.LoadWorkflow(context, (LoadWorkflowCommand)command), callback, state);
                }
                else if (command is CreateWorkflowOwnerCommand)
                {
                    if (delegates != null && delegates.CreateWorkflowOwner != null)
                        return new TypedCompletedAsyncResult<bool>(delegates.CreateWorkflowOwner(context, (CreateWorkflowOwnerCommand)command), callback, state);
                    return new TypedCompletedAsyncResult<bool>(this.CreateWorkflowOwner(context, (CreateWorkflowOwnerCommand)command), callback, state);
                }
                else if (command is DeleteWorkflowOwnerCommand)
                {
                    if (delegates != null && delegates.DeleteWorkflowOwner != null)
                        return new TypedCompletedAsyncResult<bool>(delegates.DeleteWorkflowOwner(context, (DeleteWorkflowOwnerCommand)command), callback, state);
                    return new TypedCompletedAsyncResult<bool>(this.DeleteWorkflowOwner(context, (DeleteWorkflowOwnerCommand)command), callback, state);
                }
                return new TypedCompletedAsyncResult<bool>(false, callback, state);
            }
            catch (Exception e)
            {
                return new TypedCompletedAsyncResult<Exception>(e, callback, state);
            }
        }

        protected override bool EndTryCommand(IAsyncResult result)
        {
            if (result is TypedCompletedAsyncResult<Exception> exceptionResult)
            {
                throw exceptionResult.Data;
            }
            return TypedCompletedAsyncResult<bool>.End(result);
        }
        protected Func<Serialized, bool> ToPersist { get; set; }
        
        protected Func<Serialized> ToLoad { get; set; }

        protected Func<bool> ToDelete { get; set; }

        protected Action ToCorrelate { get; set; }

        private Dictionary<string, InstanceValue> SerializeablePropertyBagConvertXNameInstanceValue(IDictionary<XName, InstanceValue> source)
        {
            Dictionary<string, InstanceValue> scratch = new Dictionary<string, InstanceValue>();
            foreach (KeyValuePair<XName, InstanceValue> property in source)
            {
                bool writeOnly = (property.Value.Options & InstanceValueOptions.WriteOnly) != 0;

                if (!writeOnly && !property.Value.IsDeletedValue)
                {
                    scratch.Add(property.Key.ToString(), property.Value);
                }
            }

            return scratch;
        }

        private Dictionary<string, InstanceValue> SerializeInstanceMetadataConvertXNameInstanceValue(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            Dictionary<string, InstanceValue> metadata = null;

            foreach (var property in command.InstanceMetadataChanges)
            {
                if (!property.Value.Options.HasFlag(InstanceValueOptions.WriteOnly))
                {
                    if (metadata == null)
                    {
                        metadata = new Dictionary<string, InstanceValue>();
                        // copy current metadata. note that we must get rid of InstanceValue as it is not properly serializeable
                        foreach (var m in context.InstanceView.InstanceMetadata)
                        {
                            metadata.Add(m.Key.ToString(), m.Value);
                        }
                    }

                    if (metadata.ContainsKey(property.Key.ToString()))
                    {
                        if (property.Value.IsDeletedValue) metadata.Remove(property.Key.ToString());
                        else metadata[property.Key.ToString()] = property.Value;
                    }
                    else
                    {
                        if (!property.Value.IsDeletedValue) metadata.Add(property.Key.ToString(), property.Value);
                    }
                }
            }

            if (metadata == null)
                metadata = new Dictionary<string, InstanceValue>();

            return metadata;
        }

        private IDictionary<XName, InstanceValue> DeserializePropertyBagConvertXNameInstanceValue(Dictionary<string, InstanceValue> source)
        {
            Dictionary<XName, InstanceValue> destination = new Dictionary<XName, InstanceValue>();

            foreach (KeyValuePair<string, InstanceValue> property in source)
            {
                destination.Add(property.Key, property.Value);
            }

            return destination;
        }


        private bool CreateWorkflowOwner(InstancePersistenceContext context, CreateWorkflowOwnerCommand command)
        {
            Guid instanceOwnerId = Guid.NewGuid();
            context.BindInstanceOwner(instanceOwnerId, instanceOwnerId);
            context.BindEvent(HasRunnableWorkflowEvent.Value);
            return true;
        }

        private bool SaveWorkflow(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            if (context.InstanceVersion == -1)
            {
                context.BindAcquiredLock(0);
            }

            if (command.CompleteInstance)
            {
                context.CompletedInstance();
                if (!KeepInstanceDataAfterCompletion && ToDelete != null)
                {
                    //DeleteFiles(context.InstanceView.InstanceId);
                    ToDelete();
                }
            }
            else
            {
                Dictionary<string, InstanceValue> instanceData = SerializeablePropertyBagConvertXNameInstanceValue(command.InstanceData);
                Dictionary<string, InstanceValue> instanceMetadata = SerializeInstanceMetadataConvertXNameInstanceValue(context, command);

                try
                {
                    //serialize_dc(instanceData, instanceMetadata);
                    //string serializedCorrelation = WorkflowSerialization.Serialize<WorkflowCorrelation>(this.Correlation);
                    string serializedInstanceData = WorkflowSerialization.Serialize<Dictionary<string, InstanceValue>>(instanceData);
                    string serializedInstanceMetadata = WorkflowSerialization.Serialize<Dictionary<string, InstanceValue>>(instanceMetadata);

                    if (ToPersist != null)
                    {
                        ToPersist(new Serialized()
                                    {
                                        SerializedInstanceData = WorkflowSerialization.Serialize<Dictionary<string, InstanceValue>>(instanceData),
                                        SerializedInstanceMetadata = WorkflowSerialization.Serialize<Dictionary<string, InstanceValue>>(instanceMetadata)
                                    });
                    }
                }
                catch (Exception exc)
                {
                    throw exc;
                }

                context.PersistedInstance(command.InstanceData);
                if (command.CompleteInstance)
                {
                    context.CompletedInstance();
                }

                if (command.UnlockInstance || command.CompleteInstance)
                {
                    context.InstanceHandle.Free();
                }
            }

            return true;
        }

        private bool LoadWorkflow(InstancePersistenceContext context, LoadWorkflowCommand command)
        {
            if (command.AcceptUninitializedInstance)
            {
                return false;
            }

            if (context.InstanceVersion == -1)
            {
                context.BindAcquiredLock(0);
            }

            IDictionary<XName, InstanceValue> instanceData = null;
            IDictionary<XName, InstanceValue> instanceMetadata = null;

            Dictionary<string, InstanceValue> serializableInstanceData = null;
            Dictionary<string, InstanceValue> serializableInstanceMetadata = null;

            try
            {
                if (ToLoad != null)
                {
                    Serialized serialized = ToLoad();

                    serializableInstanceData = WorkflowSerialization.DeSerialize<Dictionary<string, InstanceValue>>(serialized.SerializedInstanceData);
                    serializableInstanceMetadata = WorkflowSerialization.DeSerialize<Dictionary<string, InstanceValue>>(serialized.SerializedInstanceMetadata);
                }
            }
            catch (Exception exc)
            {
                throw;
            }

            if (serializableInstanceData != null)
                instanceData = this.DeserializePropertyBagConvertXNameInstanceValue(serializableInstanceData);

            if (serializableInstanceMetadata != null)
                instanceMetadata = this.DeserializePropertyBagConvertXNameInstanceValue(serializableInstanceMetadata);

            context.LoadedInstance(InstanceState.Initialized, instanceData, instanceMetadata, null, null);

            return true;
        }
        private bool DeleteWorkflowOwner(InstancePersistenceContext context, DeleteWorkflowOwnerCommand command)
        {
            return true;
        }
    }

    public class IstancestoreDelegates
    {
        public Func<InstancePersistenceContext, SaveWorkflowCommand, bool> SaveWorkflow { get; set; }
        public Func<InstancePersistenceContext, LoadWorkflowCommand, bool> LoadWorkflow { get; set; }
        public Func<InstancePersistenceContext, CreateWorkflowOwnerCommand, bool> CreateWorkflowOwner { get; set; }
        public Func<InstancePersistenceContext, DeleteWorkflowOwnerCommand, bool> DeleteWorkflowOwner { get; set; }
        public Func<CorrelationEntity, InstanceDataEntity, InstanceMetadataEntity, bool> ToPersist { get; set; }
    }

    public class Serialized
    {
        public string SerializedInstanceData { get; set; }

        public string SerializedInstanceMetadata { get; set; }
    }

}
