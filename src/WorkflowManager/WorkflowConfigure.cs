using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

                Type StoreType = Type.GetType(config_WorkflowInstanceStore);

                if (StoreType != null)
                {
                    string config_WorkflowInstanceParams = config["WorkflowInstanceStore:InstanceParamsString"];

                    return Activator.CreateInstance(StoreType, new object[] { config_WorkflowInstanceParams });
                }
                else
                {
                    return new System.Activities.Runtime.DurableInstancing.Memory.MemoryIstancestore();
                }

                //todo: default memory store
                return null;
            });

            services.AddDbContext<System.Activities.Runtime.DurableInstancing.Memory.MemoryContext>(opt => opt.UseInMemoryDatabase("workflows"));

            services.AddSingleton<System.Activities.IWorkflowsManager, System.Activities.WorkflowsManager>();
        }

        public static void UseWorkflow(this IApplicationBuilder app)
        {
            WorkflowActivator.Configure(app.ApplicationServices);

            WorkflowSerialization.initializeKnownTypes(null);
        }
    }
}
