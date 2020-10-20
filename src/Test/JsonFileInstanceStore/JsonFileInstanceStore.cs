// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.DurableInstancing;
using System.Activities.Runtime.DurableInstancing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Activities;
using System.Reflection;

namespace JsonFileInstanceStore
{
    public class FileInstanceStore : InstanceStore, System.Activities.IStoreCorrelation
    {
        public System.Activities.WorkflowCorrelation Correlation { get; set; }

        private readonly string _storeDirectoryPath;

        private string _storePathInstanceData
        {
            get
            {
                //_storeDirectoryPath + "\\" + context.InstanceView.InstanceId
                //return System.IO.Path.Combine(_storeDirectoryPath, $"{Correlation.CorrelationId}-InstanceData");
                return System.IO.Path.Combine(_storeDirectoryPath, $"AAAA-InstanceData");
            }
        }

        private string _storePathInstanceMetadata
        {
            get
            {
                //_storeDirectoryPath + "\\" + context.InstanceView.InstanceId
                //return System.IO.Path.Combine(_storeDirectoryPath, $"{Correlation.CorrelationId}-InstanceMetadata");
                return System.IO.Path.Combine(_storeDirectoryPath, $"AAAA-InstanceMetadata");
            }
        }

        private List<Type> _knownTypes = null;
        private void initializeKnownTypes(IEnumerable<Type> knownTypesForDataContractSerializer)
        {
            //https://github.com/UiPath/corewf/blob/master/src/Test/TestFileInstanceStore/TestFileInstanceStore.cs
            _knownTypes = new List<Type>();

            System.Reflection.Assembly sysActivitiesAssembly = typeof(Activity).GetTypeInfo().Assembly;
            Type[] typesArray = sysActivitiesAssembly.GetTypes();

            //Variable<int>.VariableLocation

            //var t1 = typeof(Variable<int>); 

            // Remove types that are not decorated with a DataContract attribute
            foreach (Type t in typesArray)
            {
                TypeInfo typeInfo = t.GetTypeInfo();
                if (typeInfo.GetCustomAttribute<System.Runtime.Serialization.DataContractAttribute>() != null)
                {
                    _knownTypes.Add(t);
                }
            }

            if (knownTypesForDataContractSerializer != null)
            {
                foreach (Type knownType in knownTypesForDataContractSerializer)
                {
                    _knownTypes.Add(knownType);
                }
            }

            var t1 = sysActivitiesAssembly.GetType("System.Activities.Variable`1+VariableLocation[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]");
            _knownTypes.Add(t1);
        }


        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        public FileInstanceStore(string storeDirectoryPath)
        {
            _storeDirectoryPath = storeDirectoryPath;
            Directory.CreateDirectory(storeDirectoryPath);

            initializeKnownTypes(null);
        }

        public bool KeepInstanceDataAfterCompletion
        {
            get;
            set;
        }

        private void DeleteFiles(Guid instanceId)
        {
            try
            {
                //File.Delete(_storeDirectoryPath + "\\" + instanceId.ToString() + "-InstanceData");
                //File.Delete(_storeDirectoryPath + "\\" + instanceId.ToString() + "-InstanceMetadata");
                File.Delete(_storePathInstanceData);
                File.Delete(_storePathInstanceMetadata);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception trying to delete files for {0}: {1} - {2}", instanceId.ToString(), ex.GetType().ToString(), ex.Message);
            }
        }

        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                if (command is SaveWorkflowCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(SaveWorkflow(context, (SaveWorkflowCommand)command), callback, state);
                }
                else if (command is LoadWorkflowCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(LoadWorkflow(context, (LoadWorkflowCommand)command), callback, state);
                }
                else if (command is CreateWorkflowOwnerCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(CreateWorkflowOwner(context, (CreateWorkflowOwnerCommand)command), callback, state);
                }
                else if (command is DeleteWorkflowOwnerCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(DeleteWorkflowOwner(context, (DeleteWorkflowOwnerCommand)command), callback, state);
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

        private void serialize_json(Dictionary<string, InstanceValue> instanceData, Dictionary<string, InstanceValue> instanceMetadata)
        {
            var serializedInstanceData = JsonConvert.SerializeObject(instanceData, Formatting.Indented, _jsonSerializerSettings);
            File.WriteAllText(_storePathInstanceData, serializedInstanceData);

            var serializedInstanceMetadata = JsonConvert.SerializeObject(instanceMetadata, Formatting.Indented, _jsonSerializerSettings);
            File.WriteAllText(_storePathInstanceMetadata, serializedInstanceMetadata);
        }

        private void deserialize_json(out Dictionary<string, InstanceValue> instanceData, out Dictionary<string, InstanceValue> instanceMetadata)
        {
            var serializedInstanceData = File.ReadAllText(_storePathInstanceData);
            instanceData = JsonConvert.DeserializeObject<Dictionary<string, InstanceValue>>(serializedInstanceData, _jsonSerializerSettings);

            var serializedInstanceMetadata = File.ReadAllText(_storePathInstanceMetadata);
            instanceMetadata = JsonConvert.DeserializeObject<Dictionary<string, InstanceValue>>(serializedInstanceMetadata, _jsonSerializerSettings);
        }

        private void serialize_dc(Dictionary<string, InstanceValue> instanceData, Dictionary<string, InstanceValue> instanceMetadata)
        {
            System.Runtime.Serialization.DataContractSerializerSettings settings = new System.Runtime.Serialization.DataContractSerializerSettings
            {
                PreserveObjectReferences = true,
                KnownTypes = _knownTypes
            };

            System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer(instanceData.GetType(), settings);

            string serializedInstanceData = null;
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, instanceData);
                serializedInstanceData = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }

            string serializedInstanceMetadata = null;
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, instanceMetadata);
                serializedInstanceMetadata = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }

            File.WriteAllText(_storePathInstanceData, serializedInstanceData);
            File.WriteAllText(_storePathInstanceMetadata, serializedInstanceMetadata);
        }

        private void deserialize_dc(out Dictionary<string, InstanceValue> instanceData, out Dictionary<string, InstanceValue> instanceMetadata)
        {
            System.Runtime.Serialization.DataContractSerializerSettings settings = new System.Runtime.Serialization.DataContractSerializerSettings
            {
                PreserveObjectReferences = true,
                KnownTypes = _knownTypes
            };
            System.Runtime.Serialization.DataContractSerializer deserializer = new System.Runtime.Serialization.DataContractSerializer(typeof(Dictionary<string, InstanceValue>), settings);

            var serializedInstanceData = File.ReadAllText(_storePathInstanceData);
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serializedInstanceData)))
            {
                instanceData = (Dictionary<string, InstanceValue>)deserializer.ReadObject(ms);
            }

            var serializedInstanceMetadata = File.ReadAllText(_storePathInstanceMetadata);
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serializedInstanceMetadata)))
            {
                instanceMetadata = (Dictionary<string, InstanceValue>)deserializer.ReadObject(ms);
            }
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
                if (!KeepInstanceDataAfterCompletion)
                {
                    DeleteFiles(context.InstanceView.InstanceId);
                }
            }
            else
            {
                Dictionary<string, InstanceValue> instanceData = SerializeablePropertyBagConvertXNameInstanceValue(command.InstanceData);
                Dictionary<string, InstanceValue> instanceMetadata = SerializeInstanceMetadataConvertXNameInstanceValue(context, command);

                try
                {
                    //var serializedInstanceData = JsonConvert.SerializeObject(instanceData, Formatting.Indented, _jsonSerializerSettings);
                    ////File.WriteAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceData", serializedInstanceData);
                    //File.WriteAllText(_storePathInstanceData, serializedInstanceData);
                    //var test_deserializ = JsonConvert.DeserializeObject<Dictionary<string, InstanceValue>>(serializedInstanceData, _jsonSerializerSettings);

                    //var serializedInstanceMetadata = JsonConvert.SerializeObject(instanceMetadata, Formatting.Indented, _jsonSerializerSettings);
                    ////File.WriteAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceMetadata", serializedInstanceMetadata);
                    //File.WriteAllText(_storePathInstanceMetadata, serializedInstanceMetadata);
                    serialize_dc(instanceData, instanceMetadata);
                }
                catch (Exception exc)
                {
                    System.Runtime.Serialization.DataContractSerializerSettings settings = new System.Runtime.Serialization.DataContractSerializerSettings
                    {
                        PreserveObjectReferences = true,
                        KnownTypes = _knownTypes
                    };

                    string s1 = null;
                    System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer(instanceData.GetType(), settings);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        serializer.WriteObject(ms, instanceData);
                        s1 = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                    }

                    Dictionary<string, InstanceValue> obj = null;
                    System.Runtime.Serialization.DataContractSerializer deserializer = new System.Runtime.Serialization.DataContractSerializer(instanceData.GetType(), settings);
                    using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(s1)))
                    {
                        obj = (Dictionary<string, InstanceValue>)deserializer.ReadObject(ms);
                    }


                    throw;
                }

                foreach (KeyValuePair<XName, InstanceValue> property in command.InstanceMetadataChanges)
                {
                    context.WroteInstanceMetadataValue(property.Key, property.Value);
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

            Dictionary<string, InstanceValue> serializableInstanceData;
            Dictionary<string, InstanceValue> serializableInstanceMetadata;

            try
            {
                ////var serializedInstanceData = File.ReadAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceData");
                //var serializedInstanceData = File.ReadAllText(_storePathInstanceData);
                //serializableInstanceData = JsonConvert.DeserializeObject<Dictionary<string, InstanceValue>>(serializedInstanceData, _jsonSerializerSettings);

                ////var serializedInstanceMetadata = File.ReadAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceMetadata");
                //var serializedInstanceMetadata = File.ReadAllText(_storePathInstanceMetadata);
                //serializableInstanceMetadata = JsonConvert.DeserializeObject<Dictionary<string, InstanceValue>>(serializedInstanceMetadata, _jsonSerializerSettings);

                deserialize_dc(out serializableInstanceData, out serializableInstanceMetadata);
            }
            catch (Exception exc)
            {
                throw;
            }

            instanceData = this.DeserializePropertyBagConvertXNameInstanceValue(serializableInstanceData);
            instanceMetadata = this.DeserializePropertyBagConvertXNameInstanceValue(serializableInstanceMetadata);

            context.LoadedInstance(InstanceState.Initialized, instanceData, instanceMetadata, null, null);

            return true;
        }

        private bool CreateWorkflowOwner(InstancePersistenceContext context, CreateWorkflowOwnerCommand command)
        {
            Guid instanceOwnerId = Guid.NewGuid();
            context.BindInstanceOwner(instanceOwnerId, instanceOwnerId);
            context.BindEvent(HasRunnableWorkflowEvent.Value);
            return true;
        }

        private bool DeleteWorkflowOwner(InstancePersistenceContext context, DeleteWorkflowOwnerCommand command)
        {
            return true;
        }

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
    }
}
