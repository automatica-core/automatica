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
        private readonly string _authorizationHeaderValue;

        public bool SupportsBatchedWrites => true;

        public InternalHttpTelegrafChannel(
            Uri uri,
            TimeSpan disposingWaitTaskTimeout,
            TimeSpan? httpTimeout = null,
            string authorizationHeaderValue = null)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            httpTimeout = httpTimeout ?? TimeSpan.FromSeconds(15);

            _httpTimeout = (int)httpTimeout.Value.TotalMilliseconds;
            _disposingWaitTaskTimeout = disposingWaitTaskTimeout;
            _authorizationHeaderValue = authorizationHeaderValue;

            _httpClient = new HttpClient();

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

            var taskId = Guid.NewGuid();

            var task = Task.Factory.StartNew(
                () => WriteInternal(metric, taskId),
                _cancellationTokenSource.Token,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            _tasks.TryAdd(taskId, task);
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

                var response = await _httpClient.PostAsync(_uri, new StringContent(metric, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (WebException e)
            {
                _logger.Error(e, $"status:{e.Status}, metric:{metric}");
            }
            catch (Exception e)
            {
                _logger.Error(e, $"metric:{metric}");
            }
            finally
            {
                _tasks.TryRemove(taskId, out _);
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

            _disposed = true;
        }
    }
}
