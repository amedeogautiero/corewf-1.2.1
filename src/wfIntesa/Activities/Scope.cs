using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Activities
{
    public class Scope<TRequest> : NativeActivity<TRequest>
    {
        public Scope()
        {
            this.Variables = new Collection<Variable>();
        }

        public Collection<Variable> Variables { get; set; }

        public Activity Body { get; set; }

        public InOutArgument<TRequest> Request { get; set; } 

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddChild(this.Body);
            //metadata.AddVariable(this.RequestVariable);
            

            //this.Variables.Add(this.RequestVariable);

            if (this.Variables != null)
            { 
                metadata.SetVariablesCollection(this.Variables);
            }


            //metadata.AddImplementationVariable(this.RequestVariable);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Request", typeof(TRequest), ArgumentDirection.InOut));
            metadata.Bind(this.Request, runtimeArguments[0]);


            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            var req = Request.Get(context);

            //this.Result.Set(context, req);

            //this.RequestVariable.Set(context, req);

            var v_request = this.Variables.FirstOrDefault(v => v.Name == "request");
            if (v_request != null)
            {
                v_request.Set(context, req);
            }


            int a = 0;
            //this.Result.Set(context, req);

            //context.SetValue(this.To, req);


            //if (this.RequestVariable != null)
            //{
            //    this.Result.Set(context, req);
            //    //this.RequestVariable.Set(context, req);
            //    //context.SetValue(this.Request, this.RequestVariable.Get(context));
            //    //context.SetValue(this.RequestVariable, req);
            //    //context.SetValue(this.Result, this.RequestVariable.Get(context));
            //}
            //OutRequest.Set(context, req);

            if (this.Body != null)
                context.ScheduleActivity(this.Body, new CompletionCallback(OnBodyComplete));
        }

        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            //context.SetValue(this.Result, this.ResultVariable.Get(context));
            int a = 0;
        }
    }
}
