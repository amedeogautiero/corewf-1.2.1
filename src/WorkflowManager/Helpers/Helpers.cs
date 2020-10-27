using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using wfIntesa.Activities;

namespace System.Activities.Runtime
{
    public class Helpers
    {
        public static InvokeMethod InvokeMethod (Delegate @delegate, params InArgument[] args)
        {
            InvokeMethod im = null;

            if (@delegate != null
                && @delegate.Method != null
                && @delegate.Method.IsStatic)
            {
                im = new InvokeMethod()
                {
                    MethodName = @delegate.Method.Name,
                    TargetType = @delegate.Method.DeclaringType,
                    //Parameters = args
                };

                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        im.Parameters.Add(arg);
                    }
                }
            }


            return im;
        }

        public static Activity Assign<T>(Variable<T> variable, T value)
        {
            return new Assign<T>()
            {
                To = new OutArgument<T>(variable),
                Value = new InArgument<T>(e => value)
            };
        }

        public static Activity Receive<T>(string operation, Variable<T> variable)
        {
            return new Receive<T>(operation)
            {
                Request = new OutArgument<T>(variable)
            };
        }

        public static Activity SendReplay<T>(Variable<T> variable)
        {
            return new SendReplay<T>()
            {
                Response = new InArgument<T>(variable)
            };
        }

        public static Activity SendReplay<T>(Func<ActivityContext, T> expression)
        {
            T value = default(T);

            //if (expression != null)
            //{
            //    value = expression();
            //}

            return new SendReplay<T>()
            {
                Response = new InArgument<T>(e => expression(e))
            };
        }

        public static Activity InvokeMethod(Action action, params InArgument[] arguments)
        {
            var im = new InvokeMethod()
            {
                MethodName = action.Method.Name,
                TargetType = action.Method.DeclaringType,
            };

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    im.Parameters.Add(argument);
                }
            }

            return im;
        }
        //{
        //    MethodName = nameof(WorkflowDefinitions.OpNotPermitted),
        //    TargetType = typeof(WorkflowDefinitions),
        //    Parameters =
        //    {
        //        new InArgument<string>(e => a_argument1.Get(e))
        //        ,new InArgument<int>(e => a_argument2.Get(e))
        //    }
    }

    public static class Extentions
    {
        public static SequenceHelper Set(this SequenceHelper sequence, Action<Activity> lastActivity)
        {
            if (lastActivity != null && sequence.Last != null)
            {
                lastActivity.Invoke(sequence.Last);
            }

            return sequence;
        }
        public static SequenceHelper Activity(this SequenceHelper sequence, Activity activity)
        {
            sequence.AddActivity(activity);
            return sequence;
        }
        public static SequenceHelper Assign<T>(this SequenceHelper sequence, Variable<T> variable, T value)
        {
            Activity activity = Helpers.Assign<T>(variable, value);
            sequence.Activity(activity);
            return sequence;
        }

        public static SequenceHelper Receive<T>(this SequenceHelper sequence, string operation, Variable<T> variable)
        {
            Activity activity = Helpers.Receive<T>(operation, variable);
            sequence.Activity(activity);
            return sequence;
        }

        public static SequenceHelper SendReplay<T>(this SequenceHelper sequence, Variable<T> variable)
        {
            Activity activity = Helpers.SendReplay<T>(variable);
            sequence.Activity(activity);
            return sequence;
        }

        public static SequenceHelper SendReplay<T>(this SequenceHelper sequence, Func<ActivityContext, T> expression)
        {
            Activity activity = Helpers.SendReplay<T>(expression);
            sequence.Activity(activity);
            return sequence;
        }

        public static SequenceHelper<InvokeMethod> InvokeMethod(this SequenceHelper sequence, Delegate action)
        {
            Activity activity = Helpers.InvokeMethod(action);
            sequence.Activity(activity);
            return new SequenceHelper<InvokeMethod>(sequence);
        }

        public static SequenceHelper<InvokeMethod> SetParam<T>(this SequenceHelper<InvokeMethod> sequence, InArgument<T> argument)
        {
            sequence.Last.Parameters.Add(argument);
            return sequence;
        }
    }

    public class SequenceHelper
    {
        //internal List<Activity> activities = new List<Activity>();
        //internal Variable[] variables = null;
        internal Sequence sequence = null;
        protected SequenceHelper(Variable[] variables):this()
        {
            //this.variables = variables;
            this.sequence = new Sequence();

            if (variables != null)
            {
                foreach (Variable variable in variables)
                {
                    sequence.Variables.Add(variable);
                }
            }

        }

        protected SequenceHelper()
        {
            if (this.sequence == null)
            {
                this.sequence = new Sequence();
            }
        }

        internal void AddActivity(Activity activity)
        {
            //this.activities.Add(activity);
            this.sequence.Activities.Add(activity);
        }

        internal Activity Last
        {
            get
            {
                //return activities.LastOrDefault();
                return this.sequence.Activities.LastOrDefault();
            }
        }

        public static SequenceHelper Instance(params Variable[] variables)
        {
            return new SequenceHelper(variables);
        }

        public Sequence Sequence()
        {
            return sequence;
        }
    }

    public class SequenceHelper<TActivity> : SequenceHelper where TActivity:System.Activities.Activity
    {
        public SequenceHelper(SequenceHelper @base) : base()
        {
            //this.activities = @base.activities;
            this.sequence = @base.sequence;
        }

        internal new TActivity Last
        {
            get
            {
                return (TActivity)base.Last;
            }
        }
    }

    //public class SequenceHelper<InvokeMehod> : SequenceHelper<TActivity>
    //{
    //    public SequenceHelper(SequenceHelper @base):base(@base.variables)
    //    {
    //        this.activities = @base.activities;
    //    }

        
    //}
}
