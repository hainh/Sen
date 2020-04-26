using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Utilities.Concurrency
{
    public static class TaskExtension
    {
        public static Task SyncContinueWith<T>(this Task<T> task, Action<Task<T>> action)
        {
            return task.ContinueWith(action, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
