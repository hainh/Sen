using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public class BaseScheduleGrain : Grain
    {
        public static readonly TimeSpan INFINITE_TIMESPAN = TimeSpan.FromMilliseconds(-1);
        /// <summary>
        /// Schedule a callback to call once after <paramref name="dueTime"/>.
        /// <para>
        /// See also <seealso cref="Grain.RegisterTimer(Func{object, Task}, object, TimeSpan, TimeSpan)"/>
        /// </para>
        /// </summary>
        /// <param name="asyncCallback">Callback to call</param>
        /// <param name="state">State to pass to callback</param>
        /// <param name="dueTime">Time to wait before callback execution</param>
        /// <returns>A disposable handler to cancel the schedule</returns>
        protected IDisposable Schedule(Func<object, Task> asyncCallback, object state, TimeSpan dueTime)
        {
            return RegisterTimer(asyncCallback, state, dueTime, INFINITE_TIMESPAN);
        }

        /// <summary>
        /// Schedule a callback to call in interval.
        /// <para>
        /// See also <seealso cref="Grain.RegisterTimer(Func{object, Task}, object, TimeSpan, TimeSpan)"/>
        /// </para>
        /// </summary>
        /// <param name="asyncCallback">Callback to call</param>
        /// <param name="state">State to pass to callback</param>
        /// <param name="dueTime">Time to wait before first callback execution</param>
        /// <param name="period">Time to repeat subsequence callback</param>
        /// <returns>A disposable handler to cancel the schedule</returns>
        protected IDisposable ScheduleInterval(Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            return RegisterTimer(asyncCallback, state, dueTime, period);
        }
    }
}
