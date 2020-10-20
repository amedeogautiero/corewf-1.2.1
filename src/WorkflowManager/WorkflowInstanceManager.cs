using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace System.Activities
{
    public class WorkflowInstanceManager
    {
        AutoResetEvent s_idleEvent = null;
        AutoResetEvent s_completedEvent = null;
        AutoResetEvent s_unloadedEvent = null;
        AutoResetEvent s_syncEvent = null;

        WorkflowInvokeMode invokeMode = WorkflowInvokeMode.None;

        internal System.Activities.Runtime.DurableInstancing.InstanceStore instanceStore = null;
        Activity workflow = null;

        internal Guid WorkflowId = Guid.Empty;

        private Exception exception = null;

        private void setWFEvents(WorkflowApplication wf)
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
                this.exception = e.Reason;
                //throw e.Reason;
                
                //syncEvent.Set(); 
                s_syncEvent?.Set();
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
            //wfApp.InstanceStore = WorkflowActivator.GetScope().ServiceProvider.GetService< System.Activities.Runtime.DurableInstancing.InstanceStore>();
            //if ((wfApp.InstanceStore is IStoreCorrelation)
            //    && (wfApp.InstanceStore as IStoreCorrelation).Correlation != null)
            //{
            //    (wfApp.InstanceStore as IStoreCorrelation).Correlation.WorkflowId = wfApp.Id;
            //}

            wfApp.Extensions.Add<WorkflowInstanceContext>(() =>
            {
                return instanceContext;
            });


            wfApp.Extensions.Add<string>(() =>
            {
                return Guid.Empty.ToString();
            });

            //Xml.Linq.XName WFInstanceScopeName = Xml.Linq.XName.Get("test123", "<namespace>");
            //wfApp.AddInitialInstanceValues(new Dictionary<Xml.Linq.XName, object>() { { "WorkflowHostTypeName", WFInstanceScopeName } });

            setWFEvents(wfApp);

            return wfApp;
        }

        private Guid correlate(WorkflowApplication wfApp)
        {
            //TODO: da completare
            if (wfApp.InstanceStore is IStoreCorrelation)
            {
                var scorrelation = (wfApp.InstanceStore as IStoreCorrelation);
                
                //if (scorrelation.Correlation != null 
                //    && scorrelation.Correlation.WorkflowId != Guid.Empty)
                //{
                //    return scorrelation.Correlation.WorkflowId;
                //}
                //else if(scorrelation.Correlation != null
                //    && scorrelation.Correlation.CorrelationId != Guid.Empty
                //    && scorrelation.Correlation.WorkflowId == Guid.Empty)
                //{
                //    return scorrelation.Correlation.WorkflowId;
                //}
            }

            return Guid.Empty;
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

        private TResponse execute<TRequest, TResponse>(TRequest request, string OperationName)
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

            Action correlate = delegate ()
            {
                if (instanceStore is IStoreCorrelation)
                { 
                    (instanceStore as IStoreCorrelation).Correlate();
                    this.WorkflowId = (instanceStore as IStoreCorrelation).Correlation.WorkflowId;
                }
            };

            WorkflowInstanceContext instanceContext = new WorkflowInstanceContext()
            {
                Request = request,
                Response = default(TResponse)
            };

            WorkflowApplication wfApp = null;
            //Guid wfId = Guid.Empty;

            while (invokeMode != WorkflowInvokeMode.None)
            {
                if (invokeMode == WorkflowInvokeMode.Run)
                {
                    wfApp = createWorkflowApplication(instanceContext);
                    this.WorkflowId = wfApp.Id;
                    (wfApp.InstanceStore as IStoreCorrelation).Correlation.WorkflowId = wfApp.Id;
                    resetEvents();
                    wfApp.Run(timeOut);
                    waitOne();
                }
                else if (invokeMode == WorkflowInvokeMode.ResumeBookmark)
                {
                    wfApp = createWorkflowApplication(instanceContext);
                    resetEvents();
                    //this.WorkflowId = (wfApp.InstanceStore as IStoreCorrelation).Correlation.WorkflowId;
                    correlate();
                    wfApp.Load(this.WorkflowId, timeOut);
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

                if (exception != null)
                    throw exception;
            };

            TResponse response = default(TResponse);

            try
            {
                response = (TResponse)instanceContext.Response;
            }
            catch (Exception exc)
            {
                throw exc;
            }

            return response;
        }

        private TResponse StartWorkflow_todelete<TRequest, TResponse>(TRequest request, string OperationName)
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
            //Guid wfId = Guid.Empty;

            while (invokeMode != WorkflowInvokeMode.None)
            {
                if (invokeMode == WorkflowInvokeMode.Run)
                {
                    wfApp = createWorkflowApplication(instanceContext);
                    this.WorkflowId = wfApp.Id;
                    resetEvents();
                    wfApp.Run(timeOut);
                    waitOne();
                }
                else if (invokeMode == WorkflowInvokeMode.ResumeBookmark)
                {
                    wfApp = createWorkflowApplication(instanceContext);
                    resetEvents();
                    wfApp.Load(this.WorkflowId, timeOut);
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

        private TResponse ContinueWorkflow_todelete<TRequest, TResponse>(TRequest request, string OperationName)
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

            invokeMode = WorkflowInvokeMode.ResumeBookmark;

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
                    this.WorkflowId = (wfApp.InstanceStore as IStoreCorrelation).Correlation.CorrelationId;
                    wfApp.Load(this.WorkflowId, timeOut);
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

        public TResponse StartWorkflow<TRequest, TResponse>(TRequest request, string OperationName)
        {
            invokeMode = WorkflowInvokeMode.Run;
            return execute<TRequest, TResponse>(request, OperationName);
        }

        public TResponse ContinueWorkflow<TRequest, TResponse>(TRequest request, string OperationName)
        {
            invokeMode = WorkflowInvokeMode.ResumeBookmark;
            return execute<TRequest, TResponse>(request, OperationName);
        }
    }
}
