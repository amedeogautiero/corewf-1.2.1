using System;
using System.Activities;
using System.Activities.Runtime.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.Activities
{
    public class SendReplay<TResponse> : NativeActivity
    {
        private bool OvirrideResponse = false;
        public InArgument<TResponse> Response { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var n = this.DisplayName;
            var icontext = context.GetExtension<WorkflowInstanceContext>();

            if (!icontext.responsed)
            {
                icontext.Response = this.Response.Get(context);
                icontext.responsed = true;
            }
            //icontext.Response = this.Response.Get(context);
        }
    }
}
