using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Activities
{
    public class Receive<TRequest> : NativeActivity
    {
        private string operationName = string.Empty;

        public Receive()
        {
            this.operationName = "Submit";
        }

        public Receive(string operationName)
        {
            this.operationName = operationName;
        }

        //[RequiredArgument]
        public InArgument<string> OperationName { get; set; }


        public OutArgument<TRequest> Request { get; set; }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        //protected override void CacheMetadata(NativeActivityMetadata metadata)
        //{
        //    metadata.AddImplementationVariable(_iteration);
        //}

        protected override void Execute(NativeActivityContext context)
        {
            string bookmarkName = this.operationName;// OperationName.Get(context);
            context.CreateBookmark(bookmarkName, new BookmarkCallback(BookmarkCallback), BookmarkOptions.MultipleResume);
        }

        private void BookmarkCallback(NativeActivityContext context, Bookmark bookmark, object bookmarkData)
        {
            var icontext = context.GetExtension<WorkflowInstanceContext>();
            //icontext.Response = bookmarkData;
            Request.Set(context, (TRequest)icontext.Request);
            //if (bookmarkData != null && bookmarkData.ToString() == this.operationName)
            //{
            //}
                
            context.RemoveBookmark(bookmark.Name);

            
        }



    }
}
