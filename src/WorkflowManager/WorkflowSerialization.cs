using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace System.Activities
{
    public class WorkflowSerialization
    {
        private static List<Type> _knownTypes = null;
        internal static void initializeKnownTypes(IEnumerable<Type> knownTypesForDataContractSerializer)
        {
            //https://github.com/UiPath/corewf/blob/master/src/Test/TestFileInstanceStore/TestFileInstanceStore.cs
            _knownTypes = new List<Type>();

            System.Reflection.Assembly sysActivitiesAssembly = typeof(Activity).GetTypeInfo().Assembly;
            Type[] typesArray = sysActivitiesAssembly.GetTypes();

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

            var conf = WorkflowActivator.GetScope().ServiceProvider.GetService<IConfiguration>();

            if (conf != null 
                && conf.GetSection("WorkflowSerialization:KnownTypes") != null
                && conf.GetSection("WorkflowSerialization:KnownTypes").Exists()
                && conf.GetSection("WorkflowSerialization:KnownTypes").Get<string[]>() != null)
            {
                var knownTypesConf = conf.GetSection("WorkflowSerialization:KnownTypes").Get<string[]>();

                foreach (string knownTypeConf in knownTypesConf)
                {
                    var t = sysActivitiesAssembly.GetType(knownTypeConf);
                    if (t != null)
                    {
                        _knownTypes.Add(t);
                    }
                }
            }

            //var t1 = sysActivitiesAssembly.GetType("System.Activities.Variable`1+VariableLocation[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]");
            //_knownTypes.Add(t1);

            //_knownTypes.Add(typeof(WorkflowCorrelation));
        }

        private static DataContractSerializerSettings settings = null;
        static WorkflowSerialization()
        {
            initializeKnownTypes(null);

            settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true,
                KnownTypes = _knownTypes
            };

        }

        public static string Serialize<T>(T data)
        {
            System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T), settings);

            string serializedData = null;
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, data);
                serializedData = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }

            return serializedData;
        }

        public static T DeSerialize<T>(string serialized)
        {
            DataContractSerializer deserializer = new DataContractSerializer(typeof(T), settings);
            T data = default(T);
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serialized)))
            {
                data = (T)deserializer.ReadObject(ms);
            }

            return data;
        }
    }
}
