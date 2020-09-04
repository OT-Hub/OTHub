using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync
{
    public class TaskController
    {
        private readonly Source _source;
        private readonly ConcurrentBag<TaskControllerItem> _items = new ConcurrentBag<TaskControllerItem>();

        private class TaskControllerItem
        {
            private readonly Source _source;
            private readonly TaskRun _task;
            private readonly TimeSpan _runEveryTimeSpan;
            private DateTime _lastRunDateTime;
            private SystemStatus _systemStatus;

            internal TaskControllerItem(Source source, TaskRun task, TimeSpan runEveryTimeSpan, bool startNow)
            {
                _source = source;
                _task = task;
                _runEveryTimeSpan = runEveryTimeSpan;
                _lastRunDateTime = startNow ? DateTime.MinValue : DateTime.Now;
                _systemStatus = new SystemStatus(task.Name);
            }

            public bool NeedsRunning
            {
                get { return ((DateTime.Now - _lastRunDateTime) > _runEveryTimeSpan); }
            }

            public async Task Execute()
            {
                DateTime startTime = DateTime.Now;

                

                try
                {
                    Logger.WriteLine(_source, "Starting " + _task.Name);
                    await _task.Execute(_source);

                    try
                    {
                        using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                        {
                            _systemStatus.InsertOrUpdate(connection, true);
                        }
                    }
                    catch
                    {

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                    try
                    {
                        using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                        {
                            _systemStatus.InsertOrUpdate(connection, false);
                        }
                    }
                    catch
                    {

                    }
                }
                finally
                {
                    _lastRunDateTime = DateTime.Now;
                    Logger.WriteLine(_source, "Finished " + _task.Name + " in " + (DateTime.Now - startTime).TotalSeconds + " seconds");
                }
            }
        }

        public void Schedule(TaskRun task, TimeSpan runEveryTimeSpan, bool startNow)
        {
            var item = new TaskControllerItem(_source, task, runEveryTimeSpan, startNow);
            _items.Add(item);
        }

        private bool _showSleepingLogMessage = true;
        private bool isFirstSync = true;

        public TaskController(Source source)
        {
            _source = source;
        }

        public void Start()
        {
            while (true)
            {
                if (isFirstSync)
                {
                    isFirstSync = false;
                }

                var items = _items.Where(i => i.NeedsRunning).Reverse().ToArray();

                foreach (var taskControllerItem in items)
                {
                    taskControllerItem.Execute().GetAwaiter().GetResult();
                }

                if (!items.Any())
                {
                    if (_showSleepingLogMessage)
                    {
                        _showSleepingLogMessage = false;
                        Logger.WriteLine(_source, "Sleeping...");
                    }

                    Thread.Sleep(2000);
                }
                else
                {
                    _showSleepingLogMessage = true;
                }
            }
        }
    }
}