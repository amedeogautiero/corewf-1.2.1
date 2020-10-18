using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Runtime.DurableInstancing;
using System.Activities;

namespace System.Activities
{
    public interface IWorkflowsManager
    {
        TResponse StartWorkflow<TRequest, TResponse>(Activity workflow, TRequest request, string OperationName);
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

        public TResponse StartWorkflow<TRequest, TResponse>(Activity workflow, TRequest request, string OperationName)
        {
            TResponse response = default(TResponse);

            WorkflowInstanceManager workflowManager = new WorkflowInstanceManager(workflow);
            workflowManager.instanceStore = InstanceStore;
            try
            {
                response = workflowManager.StartWorkflow<TRequest, TResponse>(request, OperationName);
            }
            catch (Exception exc)
            {
                throw exc;
            }
            return response;
        }
    }
}
