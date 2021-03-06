﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Runtime.DurableInstancing;
using System.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace wfIntesa.todelete
{
    public class WorkflowsManager
    {
        private InstanceStore m_InstanceStore = null;
        public WorkflowsManager()
        {
            //string config_WorkflowInstanceStore = wfIntesa.Startup.config["WorkflowInstanceStore:StoreType"];
            //string config_WorkflowInstanceParams = wfIntesa.Startup.config["WorkflowInstanceStore:InstanceParamsString"];
            //if (!string.IsNullOrEmpty(config_WorkflowInstanceStore))
            //{
            //    InstanceStore instanceStore = new JsonFileInstanceStore.FileInstanceStore(config_WorkflowInstanceParams);
            //}
        }

        private InstanceStore InstanceStore
        {
            get
            {
                if (m_InstanceStore == null)
                {
                    //string config_WorkflowInstanceStore = wfIntesa.Startup.config["WorkflowInstanceStore:StoreType"];
                    //string config_WorkflowInstanceParams = wfIntesa.Startup.config["WorkflowInstanceStore:InstanceParamsString"];
                    //if (!string.IsNullOrEmpty(config_WorkflowInstanceStore))
                    //{
                    //    m_InstanceStore = new JsonFileInstanceStore.FileInstanceStore(config_WorkflowInstanceParams);
                    //}
                    m_InstanceStore = WorkflowActivator.GetScope().ServiceProvider.GetService<InstanceStore>();
                }

                return m_InstanceStore;
            }
        }

        private WorkflowInstanceManager StartWorkflow_todelete(Activity workflow)
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

    public class WorkflowInstanceManager
    {
        AutoResetEvent s_idleEvent = null;
        AutoResetEvent s_completedEvent = null;
        AutoResetEvent s_unloadedEvent = null;
        AutoResetEvent s_syncEvent = null;

        WorkflowInvokeMode invokeMode = WorkflowInvokeMode.None;

        internal System.Activities.Runtime.DurableInstancing.InstanceStore instanceStore = null;
        Activity workflow = null;

        private void setWFEvents (WorkflowApplication wf)
        {
            wf.Idle = delegate (WorkflowApplicationIdleEventArgs e)
            {
                Console.WriteLine("Workflow idled");
                s_idleEvent.Set();
                //s_syncEvent.Set();
            };

            wf.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Console.WriteLine("Workflow completed with state {0}.", e.CompletionState.ToString());

                if (e.TerminationException != null)
                {
                    Console.WriteLine("TerminationException = {0}; {1}", e.TerminationException.GetType().ToString(), e.TerminationException.Message);
                }
                s_completedEvent.Set();
                //s_syncEvent.Set();
            };

            wf.Unloaded = delegate (WorkflowApplicationEventArgs e)
            {
                Console.WriteLine("Workflow unloaded");
                if (this.invokeMode == WorkflowInvokeMode.Run)
                {
                    this.invokeMode = WorkflowInvokeMode.ResumeBookmark;
                }
                else
                {
                    this.invokeMode = WorkflowInvokeMode.None;
                }

                s_unloadedEvent.Set();
                //s_syncEvent.Set();
            };


            wf.PersistableIdle = delegate (WorkflowApplicationIdleEventArgs e)
            {
                if (instanceStore != null)
                {
                    return PersistableIdleAction.Unload;
                }

                return PersistableIdleAction.None;
            };

            wf.Aborted = e => 
            {
                int a = 0;
                //syncEvent.Set(); 
                s_syncEvent.Set();
            };
            //wfApp.Idle = e => { /*syncEvent.Set();*/ };
            //wfApp.PersistableIdle = e => { return PersistableIdleAction.Unload; };
            //wfApp.OnUnhandledException = e => { return UnhandledExceptionAction.Terminate; };

        }

        public WorkflowInstanceManager(Activity workflow)
        {
            this.workflow = workflow;
        }

        private void resetEvents()
        { 
            s_idleEvent = new AutoResetEvent(false);
            s_completedEvent = new AutoResetEvent(false);
            s_unloadedEvent = new AutoResetEvent(false);
        }

        private WorkflowApplication createWorkflowApplication(WorkflowInstanceContext instanceContext)
        {
            WorkflowApplication wfApp = new WorkflowApplication(this.workflow);
            wfApp.InstanceStore = this.instanceStore;
            
            wfApp.Extensions.Add<WorkflowInstanceContext>(() =>
            {
                return instanceContext;
            });
            setWFEvents(wfApp);

            return wfApp;
        }

        public TResponse Execute2<TRequest, TResponse>(TRequest request)
        {
            resetEvents();

            Action<WorkflowApplication> setWFEvents = delegate (WorkflowApplication wf)
            {
                wf.Idle = delegate (WorkflowApplicationIdleEventArgs e)
                {
                    Console.WriteLine("Workflow idled");
                    s_idleEvent.Set();
                };

                wf.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
                {
                    Console.WriteLine("Workflow completed with state {0}.", e.CompletionState.ToString());

                    if (e.TerminationException != null)
                    {
                        Console.WriteLine("TerminationException = {0}; {1}", e.TerminationException.GetType().ToString(), e.TerminationException.Message);
                    }
                    s_completedEvent.Set();
                };

                wf.Unloaded = delegate (WorkflowApplicationEventArgs e)
                {
                    Console.WriteLine("Workflow unloaded");
                    s_unloadedEvent.Set();
                };

                wf.PersistableIdle = delegate (WorkflowApplicationIdleEventArgs e)
                {
                    if (instanceStore != null)
                    {
                        return PersistableIdleAction.Unload;
                    }

                    return PersistableIdleAction.None;
                };
            };

            Action waitOne = delegate ()
            {
                if (instanceStore != null)
                {
                    s_unloadedEvent.WaitOne();
                }
                else
                {
                    s_idleEvent.WaitOne();
                }
            };

            TResponse respnse = default(TResponse);

            Dictionary<string, object> inputs = new Dictionary<string, object>();
            inputs.Add("Request", request);
            WorkflowApplication wfApp = new WorkflowApplication(this.workflow, inputs);

            setWFEvents(wfApp);

            wfApp.Run();

            waitOne();

            return respnse;
        }

        public TResponse Execute<TRequest, TResponse>(TRequest request) 
        {
            TResponse respnse = default(TResponse);

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            //WorkflowInstanceContext<TRequest, TResponse> instanceContext = new WorkflowInstanceContext<TRequest, TResponse>()
            WorkflowInstanceContext instanceContext = new WorkflowInstanceContext()
            {
                Request = request,
                Response = default(TResponse)
            };
            

            Dictionary<string, object> inputs = new Dictionary<string, object>();
            //inputs.Add("Request", request);

            WorkflowApplication wf = new WorkflowApplication(this.workflow, inputs);
            wf.Extensions.Add<WorkflowInstanceContext>( () => 
            { 
                return instanceContext; 
            });

            wf.Completed = e => { syncEvent.Set(); };
            wf.Unloaded = e => { syncEvent.Set(); };
            wf.Aborted = e => { syncEvent.Set(); };
            wf.Idle = e => { syncEvent.Set(); };
            wf.PersistableIdle = e => { return PersistableIdleAction.None; };
            wf.OnUnhandledException = e => { return UnhandledExceptionAction.Terminate; };

            wf.Run();

            syncEvent.WaitOne();

            TResponse response = default(TResponse);

            try
            {
                response = (TResponse)instanceContext.Response;
            }
            catch
            { 
            }

            return response;
        }

        public TResponse StartWorkflow<TRequest, TResponse>(TRequest request, string OperationName)
        {
            TimeSpan timeOut = TimeSpan.FromMinutes(1);

            Action waitOne = delegate ()
            {
                s_syncEvent = null;
                if (instanceStore != null)
                {
                    s_syncEvent = s_unloadedEvent;
                    s_unloadedEvent.WaitOne();
                }
                else
                {
                    s_syncEvent = s_idleEvent;
                    s_idleEvent.WaitOne();
                }
            };

            WorkflowInstanceContext instanceContext = new WorkflowInstanceContext()
            {
                Request = request,
                Response = default(TResponse)
            };
            
            invokeMode = WorkflowInvokeMode.Run;

            WorkflowApplication wfApp = null;
            Guid wfId = Guid.Empty;

            while (invokeMode != WorkflowInvokeMode.None)
            {
                if (invokeMode == WorkflowInvokeMode.Run) 
                {
                    wfApp = createWorkflowApplication(instanceContext);
                    wfId = wfApp.Id;
                    resetEvents();
                    wfApp.Run(timeOut);
                    waitOne();
                }
                else if (invokeMode == WorkflowInvokeMode.ResumeBookmark)
                {
                    wfApp = createWorkflowApplication(instanceContext);
                    resetEvents();
                    wfApp.Load(wfId, timeOut);
                    var isWaiting = wfApp.GetBookmarks().FirstOrDefault(b => b.BookmarkName == OperationName);
                    if (isWaiting != null)
                    {
                        wfApp.ResumeBookmark(OperationName, "bookmark data", timeOut);
                        waitOne();
                    }
                    else
                    {
                        throw new Exception($"Bookmark {OperationName} missing on workflow with id {wfApp.Id}");
                    }
                }
            }; 
            

            TResponse response = default(TResponse);

            try
            {
                response = (TResponse)instanceContext.Response;
            }
            catch
            {
            }

            return response;
        }
    }

    public class WorkflowInstanceContext
    {
        public object Request { get; set; }

        public object Response { get; set; }
    }

    public class WorkflowInstanceContext<TRequest, TResponse> : WorkflowInstanceContext
    {
        public new TRequest Request { get; set; }

        public new TResponse Response { get; set; }

    }

    public enum WorkflowInvokeMode
    {
        UnKnow,
        None,
        Run,
        ResumeBookmark
    }
}
