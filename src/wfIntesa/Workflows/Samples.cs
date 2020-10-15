using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using wfIntesa.Activities;

namespace wfIntesa.Workflows
{
    public class Samples
    {
        public static void Test()
        {
            Console.WriteLine("Test.....");
            int a = 0;
        }

        public static void sample_hello()
        {
            var helloWorldActivity = new Sequence()
            {
                Activities =
                {
                    new WriteLine
                    {
                        Text = "Hello World!"
                    }
                }
            };

            WorkflowInvoker.Invoke(helloWorldActivity);
        }

        public static Guid sample_bookmark1(Guid bookmarkId)
        {
            AutoResetEvent s_idleEvent = new AutoResetEvent(false);
            AutoResetEvent s_completedEvent = new AutoResetEvent(false);
            AutoResetEvent s_unloadedEvent = new AutoResetEvent(false);
            System.Activities.Runtime.DurableInstancing.InstanceStore s_fileStore = null;


            string config_WorkflowInstanceStore = wfIntesa.Startup.config["WorkflowInstanceStore:StoreType"];
            string config_WorkflowInstanceParams = wfIntesa.Startup.config["WorkflowInstanceStore:InstanceParamsString"];
            if (!string.IsNullOrEmpty(config_WorkflowInstanceStore))
            { 
                s_fileStore = new JsonFileInstanceStore.FileInstanceStore(config_WorkflowInstanceParams);
            }

            Func<Activity> createWorkflow = delegate ()
            {
                Variable<DateTime> StartTime = new Variable<DateTime>();

                Sequence workflow = new Sequence();
                
                workflow.Variables.Add(StartTime);

                workflow.Activities.Add(
                new Assign<DateTime>
                {
                    Value = DateTime.Now,
                    To = StartTime
                });

                workflow.Activities.Add(
                new WriteLine
                {
                    Text = "Before Bookmark"
                });

                workflow.Activities.Add(
                    new WriteLine
                    {
                        Text = "Before Bookmark"
                    });


                workflow.Activities.Add(new BookmarkActivity());

                workflow.Activities.Add(
                    new WriteLine
                    {
                        Text = "After Bookmark"
                    });

                workflow.Activities.Add(
                    new InvokeMethod()
                    {
                         MethodName = "Test",
                         TargetType = typeof(Samples)
                    });


                
                    return workflow;
            };

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
                    if (s_fileStore != null)
                    {
                        return PersistableIdleAction.Unload;
                    }

                    return PersistableIdleAction.None;
                };
            };

            Action waitOne = delegate ()
            {
                if (s_fileStore != null)
                {
                    s_unloadedEvent.WaitOne();
                }
                else
                {
                    s_idleEvent.WaitOne();
                }
            };

            Activity wf = createWorkflow();

            Guid workflowInstanceId = Guid.Empty;
            WorkflowApplication wfApp = new WorkflowApplication(wf);
            wfApp.InstanceStore = s_fileStore;

            setWFEvents(wfApp);

            if (bookmarkId == Guid.Empty)
            {
                
                wfApp.Run();

                waitOne();
            }
            else
            {
                wfApp.Load(bookmarkId);
                wfApp.ResumeBookmark(bookmarkId.ToString(), "bookmark data");
                
                waitOne();
            }
            
            workflowInstanceId = wfApp.Id;

            return workflowInstanceId;
        }
    }
}
