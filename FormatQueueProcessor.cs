using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DisAsm6502
{
    public class FormatQueueProcessor : Notifier
    {
        /// <summary>
        /// FormatQueue states
        /// </summary>
        private enum State
        {
            Stop,
            Run,
            Pause
        }

        /// <summary>
        /// Constructor
        /// Create the queue
        /// </summary>
        public FormatQueueProcessor()
        {
            FormatQueue = new Queue<Tuple<int, int>>();
        }

        private Window _owner;
        /// <summary>
        /// Owner of the Queue
        /// </summary>
        public Window Owner
        {
            get => _owner;
            set
            {
                _owner = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// View model
        /// This is from MainWindow
        /// </summary>
        private ViewModel.ViewModel View => ((MainWindow) Owner)?.View;

        private State _queueState = State.Stop;

        /// <summary>
        /// Queue state
        /// </summary>
        private State QueueState
        {
            get => _queueState;
            set
            {
                _queueState = value;
                OnPropertyChanged();
            }
        }

        private Queue<Tuple<int, int>> _formatQueue;
        /// <summary>
        /// Queue
        /// </summary>
        public Queue<Tuple<int, int>> FormatQueue
        {
            get => _formatQueue;
            set
            {
                _formatQueue = value;
                OnPropertyChanged();
            }
        }

        private Task _processQueTask;

        /// <summary>
        /// Stop the queue
        /// </summary>
        public void Stop()
        {
            if (QueueState != State.Stop)
            {
                QueueState = State.Stop;
                _processQueTask.Wait();
            }

            _processQueTask.Dispose();
            _processQueTask = null;
        }

        /// <summary>
        /// start the queue
        /// </summary>
        public void Start()
        {
            if (QueueState == State.Stop)
            {
                QueueState = State.Run;
                _processQueTask = Task.Run(ProcessFormatQueue);
            }
            else if (QueueState == State.Pause)
            {
                QueueState = State.Run;
            }
        }

        /// <summary>
        /// pause the queue
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void Pause()
        {
            switch (QueueState)
            {
                case State.Run:
                    QueueState = State.Pause;
                    break;

                case State.Stop:
                    QueueState = State.Pause;
                    _processQueTask = Task.Run(ProcessFormatQueue);
                    break;
            }
        }

        public static readonly object FormatQueueLock = new object();

        /// <summary>
        /// Process the format que
        /// </summary>
        private void ProcessFormatQueue()
        {
            const int sliceSize = 5;

            var items = new List<Tuple<int, int>>();
            do
            {
                if (QueueState == State.Run)
                {
                    lock (FormatQueueLock)
                    {
                        var count = FormatQueue.Count;
                        while (count > 0)
                        {
                            if (QueueState == State.Run)
                            {
                                var cnt = count % sliceSize;
                                var sz = cnt > 0 ? cnt : count;
                                for (var i = 0; i < sz; ++i)
                                {
                                    items.Add(FormatQueue.Dequeue());
                                }

                                Owner.Dispatcher.Invoke(() =>
                                {
                                    View.AssemblerLineCollection.FormatItems(items);
                                    View.SyncRowsLabels();
                                });
                            }

                            Thread.Sleep(5);
                            count = FormatQueue.Count;
                        }
                    }
                }
                Thread.Sleep(10);
            } while (QueueState != State.Stop);
        }
    }
}
