using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegraf.Channel;

namespace Automatica.Core.Runtime.Telegraf
{
    public class InternalHttpTelegrafChannel : ITelegrafChannel
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _uri;
        private readonly int _httpTimeout;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(HttpTelegrafChannel).FullName);
        private readonly ConcurrentDictionary<Guid, Task> _tasks = new ConcurrentDictionary<Guid, Task>();
        private bool _disposed;
        private bool _disposing;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly TimeSpan _disposingWaitTaskTimeout;
        private readonly string? _authorizationHeaderValue;

        public bool SupportsBatchedWrites => true;

        public InternalHttpTelegrafChannel(
            Uri uri,
            TimeSpan disposingWaitTaskTimeout,
            TimeSpan? httpTimeout = null,
            string? authorizationHeaderValue = null)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            httpTimeout = httpTimeout ?? TimeSpan.FromSeconds(15);

            _httpTimeout = (int)httpTimeout.Value.TotalMilliseconds;
            _disposingWaitTaskTimeout = disposingWaitTaskTimeout;

            if(authorizationHeaderValue != null)
                _authorizationHeaderValue = authorizationHeaderValue;

            _httpClient = new HttpClient
            {
                // the configured timeout was never applied (default of HttpClient is 100s)
                Timeout = TimeSpan.FromMilliseconds(_httpTimeout)
            };

            if (!String.IsNullOrEmpty(_authorizationHeaderValue))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authorizationHeaderValue);
            }
        }

        public void Write(string metric)
        {
            if (string.IsNullOrWhiteSpace(metric))
                return;

            if (_disposing || _disposed)
                return;

            var taskId = Guid.NewGuid();

            // Task.Run unwraps the async WriteInternal, StartNew returned a Task<Task> that completed at the first await,
            // so Dispose never waited for the http requests
            var task = Task.Run(() => WriteInternal(metric, taskId), _cancellationTokenSource.Token);

            _tasks.TryAdd(taskId, task);
            // removed after it was added, also if the task completes before TryAdd
            task.ContinueWith(static (_, state) =>
                {
                    var (tasks, id) = ((ConcurrentDictionary<Guid, Task>, Guid))state!;
                    tasks.TryRemove(id, out _);
                }, (_tasks, taskId), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        public Task WriteAsync(string metric)
        {
            Write(metric);

            return Task.CompletedTask;
        }

        internal async Task WriteInternal(string metric, Guid taskId)
        {
            try
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    return;

                using var content = new StringContent(metric, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(_uri, content, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();
            }
            catch (WebException e)
            {
                _logger.Error(e, $"status:{e.Status}, metric:{metric}");
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
            {
                // disposed
            }
            catch (Exception e)
            {
                _logger.Error(e, $"metric:{metric}");
            }
        }

        internal IEnumerable<Task> GetTasks()
        {
            var tasks = _tasks.Where(t => t.Value != null && t.Value.IsCompleted == false).Select(t => t.Value);

            return tasks;
        }

        public void Dispose()
        {
            if (_disposed || _disposing)
                return;

            _disposing = true;

            try
            {
                _logger.Info($"Waiting for completion of {GetTasks().Count()} tasks, maximum waiting time: {_disposingWaitTaskTimeout:g}");

                var tasks = GetTasks().ToArray();

                var completed = Task.WaitAll(tasks, _disposingWaitTaskTimeout);

                if (completed == false)
                    _logger.Warn($"During '{_disposingWaitTaskTimeout:g}' {tasks.Count(t => t.IsCompleted)} from  {tasks.Length} tasks were completed");
                else
                    _logger.Info("All tasks completed");

                _cancellationTokenSource.Cancel();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
