using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Activities
{
    public class SFSystemCalculator : Activity
    {
        public static void Test(decimal tot)
        {
        }

        public static void TestOP(string op)
        {
        }

        public ActivityDelegate OperatioNotPermittedDelegate { get; set; }

        public OutArgument<decimal> Result { get; set; }

        public SFSystemCalculator()
        {
            #region Variables

            Variable<Messages.SFSystemCalculatorOpRequest> v_OPrequest = new Variable<Messages.SFSystemCalculatorOpRequest>();

            Variable<decimal> v_totale = new Variable<decimal>("v_totale", 0);
            Variable<int> v_number = new Variable<int>();
            Variable<bool> v_continue = new Variable<bool>("v_continue", true);

            #endregion

            //Switch<string> @switch = new Switch<string>()
            Func<Activity> @switch = delegate ()
            {
                return new Switch<string>()
                {
                    //Expression = new System.Activities.Expressions.ArgumentValue<string> { ArgumentName = "Operation" },
                    Expression = new InArgument<string>(e => v_OPrequest.Get(e).Op),
                    Cases =
                    {
                        {
                            "+", new Assign<decimal>()
                            {
                                DisplayName = "Assign +",
                                To = new OutArgument<decimal>(v_totale),
                                Value = new InArgument<decimal>(env => v_totale.Get(env) + v_OPrequest.Get(env).Number)
                            }
                        },
                        {
                            "-", new Assign<decimal>()
                            {
                                DisplayName = "Assign -",
                                To = new OutArgument<decimal>(v_totale),
                                Value = new InArgument<decimal>(env => v_totale.Get(env) - v_OPrequest.Get(env).Number)
                            }
                        },
                        {
                            "*", new Assign<decimal>()
                            {
                                DisplayName = "Assign *",
                                To = new OutArgument<decimal>(v_totale),
                                Value = new InArgument<decimal>(env => v_totale.Get(env) * v_OPrequest.Get(env).Number)
                            }
                        },
                        {
                            ":", new If()
                            {
                                Condition = new InArgument<bool>(e => v_OPrequest.Get(e).Number != 0),
                                Then = new Assign<decimal>()
                                {
                                    DisplayName = "Assign :",
                                    To = new OutArgument<decimal>(v_totale),
                                    Value = new InArgument<decimal>(env => v_totale.Get(env) / v_OPrequest.Get(env).Number)
                                },
                                Else = new InvokeDelegate()
                                {
                                    Delegate = this.OperatioNotPermittedDelegate,
                                    DelegateArguments =
                                    {
                                        { "Argument1", new InArgument<string>(e => v_OPrequest.Get(e).Op) }
                                        ,{ "Argument2", new InArgument<int>(e => v_OPrequest.Get(e).Number) }
                                    }
                                }
                            }
                        }
                    },
                    Default = new InvokeMethod()
                    {
                        MethodName = nameof(SFSystemCalculator.TestOP),
                        TargetType = typeof(SFSystemCalculator),
                        Parameters =
                        {
                            new InArgument<string>(env => v_OPrequest.Get(env).Op)
                        }
                    }
                };
            };

            this.Implementation = () =>
                new Sequence()
                {
                    Variables = { v_OPrequest , v_totale , v_number , v_continue },
                    Activities =
                    {
                        new Assign<bool>()
                        { 
                            To = new OutArgument<bool>(v_continue),
                            Value = new InArgument<bool>(true),
                        },
                        new While()
                        {
                            Condition = v_continue,
                            Body = new Pick()
                            {
                                Branches = {
                                    new PickBranch()
                                    {
                                        Trigger = new Receive<Messages.SFSystemCalculatorOpRequest>("continue")
                                        {
                                            Request = new OutArgument<Messages.SFSystemCalculatorOpRequest>(v_OPrequest)
                                        },
                                        Action = new Sequence()
                                        {
                                            Activities = 
                                            {
                                                @switch(),
                                                new SendReplay<Messages.SFSystemCalculatorOpResponse>()
                                                {
                                                    Response = new InArgument<Messages.SFSystemCalculatorOpResponse>(e => new Messages.SFSystemCalculatorOpResponse(){ Executed = true })
                                                }
                                            }
                                        }
                                    },
                                    new PickBranch()
                                    {
                                        Trigger = new Receive("end")
                                        {

                                        },
                                        Action = new Sequence()
                                        {
                                            Activities = {
                                                new Assign<bool>()
                                                {
                                                    To = new OutArgument<bool>(v_continue),
                                                    Value = new InArgument<bool>(e => false)
                                                },
                                                new SendReplay<Messages.SFSystemCalculatorEndResponse>()
                                                {
                                                    Response = new InArgument<Messages.SFSystemCalculatorEndResponse>(e => new Messages.SFSystemCalculatorEndResponse()
                                                    {
                                                         Result = v_totale.Get(e)
                                                    })
                                                }
                                            }
                                        },
                                    },
                                    new PickBranch()
                                    {
                                        Trigger = new Delay()
                                        {
                                            Duration = TimeSpan.FromSeconds(10)
                                        },
                                        Action = new Sequence()
                                        {
                                            Activities =
                                            { 
                                                new Assign<bool>()
                                                {
                                                    To = new OutArgument<bool>(v_continue),
                                                    Value = new InArgument<bool>(e => false)
                                                },
                                                new SendReplay<Messages.SFSystemCalculatorOpResponse>()
                                                {
                                                    Response = new InArgument<Messages.SFSystemCalculatorOpResponse>(e => new Messages.SFSystemCalculatorOpResponse()
                                                    {
                                                          Executed = false,
                                                          Risultato = new Messages.Risultato()
                                                          {
                                                              Esito = false,
                                                              Message = "TimeOut"
                                                          }
                                                    })
                                                }
                                            }
                                        }
                                        
                                    }
                                }
                            }
                        },
                    }
                };
        }
    }
}
