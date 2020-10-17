using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Activities
{
    public class SendReplay<TResponse> : NativeActivity
    {
        public InArgument<TResponse> Response { get; set; }

        //protected override void CacheMetadata(NativeActivityMetadata metadata)
        //{
        //    metadata.AddImplementationVariable(_iteration);
        //}

        protected override void Execute(NativeActivityContext context)
        {
            var icontext = context.GetExtension<WorkflowInstanceContext>();

            icontext.Response = this.Response.Get(context);
        }
    }
}
