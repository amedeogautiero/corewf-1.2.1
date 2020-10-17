using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Activities
{
    public sealed class Divide : NativeActivity
    {
        public Divide()
        {
            this.Variables = new Collection<Variable>();
        }

        [RequiredArgument]
        public InArgument<int> Dividend { get; set; }

        [RequiredArgument]
        public InArgument<int> Divisor { get; set; }

        public OutArgument<int> Remainder { get; set; }
        public OutArgument<int> Quotient { get; set; }
        public OutArgument<decimal> Result { get; set; }

        public Collection<Variable> Variables { get; set; }

        //protected override void CacheMetadata(NativeActivityMetadata metadata)
        //{
        //    metadata.AddChild(this.Body);

        //    if (this.Variables != null)
        //    {
        //        metadata.SetVariablesCollection(this.Variables);
        //    }

        //    var runtimeArguments = new Collection<RuntimeArgument>();
        //    runtimeArguments.Add(new RuntimeArgument("Dividend", typeof(int), ArgumentDirection.In));
        //    runtimeArguments.Add(new RuntimeArgument("Divisor", typeof(int), ArgumentDirection.In));
        //    metadata.Bind(this.Dividend, runtimeArguments[0]);
        //    metadata.Bind(this.Divisor, runtimeArguments[1]);
        //    metadata.SetArgumentsCollection(runtimeArguments);
        //    metadata.AddImplementationChild(Body);
        //}

        protected override void Execute(NativeActivityContext context)
        {
            int dividend = Dividend.Get(context);
            int divisor = Divisor.Get(context);
            int quotient = dividend / divisor;
            int remainder = dividend % divisor;
            decimal result = (decimal)dividend / (decimal)divisor;

            Quotient.Set(context, quotient);
            Remainder.Set(context, remainder);
            Result.Set(context, result);
        }


        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            //context.SetValue(this.Result, this.ResultVariable.Get(context));
            int a = 0;
        }
    }
}
