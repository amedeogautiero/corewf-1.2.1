using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfIntesa.Activities;

namespace wfIntesa.Workflows
{
    public class WorkflowDefinitions
    {
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
                            DisplayName = "SendReplay totale",
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
    }
}
