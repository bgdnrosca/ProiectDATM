using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace BluetoothChat.Helpers
{
    public class TaskAdapter : ObservableObject
    {
        #region FIELDS
        // -------------------------------------------------------------------------------------------------------

        private readonly Task m_task;
        private readonly Task m_completion;
        private readonly TimeSpan m_minimumExecutionTime;

        #endregion

        #region PROPERTIES
        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets a task representing the completion of the task.
        /// </summary>
        public Task Completion
        {
            get { return m_completion; }
        }

        /// <summary>
        /// Gets the task status.
        /// </summary>
        public TaskStatus Status
        {
            get { return m_task.Status; }
        }

        /// <summary>
        /// Gets whether the task is completed.
        /// </summary>
        public bool IsCompleted
        {
            get { return m_task.IsCompleted; }
        }

        /// <summary>
        /// Gets whether the task is executing.
        /// </summary>
        public bool IsExecuting
        {
            get { return !m_task.IsCompleted; }
        }

        /// <summary>
        /// Gets whether the task successfully completed.
        /// </summary>
        public bool IsSuccessfullyCompleted
        {
            get { return m_task.Status == TaskStatus.RanToCompletion; }
        }

        /// <summary>
        /// Gets whether the task was canceled.
        /// </summary>
        public bool IsCanceled
        {
            get { return m_task.IsCanceled; }
        }

        /// <summary>
        /// Gets whether the task faulted.
        /// </summary>
        public bool IsFaulted
        {
            get { return m_task.IsFaulted; }
        }

        /// <summary>
        /// Gets whether the task is in waiting for activation mode.
        /// </summary>
        public bool IsWaitingForActivation
        {
            get { return m_task.Status == TaskStatus.WaitingForActivation; }
        }

        /// <summary>
        /// Gets the exception that caused the task to fault. If the task is not faulted, the exception
        /// is null.
        /// </summary>
        public AggregateException Exception
        {
            get { return m_task.Exception; }
        }

        /// <summary>
        /// Gets the inner exception that caused the task to fault. If the task is not faulted, the
        /// exception is null.
        /// </summary>
        public Exception InnerException
        {
            get { return (Exception == null) ? null : Exception.InnerException; }
        }

        /// <summary>
        /// Gets the error mesage of the faulted task. If the task is not faulted, the error message
        /// is null.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return (InnerException == null) ? null : InnerException.Message;
            }
        }

        public TimeSpan MinimumExecutionTime { get { return m_minimumExecutionTime; } }

        #endregion

        #region CONSTRUCTORS
        // -------------------------------------------------------------------------------------------------------

        public TaskAdapter(Task task) : this(task, TimeSpan.Zero)
        {

        }

        public TaskAdapter(Task task, TimeSpan minimumExecutionTime)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            m_task = task;
            m_minimumExecutionTime = minimumExecutionTime;
            m_completion = task.IsCompleted ? Task.FromResult(true) : WatchTaskAsync(task);
        }


        #endregion

        #region PROTECTED METHODS
        // -------------------------------------------------------------------------------------------------------

        protected virtual void OnTaskCompleted() { }

        #endregion

        #region PRIVATE METHODS
        // -------------------------------------------------------------------------------------------------------

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await Task.WhenAll(task, Task.Delay(m_minimumExecutionTime));
            }
            catch (Exception e)
            {
                //Error
            }

            RaisePropertyChanged(() => Status);
            RaisePropertyChanged(() => IsCompleted);
            RaisePropertyChanged(() => IsExecuting);
            RaisePropertyChanged(() => IsCanceled);
            RaisePropertyChanged(() => IsFaulted);
            RaisePropertyChanged(() => Exception);
            RaisePropertyChanged(() => InnerException);
            RaisePropertyChanged(() => ErrorMessage);
            RaisePropertyChanged(() => IsSuccessfullyCompleted);

            OnTaskCompleted();
        }

        #endregion
    }
}
