﻿using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Activities
{
    public class SendReplay_todelete<TResponse> : NativeActivity
    {
        public InArgument<TResponse> Response { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var n = this.DisplayName;
            var icontext = context.GetExtension<WorkflowInstanceContext>();

            icontext.Response = this.Response.Get(context);
        }
    }
}
