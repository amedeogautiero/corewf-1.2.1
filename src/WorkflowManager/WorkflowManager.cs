using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Runtime.DurableInstancing;
using System.Activities;
using Microsoft.Extensions.DependencyInjection;
namespace System.Activities
{
    public interface IWorkflowsManager
    {
        TResponse StartWorkflow<TRequest, TResponse>(WorkflowDefinition definition, TRequest request, string OperationName);

        TResponse ContinueWorkflow<TRequest, TResponse>(WorkflowDefinition definition, TRequest request, string OperationName);
    }

    public class WorkflowsManager : IWorkflowsManager
    {
        private InstanceStore m_InstanceStore = null;
        public WorkflowsManager(InstanceStore instanceStore)
        {
            //string config_WorkflowInstanceStore = wfIntesa.Startup.config["WorkflowInstanceStore:StoreType"];
            //string config_WorkflowInstanceParams = wfIntesa.Startup.config["WorkflowInstanceStore:InstanceParamsString"];
            //if (!string.IsNullOrEmpty(config_WorkflowInstanceStore))
            //{
            //    InstanceStore instanceStore = new JsonFileInstanceStore.FileInstanceStore(config_WorkflowInstanceParams);
            //}

            this.InstanceStore = instanceStore;
        }

        private InstanceStore InstanceStore_todelete
        {
            get
            {
                //if (m_InstanceStore == null)
                //{
                //    string config_WorkflowInstanceStore = wfIntesa.Startup.config["WorkflowInstanceStore:StoreType"];
                //    string config_WorkflowInstanceParams = wfIntesa.Startup.config["WorkflowInstanceStore:InstanceParamsString"];
                //    if (!string.IsNullOrEmpty(config_WorkflowInstanceStore))
                //    {
                //        m_InstanceStore = new JsonFileInstanceStore.FileInstanceStore(config_WorkflowInstanceParams);
                //    }
                //}

                return m_InstanceStore;
            }
        }

        public InstanceStore InstanceStore { get; private set; }

        public WorkflowInstanceManager StartWorkflow(Activity workflow)
        {
            WorkflowInstanceManager workflowManager = new WorkflowInstanceManager(workflow);
            workflowManager.instanceStore = InstanceStore;
            return workflowManager;
        }

        public TResponse StartWorkflow<TRequest, TResponse>(WorkflowDefinition definition, TRequest request, string OperationName)
        {
            TResponse response = default(TResponse);

            WorkflowInstanceManager workflowManager = new WorkflowInstanceManager(definition.Workflow);
            //workflowManager.instanceStore = InstanceStore;

            workflowManager.instanceStore = WorkflowActivator.GetScope().ServiceProvider.GetService<System.Activities.Runtime.DurableInstancing.InstanceStore>();
            if (workflowManager.instanceStore is IStoreCorrelation)
            {
                //(workflowManager.instanceStore as IStoreCorrelation).Correlation = new WorkflowCorrelation()
                //{
                //    //CorrelationId = definition.InstanceCorrelation,
                //    CorrelationId = definition.Correlation.CorrelationId,
                //    //WorkflowId = wfApp.Id,
                //};
                (workflowManager.instanceStore as IStoreCorrelation).Correlation = definition.Correlation;
            }

            //if (InstanceStore is IStoreCorrelation)
            //{
            //    (InstanceStore as IStoreCorrelation).Correlation.CorrelationId = definition.InstanceCorrelation;
            //}

            try
            {
                response = workflowManager.StartWorkflow<TRequest, TResponse>(request, OperationName);
                definition.Correlation.WorkflowId = workflowManager.WorkflowId;
            }
            catch (Exception exc)
            {
                throw exc;
            }
            return response;
        }

        public TResponse ContinueWorkflow<TRequest, TResponse>(WorkflowDefinition definition, TRequest request, string OperationName)
        {
            TResponse response = default(TResponse);

            WorkflowInstanceManager workflowManager = new WorkflowInstanceManager(definition.Workflow);
            //workflowManager.instanceStore = InstanceStore;

            workflowManager.instanceStore = WorkflowActivator.GetScope().ServiceProvider.GetService<System.Activities.Runtime.DurableInstancing.InstanceStore>();
            if (workflowManager.instanceStore is IStoreCorrelation)
            {
                (workflowManager.instanceStore as IStoreCorrelation).Correlation = new WorkflowCorrelation()
                {
                    //CorrelationId = definition.InstanceCorrelation,
                    CorrelationId = definition.Correlation.CorrelationId,
                    //WorkflowId = wfApp.Id,
                };
            }

            try
            {
                response = workflowManager.ContinueWorkflow<TRequest, TResponse>(request, OperationName);
                definition.Correlation.WorkflowId = workflowManager.WorkflowId;
            }
            catch (Exception exc)
            {
                throw exc;
            }
            return response;
        }
    }
}
