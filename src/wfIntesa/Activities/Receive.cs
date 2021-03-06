﻿using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfIntesa.Activities
{
    public class Receive_todelete : NativeActivity
    { 
        protected string operationName = string.Empty;

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        public Receive_todelete()
        {
            this.operationName = "Submit";
        }
        public Receive_todelete(string operationName)
        {
            this.operationName = operationName;
        }

        //[RequiredArgument]
        public InArgument<string> OperationName { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            string bookmarkName = this.operationName;// OperationName.Get(context);
            context.CreateBookmark(bookmarkName, new BookmarkCallback(BookmarkCallback), BookmarkOptions.MultipleResume);
        }

        private void BookmarkCallback(NativeActivityContext context, Bookmark bookmark, object bookmarkData)
        {
            context.RemoveBookmark(bookmark.Name);
        }
    }

    public class Receive_todelete<TRequest> : Receive_todelete // NativeActivity
    {
        public Receive_todelete():base()
        {
            
        }
        public Receive_todelete(string operationName):base(operationName)
        {
            
        }
        public OutArgument<TRequest> Request { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            string bookmarkName = this.operationName;// OperationName.Get(context);
            context.CreateBookmark(bookmarkName, new BookmarkCallback(BookmarkCallback), BookmarkOptions.MultipleResume);
        }

        private void BookmarkCallback(NativeActivityContext context, Bookmark bookmark, object bookmarkData)
        {
            var icontext = context.GetExtension<WorkflowInstanceContext>();
            //icontext.Response = bookmarkData;

            if (icontext != null && icontext.Request != null)
                Request.Set(context, (TRequest)icontext.Request);

            //if (bookmarkData != null && bookmarkData.ToString() == this.operationName)
            //{
            //}
                
            context.RemoveBookmark(bookmark.Name);

            
        }



    }
}
