﻿using System;
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
using wfIntesa.Messages;

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

        public static void Test(int num, int tot)
        {
            Console.WriteLine("Test.....");
            int a = 0;
        }

        public static void Test(decimal str)
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
            wf.Completed = e => { printOutputs(e.Outputs); syncEvent.Set(); };
            wf.Unloaded = e => { syncEvent.Set(); };
            wf.Aborted = e => { syncEvent.Set(); };
            wf.Idle = e => { syncEvent.Set(); };
            wf.PersistableIdle = e => { return PersistableIdleAction.None; };
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


            //string config_WorkflowInstanceStore = wfIntesa.Startup.config["WorkflowInstanceStore:StoreType"];
            //string config_WorkflowInstanceParams = wfIntesa.Startup.config["WorkflowInstanceStore:InstanceParamsString"];
            //if (!string.IsNullOrEmpty(config_WorkflowInstanceStore))
            //{ 
            //    s_fileStore = new JsonFileInstanceStore.FileInstanceStore(config_WorkflowInstanceParams);
            //}

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

                Variable<Messages.SFSystemCalculatorStartResponse> v_response1 = new Variable<Messages.SFSystemCalculatorStartResponse>();

                Sequence workflow = new Sequence()
                {
                    Variables = { v_result, v_resultInt, v_remainder, v_request, v_response, v_response1 },
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
                        new Assign<SFSystemCalculatorStartResponse>()
                        {
                            To = v_response1,
                            Value =new InArgument<SFSystemCalculatorStartResponse>(e => new SFSystemCalculatorStartResponse()
                            {
                                 Started = true,
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
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.NewGuid() },
                Workflow = getWorkflowDefinition(),
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

        public static string sample_pick1(HttpContext context)
        {
            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                //Correlation = new WorkflowCorrelation() { CorrelationId = Guid.Parse("7777b449-410c-49d9-b29b-b37419d0895a") /*Guid.NewGuid()*/ },
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.NewGuid() },
                Workflow = WorkflowDefinitions.workflow_pick1(),
            };

            var response = manager.StartWorkflow<RequestBase, bool>(workflowDefinition, null, "Start");

            if (response)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<form action='/step' method='post'>");
                sb.Append($"<input type='hidden' name='correlationid' value='{workflowDefinition.Correlation.CorrelationId}' />");
                sb.Append("<input type='hidden' name='step' value='step_pick1_somma' />");
                sb.Append("<input type='text' name='numero'/>");
                sb.Append("<input type='submit' value='submit'/>");
                sb.Append("</form>");
                return sb.ToString();
            }

            return "Qualcosa è andato storto";
        }

        public static string step_pick1_somma(HttpContext context)
        {
            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                Workflow = WorkflowDefinitions.workflow_pick1(),
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.Parse(context.Request.Form["correlationid"]) },
            };
            int numero = int.Parse(context.Request.Form["numero"]);

            var response = manager.ContinueWorkflow<int, bool>(workflowDefinition, numero, "Somma");

            if (response)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<form action='/step' method='post'>");
                sb.Append($"<input type='hidden' name='correlationid' value='{workflowDefinition.Correlation.CorrelationId}' />");
                sb.Append("<input type='hidden' name='step' value='step_pick1_somma' />");
                sb.Append("<input type='text' name='numero'/>");
                sb.Append("<input type='submit' value='Somma'/>");
                sb.Append("</form></br></br>");

                sb.Append("<form action='/step' method='post'>");
                sb.Append($"<input type='hidden' name='correlationid' value='{workflowDefinition.Correlation.CorrelationId}' />");
                sb.Append("<input type='hidden' name='step' value='step_pick1_fine' />");
                sb.Append("<input type='submit' value='Fine'/>");
                sb.Append("</form></br></br>");

                return sb.ToString();
            }

            return "Qualcosa è andato storto";
        }

        public static string step_pick1_fine(HttpContext context)
        {
            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                Workflow = WorkflowDefinitions.workflow_pick1(),
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.Parse(context.Request.Form["correlationid"]) },
            };

            var response = manager.ContinueWorkflow<RequestBase, int>(workflowDefinition, null, "Fine");

            StringBuilder sb = new StringBuilder();
            sb.Append($"Totale: {response}</br></br>");
            return sb.ToString();
        }

        public static string sample_dynamic1(HttpContext context)
        {
            var actual = new InArgument<decimal>(0);
            var number = new InArgument<decimal>(0);
            var operation = new InArgument<string>("-");
            var result = new InArgument<decimal>(0);

            Variable<decimal> v_result = new Variable<decimal>();

            Sequence Workflow = new Sequence()
            {
                Variables = { v_result },
                Activities =
                {
                    new SFSystemDummy()
                    {
                        Operation = operation,
                        Result = new OutArgument<decimal>(v_result),
                        Delegate1 = new ActivityAction()
                        {
                            Handler = new InvokeMethod()
                            {
                                MethodName = nameof(Samples.Test),
                                TargetType = typeof(Samples),
                                Parameters = { new InArgument<decimal>(123) }
                            }
                        }
                    },
                    new InvokeMethod()
                    {
                        MethodName = nameof(Samples.Test),
                        TargetType = typeof(Samples),
                        Parameters = { new InArgument<decimal>(env => v_result.Get(env)) }
                    }
                }
            };

            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                //Correlation = new WorkflowCorrelation() { CorrelationId = Guid.Parse("7777b449-410c-49d9-b29b-b37419d0895a") /*Guid.NewGuid()*/ },
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.NewGuid() },
                Workflow = Workflow, //WorkflowDefinitions.workflow_dynamic1(),
            };

            var response = manager.StartWorkflow<RequestBase, bool>(workflowDefinition, null, "Start");

            return null;
        }

        public static string sample_SFSystemCalculator(HttpContext context)
        {
            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.NewGuid() },
                Workflow = WorkflowDefinitions.workflow_SFSystemCalculator(),
            };

            var response = manager.StartWorkflow<SFSystemCalculatorStartRequest, SFSystemCalculatorStartResponse>(workflowDefinition, null, "Start");

            StringBuilder sb = new StringBuilder();

            if (response != null && response.Started)
            {
                sb.Append("<script>");
                sb.Append("function setOpt(op){document.getElementById('op').value = op.value;}");
                sb.Append("</script>");

                sb.Append($"Workflow SFSystemCalculator partito</br></br>");
                sb.Append("<form action='/step' method='post'>");
                sb.Append($"<input type='hidden' name='correlationid' value='{workflowDefinition.Correlation.CorrelationId}' />");
                sb.Append("<input type='hidden' name='step' value='step_SFSystemCalculator_continue' />");
                sb.Append("<input type='hidden' name='op' id='op'/>");

                sb.Append("<div>");
                sb.Append("<div><input type='text' name='number' value='0'/></div>");
                sb.Append("<div><input onclick='setOpt(this);' type='submit' value='+' style='width:30px;' /> <input onclick='setOpt(this);' type='submit' value='-' style='width:30px;'/></div>");
                sb.Append("<div><input onclick='setOpt(this);' type='submit' value='*' style='width:30px;' /> <input onclick='setOpt(this);' type='submit' value=':' style='width:30px;'/></div>");
                sb.Append("</div>");

                sb.Append("</form>");
            }

            return sb.ToString();
        }
        public static string step_SFSystemCalculator_continue(HttpContext context)
        {
            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                Workflow = WorkflowDefinitions.workflow_SFSystemCalculator(),
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.Parse(context.Request.Form["correlationid"]) },
            };
            int numero = 0;
            int.TryParse(context.Request.Form["number"], out numero);
            
            var request = new SFSystemCalculatorOpRequest()
            {
                Number = numero,
                Op = context.Request.Form["op"]
            };

            var response = manager.ContinueWorkflow<SFSystemCalculatorOpRequest, SFSystemCalculatorOpResponse>(workflowDefinition, request, "continue");

            StringBuilder sb = new StringBuilder();

            if (response != null && response.Risultato.Esito && response.Executed)
            {
                sb.Append("<script>");
                sb.Append("function setOpt(op){document.getElementById('op').value = op.value;}");
                sb.Append("</script>");

                sb.Append($"Workflow SFSystemCalculator calcolo eseguito</br></br>");
                sb.Append("<form action='/step' method='post'>");
                sb.Append($"<input type='hidden' name='correlationid' value='{workflowDefinition.Correlation.CorrelationId}' />");
                sb.Append("<input type='hidden' name='step' value='step_SFSystemCalculator_continue' />");
                sb.Append("<input type='hidden' name='op' id='op'/>");
                sb.Append("<div>");
                sb.Append("<div><input type='text' name='number' value='0'/></div>");
                sb.Append("<div><input onclick='setOpt(this);' type='submit' value='+' style='width:30px;' /> <input onclick='setOpt(this);' type='submit' value='-' style='width:30px;'/></div>");
                sb.Append("<div><input onclick='setOpt(this);' type='submit' value='*' style='width:30px;' /> <input onclick='setOpt(this);' type='submit' value=':' style='width:30px;'/></div>");
                sb.Append("</div>");
                sb.Append("</form>");
                sb.Append("<form action='/step' method='post'>");
                sb.Append($"<input type='hidden' name='correlationid' value='{workflowDefinition.Correlation.CorrelationId}' />");
                sb.Append("<input type='hidden' name='step' value='step_SFSystemCalculator_end' />");
                sb.Append("<input onclick='setOpt(this);' type='submit' value='Fine'/> ");
                sb.Append("</form>");
            }
            else if (response != null && !response.Risultato.Esito)
            {
                sb.Append($"<p style='color:red'>{response.Risultato.Message}</p>");
            }
                
            return sb.ToString(); 
        }

        public static string step_SFSystemCalculator_end(HttpContext context)
        {
            WorkflowDefinition workflowDefinition = new WorkflowDefinition()
            {
                Workflow = WorkflowDefinitions.workflow_SFSystemCalculator(),
                Correlation = new WorkflowCorrelation() { CorrelationId = Guid.Parse(context.Request.Form["correlationid"]) },
            };

            var request = new SFSystemCalculatorEndRequest();
            var response = manager.ContinueWorkflow<SFSystemCalculatorEndRequest, SFSystemCalculatorEndResponse>(workflowDefinition, request, "end");

            StringBuilder sb = new StringBuilder();

            if (response != null)
            {
                sb.Append($"Il risultato e': { response.Result}");
            }

            return sb.ToString();
        }
    }

    
}
