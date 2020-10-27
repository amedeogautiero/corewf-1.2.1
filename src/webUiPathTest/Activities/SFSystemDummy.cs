using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.Activities.Local
{
    public class SFSystemDummy : Activity
    {
        public InArgument<string> Operation { get; set; }
        public InArgument<decimal> Actual { get; set; }

        public InArgument<decimal> Number { get; set; }

        public OutArgument<decimal> Result { get; set; }

        public ActivityDelegate Delegate1 { get; set; }

        public SFSystemDummy()
        {

            this.Implementation = () =>
                new Sequence()
                {
                    Activities =
                    {
                        new Switch<string>()
                        {
                            Expression = new System.Activities.Expressions.ArgumentValue<string> { ArgumentName = "Operation" },
                            Cases =
                            {
                                {
                                    "+", new Assign<decimal>()
                                    {
                                        To = new System.Activities.Expressions.ArgumentReference<decimal> { ArgumentName = "Result" },
                                        Value = 12
                                    }
                                },
                                {
                                    "-",
                                    new Sequence()
                                    {
                                        Activities =
                                        {
                                            new Assign<decimal>()
                                            {
                                                To = new System.Activities.Expressions.ArgumentReference<decimal> { ArgumentName = "Result" },
                                                Value = 7
                                            },
                                            new InvokeDelegate()
                                            {
                                                Delegate = Delegate1,
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
        }
    }
}
