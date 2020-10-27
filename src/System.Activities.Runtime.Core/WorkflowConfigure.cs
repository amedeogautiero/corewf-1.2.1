using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Activities;
using System.Activities.Runtime.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace /*System.Activities*/ Microsoft.Extensions.DependencyInjection
{
    public static class WorkflowConfigure
    {
        public static void AddWorkflow(this IServiceCollection services)
        {
            services.AddTransient(typeof(System.Activities.Runtime.DurableInstancing.InstanceStore), sp =>
            {
                var config = sp.GetService<IConfiguration>();
                string config_WorkflowInstanceStore = config["WorkflowInstanceStore:StoreType"];

                Type StoreType = null;

                if (!string.IsNullOrEmpty(config_WorkflowInstanceStore))
                    StoreType = Type.GetType(config_WorkflowInstanceStore);

                if (StoreType != null)
                {
                    string config_WorkflowInstanceParams = config["WorkflowInstanceStore:InstanceParamsString"];

                    return Activator.CreateInstance(StoreType, new object[] { config_WorkflowInstanceParams });
                }
                else
                {
                    return new System.Activities.Runtime.Core.DurableInstancing.Memory.MemoryIstancestore();
                }

                //todo: default memory store
                return null;
            });

            services.AddDbContext<System.Activities.Runtime.Core.DurableInstancing.Memory.MemoryContext>(opt => opt.UseInMemoryDatabase("workflows"));

            services.AddSingleton<System.Activities.Runtime.Core.IWorkflowsManager, System.Activities.Runtime.Core.WorkflowsManager>();
        }

        public static void UseWorkflow(this IApplicationBuilder app)
        {
            WorkflowActivator.Configure(app.ApplicationServices);

            //var ass_ref = System.Reflection.Assembly.GetEntryAssembly()
            //                                    .GetReferencedAssemblies()
            //                                    .Select(System.Reflection.Assembly.Load)
            //                                    //.SelectMany(x => x.DefinedTypes)
            //                                    .SelectMany(x => x.GetTypes())
            //                                    .OrderBy(t => t.Name)
            //                                    //.ToArray()
            //;


            //var mytypes = System.Reflection.Assembly.GetEntryAssembly().GetTypes();
            //var all = ass_ref.Union(System.Reflection.Assembly.GetEntryAssembly().GetTypes());

            //var types = from t in all
            //            let att = t.GetCustomAttribute<DataContractAttribute>()
            //            where att != null 
            //            orderby t.Name
            //            select t;

            //var r = types.ToList();

            //.Where(type => typeof(IProfile).IsAssignableFrom(type));

            //var all = from a in System.Reflection.Assembly.GetEntryAssembly().GetReferencedAssemblies()
            //          select a;

            //var load = 

            WorkflowSerialization.initializeKnownTypes(null);
        }
    }
}
