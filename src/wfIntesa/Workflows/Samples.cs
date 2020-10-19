using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using wfIntesa.Activities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace wfIntesa.Workflows
{
    public class Samples
    {
        static System.Activities.IWorkflowsManager manager = null;

        static Samples()
        {
            //manager = ServiceActivator.GetScope().ServiceProvider.GetService<System.Activities.IWorkflowsManager>();
            manager = WorkflowActivator.GetScope().ServiceProvider.GetService<System.Activities.IWorkflowsManager>();
        }

        public static void Test(string str)
        {
            Console.WriteLine("Test.....");
            int a = 0;
        }

        public static void Test(int str)
        {
            Console.WriteLine("Test.....");
            int a = 0;
        }

        public static string sample_request1()
        {
            Variable<string> receive = new Variable<string>();
            Sequence workflow = new Sequence()
            {
                Variables = { receive },
                Activities = { 
                    new Receive<string>()
                    {
                        OperationName = new InArgument<string>("test"),
                        Request = new OutArgument<string>(receive)   
                    },
                    new WriteLine()
                    {
                        Text = "Ciao mondo"
                    },
                    new SendReplay<string>()
                    {
                        Response = new InArgument<string>(e => receive.Get(e))
                    }
                }
            };

            //WorkflowsManager manager = new WorkflowsManager();

            // wfi = manager.StartWorkflow(workflow);
            //var response = wfi.Execute<string, string>("request in input");
            //return response;
            return null;
        }

        public static string sample_divide()
        {
            int dividend = 500;
            int divisor = 36;

            string ret = string.Empty;

            Action<IDictionary<string, object>> printOutputs = delegate (IDictionary<string, object> outArgs)
            {
                if (outArgs != null)
                {
                    ret = $"{dividend} / {divisor} = {outArgs["Result"]} Remainder {outArgs["Remainder"]}";
                }
            };

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add("Dividend", dividend);
            arguments.Add("Divisor", divisor);

            //IDictionary<string, object> outputs = null;
            //outputs = WorkflowInvoker.Invoke(new Divide(), arguments);
            //printOutputs(outputs);

            Variable<int> res = new Variable<int>();
            

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            Divide divide = new Divide()
            {
                Variables = { res },
                //Result = new OutArgument<int>(res),
                //Body = new Sequence()
                //{
                //    Activities = {
                //        //new InvokeMethod()
                //        //{
                //        //    MethodName = nameof(Samples.Test),
                //        //    TargetType = typeof(Samples),
                //        //    Parameters = { new InArgument<int>(env => res.Get(env)) }
                //        //}
                //    }
                //}
            };

            var wf = new WorkflowApplication(divide, arguments);
            wf.Completed = e => { printOutputs(e.Outputs);  syncEvent.Set(); };
            wf.Unloaded = e => { syncEvent.Set(); };
            wf.Aborted = e => { syncEvent.Set(); };
            wf.Idle = e => { syncEvent.Set(); };
            wf.PersistableIdle = e => { return PersistableIdleAction.None;  };
            wf.OnUnhandledException = e => { return UnhandledExceptionAction.Terminate; };

            wf.Run(TimeSpan.FromMinutes(1));

            syncEvent.WaitOne();

            return ret;
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
                Variable<string> request = new Variable<string>("request", "default val");
                
                //Variable<string> request = new Variable<string>
                //{
                //    Default = "Hello World.",
                //    Modifiers = VariableModifiers.Mapped,
                //};

                Scope<string> root = new Scope<string>()
                {
                    Variables = { request },
                    //Request = new InOutArgument<string>(request)
                };
                
                //root.Variables.Add(request);
                //root.Result = new OutArgument<string>(request2);

                Sequence workflow = new Sequence();
                workflow.Variables.Add(StartTime);
                

                workflow.Activities.Add(
                new Assign<DateTime>
                {
                    Value = DateTime.Now,
                    To = StartTime
                });

                //workflow.Activities.Add(
                //new Assign<string>
                //{
                //    Value = "prova",
                //    To = request2
                //});

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
                    new WriteLine
                    {
                        Text = new InArgument<string>(env => request.Get(env)),
                    });

                workflow.Activities.Add(
                    new Assign<string>
                    {
                        Value = new InArgument<string>("test value"),
                        //Value = new InArgument<string>(env => root.Request.Get(env)),
                        //Value = root.Request.Get(null),
                        To = request
                    });

                workflow.Activities.Add(
                    new InvokeMethod()
                    {
                        MethodName = nameof(Samples.Test),
                        TargetType = typeof(Samples),
                        Parameters = { new InArgument<string>(env => request.Get(env)) }
                        //Parameters = { new InArgument<string> { Expression = request } },
                        //Parameters = { root.Request },
                    });

                root.Body = workflow;

                //{
                //    Variables = { request },
                //    //Request = request,
                //    Body = workflow,
                //};

                //root.RequestVariable = request;
                //root.Variables.Add(request);

                //root.To = request;
                //root.Result = request;

                //workflow.Activities.Add(
                //new Assign<string>
                //{
                //    Value = new InArgument<string>(env => root.Request.Get(env)),
                //    To = request
                //});

                //workflow.Activities.Add(
                //new InvokeMethod()
                //{
                //    MethodName = nameof(Samples.Test),
                //    TargetType = typeof(Samples),
                //    //Parameters = { new InArgument<string> { Expression = request } },
                //    //Parameters = { root.Request },
                //});

                //Argument a1 = Argument.Create(typeof(Variable<string>), ArgumentDirection.In);
                //workflow.Activities.Add(
                //    new WriteLine
                //    {
                //        Text = new InArgument<string> { Expression = request }
                //    });



                return root;
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
            WorkflowApplication wfApp = null;

            if (bookmarkId == Guid.Empty)
            {
                Dictionary<string, object> inputs = new Dictionary<string, object>();
                inputs.Add("Request", "bookmark1 request input value");
                wfApp = new WorkflowApplication(wf, inputs);
            }
            else
            {
                wfApp = new WorkflowApplication(wf);
            }

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

        public static void sample_bookmark2()
        {
            //WorkflowsManager manager = new WorkflowsManager();

            //Variable<string> request = new Variable<string>("request", "default value");
           
            //Variable<string> var1 = new Variable<string>();

            Scope<string> workflow = new Scope<string>()
            {
                //RequestVariable = request,
                //Variables = { var1 },
                //Body = new Assign<string>
                //{
                //    Value = "prova",
                //    To = var1,
                //    //new InArgument<string> { Expression = request }
                //}
            };

            


            //var wfi = manager.StartWorkflow(workflow);
            //wfi.Execute<string, string>("request in input");

        }

        public static string sample_SendReplay1(HttpContext context)
        {
            Func<Activity> getWorkflowDefinition = delegate ()
            {
                Variable<SendReplay1Request> v_request = new Variable<SendReplay1Request>();
                Variable<SendReplay1Response> v_response = new Variable<SendReplay1Response>();
                Variable<int> v_resultInt = new Variable<int>();
                Variable<int> v_remainder = new Variable<int>();
                Variable<decimal> v_result = new Variable<decimal>();

                Sequence workflow = new Sequence()
                {
                    Variables = { v_result, v_resultInt, v_remainder, v_request, v_response },
                    Activities = {
                        new Receive<SendReplay1Request>("Submit")
                        {
                            Request = new OutArgument<SendReplay1Request>(v_request)
                        },
                        new Divide()
                        {
                            //IN
                            Dividend = new InArgument<int>(e => v_request.Get(e).Dividend),
                            Divisor  = new InArgument<int>(e => v_request.Get(e).Divisor),
                        
                            //OUT
                            Quotient = new OutArgument<int>(v_resultInt),
                            Result = new OutArgument<decimal>(v_result),
                            Remainder = new OutArgument<int>(v_remainder),
                        },
                        new Assign<SendReplay1Response>()
                        {
                            To = v_response,
                            Value =new InArgument<SendReplay1Response>(e => new SendReplay1Response()
                            {
                                 Result = v_result.Get(e),
                                 Quotient = v_resultInt.Get(e),
                                 Remainder = v_remainder.Get(e),
                            })
                        },
                        new SendReplay<SendReplay1Response>()
                        {
                            Response = v_response
                        },
                    }
                };

                return workflow;
            };

            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                Workflow = getWorkflowDefinition(),
                InstanceCorrelation = Guid.NewGuid(), //id contesto ....
            };

            SendReplay1Request request = new SendReplay1Request()
            {
                 Dividend = 500,
                 Divisor = 13,
            };
            
            var response = manager.StartWorkflow<SendReplay1Request, SendReplay1Response>(workflowDefinition, request, "Submit");

            StringBuilder sb = new StringBuilder();
            sb.Append($"{request.Dividend} / {request.Divisor} = {response.Result} or ({response.Quotient} with {response.Remainder} of Remainder )");
            return sb.ToString();
        }

        public static string sample_pick1()
        {
            Func<Activity> getWorkflowDefinition = delegate ()
            {
                Variable<int> v_totale = new Variable<int>();
                Variable<int> v_ndasommare = new Variable<int>();
                Variable<bool> v_continue = new Variable<bool>("v_fine", true);

                Sequence workflow = new Sequence()
                {
                    Variables = { v_ndasommare, v_totale , v_continue },
                    Activities = {
                        new Receive("Start")
                        {

                        },
                        new SendReplay<bool>()
                        {
                            DisplayName = "SendReplay start",
                            Response = new InArgument<bool>(true)
                        },
                        new While()
                        {
                            Condition = v_continue,
                            Body = new System.Activities.Statements.Pick()
                            {
                                Branches = {
                                    new PickBranch()
                                    {
                                        Trigger = new Receive<int>("Somma")
                                        {
                                            Request = new OutArgument<int>(v_ndasommare)
                                        },
                                        Action = new Assign()
                                        {
                                            To = new OutArgument<int>(v_totale),
                                            Value = new InArgument<int>(e => v_totale.Get(e) + v_ndasommare.Get(e) )
                                        }
                                    },
                                    new PickBranch()
                                    {
                                        Trigger = new Receive("Fine")
                                        {
                                            
                                        },
                                        Action = new Assign<bool>()
                                        {
                                            To = new OutArgument<bool>(v_continue),
                                            Value = new InArgument<bool>(true)
                                        }
                                    }
                                }
                            }
                        },
                        new SendReplay<int>()
                        {
                            DisplayName = "SendReplay totale",
                            Response = new InArgument<int>(v_totale)
                        }
                    }
                };

                return workflow;
            };

            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                Workflow = getWorkflowDefinition(),
                InstanceCorrelation = Guid.NewGuid(),
            };

            var response = manager.StartWorkflow<RequestBase, bool>(workflowDefinition, null, "Start");

            if (response)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<form action='/step'>");
                sb.Append($"<input type='hidden' name='correlationid' value='{workflowDefinition.InstanceCorrelation}' />");
                sb.Append("<input type='hidden' name='step' value='step_pick1_somma' />");
                sb.Append("<input type='text' name='numero'/>");
                sb.Append("<input type='button' value='submit'/>");
                sb.Append("</form>");
                return sb.ToString();
            }

            return "Qualche cosa è andato storto";
        }
    }

    public class RequestBase
    { }

    public class ResponseBase
    { }


    public class SendReplay1Request : RequestBase
    {
        public int Dividend { get; set; }

        public int Divisor { get; set; }
    }

    public class SendReplay1Response : ResponseBase
    {
        public int Quotient { get; set; }

        public decimal Result { get; set; }

        public int Remainder { get; set; }
    }
}
