using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Local;
using System.Activities.Runtime;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Messages;

namespace webUiPathTest.Workflows
{
    public class WorkflowDefinitions
    {
        public static void OpNotPermitted(string op, int number)
        {
            int a = 0;   
        }

        public static Activity workflow_pick1()
        {
            Variable<int> v_totale = new Variable<int>("v_totale", 0);
            Variable<int> v_ndasommare = new Variable<int>();
            Variable<bool> v_continue = new Variable<bool>("v_continue", true);

            Sequence workflow = new Sequence()
            {
                Variables = { v_ndasommare, v_totale, v_continue },
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
                                        Action = new Sequence()
                                        {
                                            Activities = {
                                                new Assign()
                                                {
                                                    To = new OutArgument<int>(v_totale),
                                                    Value = new InArgument<int>(e => v_totale.Get(e) + v_ndasommare.Get(e) )
                                                },
                                                new InvokeMethod()
                                                {
                                                    MethodName = nameof(Samples.Test),
                                                    TargetType = typeof(Samples),
                                                    Parameters = 
                                                    {
                                                        new InArgument<int>(env => v_ndasommare.Get(env))
                                                        ,new InArgument<int>(env => v_totale.Get(env)) 
                                                    }
                                                },
                                                new SendReplay<bool>()
                                                {
                                                    Response = new InArgument<bool>(true)
                                                }
                                            }
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
                                            Value = new InArgument<bool>(false)
                                        }
                                    }
                                }
                            }
                        },
                        new SendReplay<int>()
                        {
                            DisplayName = "SendReplay pick1 totale",
                            Response = new InArgument<int>(v_totale)
                        }
                    }
            };

            Sequence workflow2 = new Sequence()
            {
                Variables = { v_ndasommare, v_totale, v_continue },
                Activities = {
                    new Sequence()
                    { 
                        Activities = { 
                            new Receive("Start")
                            { 
                            },
                            new SendReplay<bool>()
                            {
                                Response = new InArgument<bool>(true)
                            },
                            new While()
                            {
                                Condition = v_continue,
                                Body = new Pick()
                                {
                                    Branches = {
                                        new PickBranch()
                                        {
                                            Trigger = new Receive("Somma")
                                            {
                                            },
                                            Action = new SendReplay<bool>()
                                            {
                                                Response = new InArgument<bool>(true)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            };


            return workflow;
        }

        public static Activity workflow_dynamic1()
        {
            //https://docs.microsoft.com/it-it/dotnet/api/system.activities.dynamicactivity?view=netframework-4.8

            //var numbers = new InArgument<List<int>>();
            //var average = new OutArgument<double>();
            var actual = new InArgument<decimal>(0);
            var number = new InArgument<decimal>(0);
            var operation = new InArgument<string>(); 
            var result = new InArgument<decimal>(0);

            var v_number = new Variable<decimal>();


            return new DynamicActivity()
            {
                Properties =
                {
                    // Input argument
                    new DynamicActivityProperty
                    {
                        Name = "Actual",
                        Type = actual.GetType(),
                        Value = actual,
                    },
                    new DynamicActivityProperty
                    {
                        Name = "Number",
                        Type = number.GetType(),
                        Value = number,
                    },
                    new DynamicActivityProperty
                    {
                        Name = "Operation",
                        Type = operation.GetType(),
                        Value = operation,
                    },

                    // Output argument
                    new DynamicActivityProperty
                    {
                        Name = "Result",
                        Type = result.GetType(),
                        Value = result
                    }
                },
                Implementation = () =>
                    new Sequence()
                    {
                        Activities = {
                            
                            new Switch<string>()
                            {
                                Expression = new System.Activities.Expressions.ArgumentValue<string> { ArgumentName = "Operation" },
                                Cases = 
                                {
                                    {
                                        "+", new Assign<decimal>()
                                        {
                                            To = new System.Activities.Expressions.ArgumentReference<decimal> { ArgumentName = "Result" },
                                            Value = new InArgument<decimal>(env => actual.Get(env) + number.Get(env))
                                        }
                                    }
                                }
                            }
                        }
                    }
            };
        }

        public static Activity workflow_SFSystemCalculator()
        {
            Activity workflow;

            DelegateInArgument<string> a_argument1 = new DelegateInArgument<string>("Argument1");
            DelegateInArgument<int> a_argument2 = new DelegateInArgument<int>("Argument2");

            #region Variables
            Variable<Messages.SFSystemCalculatorStartRequest> v_request1 = new Variable<Messages.SFSystemCalculatorStartRequest>();
            Variable<Messages.SFSystemCalculatorStartResponse> v_response1 = new Variable<Messages.SFSystemCalculatorStartResponse>();

            Variable<Messages.SFSystemCalculatorOpRequest> v_OPrequest = new Variable<Messages.SFSystemCalculatorOpRequest>();
            

            Variable<int> v_totale = new Variable<int>("v_totale", 0);
            Variable<int> v_ndasommare = new Variable<int>();
            Variable<bool> v_continue = new Variable<bool>("v_continue", true);

            Variable<SendReplay1Response> v_response = new Variable<SendReplay1Response>();
            #endregion

            var operatioNotPermittedDelegate = new ActivityAction<string, int>()
            {
                Argument1 = a_argument1,
                Argument2 = a_argument2,
                Handler = new InvokeMethod()
                {
                    MethodName = nameof(WorkflowDefinitions.OpNotPermitted),
                    TargetType = typeof(WorkflowDefinitions),
                    Parameters =
                    {
                        new InArgument<string>(e => a_argument1.Get(e))
                        ,new InArgument<int>(e => a_argument2.Get(e))
                    }
                }
            };

           

            var sf = new SFSystemCalculator()
            {
                OperatioNotPermittedDelegate = new ActivityAction<string, int>()
                {
                    Argument1 = a_argument1,
                    Argument2 = a_argument2,

                    #region fuffa
                    //Handler = new Sequence()
                    //{
                    //    Activities =
                    //    {
                    //        new InvokeMethod()
                    //        {
                    //            MethodName = nameof(WorkflowDefinitions.OpNotPermitted),
                    //            TargetType = typeof(WorkflowDefinitions),
                    //            Parameters =
                    //            {
                    //                new InArgument<string>(e => a_argument1.Get(e))
                    //                ,new InArgument<int>(e => a_argument2.Get(e))
                    //            }
                    //        },
                    //        new SendReplay<Messages.SFSystemCalculatorOpResponse>()
                    //        {
                    //            Response = new InArgument<SFSystemCalculatorOpResponse>(e => new SFSystemCalculatorOpResponse() { Risultato = new Risultato() { Esito = false, Message = "Operazione non possibile" } })
                    //        }
                    //    }
                    //}
                    #endregion

                    Handler = SequenceHelper.Instance()
                                .InvokeMethod(new Action<string, int>(WorkflowDefinitions.OpNotPermitted))
                                    .SetParam<string>(new InArgument<string>(e => a_argument1.Get(e)))
                                    .SetParam<int>(new InArgument<int>(e => a_argument2.Get(e)))
                                .SendReplay<Messages.SFSystemCalculatorOpResponse>(e => new SFSystemCalculatorOpResponse() { Risultato = new Risultato() { Esito = false, Message = "Operazione non possibile" } })
                            .Sequence()
                    
                }
            };

            Sequence mainSequence = new Sequence()
            {
                Variables = { v_totale, v_ndasommare, v_continue, v_request1, v_response1, v_OPrequest },
                Activities = {
                        new Receive<Messages.SFSystemCalculatorStartRequest>("Start")
                        {
                            Request = new OutArgument<Messages.SFSystemCalculatorStartRequest>(v_request1)
                        },
                        new Assign()
                        {
                            To = new OutArgument<bool>(v_continue),
                            Value = new InArgument<bool>(e => true)
                        },
                        new Assign()
                        {
                            To = new OutArgument<Messages.SFSystemCalculatorStartResponse>(v_response1),
                            Value = new InArgument<Messages.SFSystemCalculatorStartResponse>(e => new Messages.SFSystemCalculatorStartResponse(){ Started = true })
                        },
                        //Helpers.Assign<bool>(v_continue, true),
                        //Helpers.Assign<Messages.SFSystemCalculatorStartResponse>(v_response1, new Messages.SFSystemCalculatorStartResponse(){ Started = true }),
                        new SendReplay<Messages.SFSystemCalculatorStartResponse>()
                        {
                            DisplayName = "SendReplay start",
                            Response = new InArgument<Messages.SFSystemCalculatorStartResponse>(v_response1)
                        },
                        sf
                }
            };

            var mainSequence2 = SequenceHelper.Instance(v_totale, v_ndasommare, v_continue, v_request1, v_response1, v_OPrequest)
                    .Receive<SFSystemCalculatorStartRequest>("Start", v_request1)
                    .Assign<bool>(v_continue, true)
                        .Set(a => a.DisplayName = "Assign continue")
                    .Assign<Messages.SFSystemCalculatorStartResponse>(v_response1, new Messages.SFSystemCalculatorStartResponse() { Started = true })
                    .SendReplay<Messages.SFSystemCalculatorStartResponse>(v_response1)
                    .Activity(sf)
            .Sequence();

            workflow = mainSequence2;

            return workflow;
        }

        
    }
}
