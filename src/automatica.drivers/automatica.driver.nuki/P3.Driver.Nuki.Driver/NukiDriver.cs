using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.Nuki.Driver.Model;
using Timer = System.Threading.Timer;

namespace P3.Driver.Nuki.Driver
{
    internal class NukiDriver : DriverNoneAttributeBase
    {
        private Timer _timer;
      
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private int _pollTime;
        private readonly List<NukiSmartLock> _smartLocks = new();

        private string _bridgeAddress;
        private string _token;
        private bool _hashedToken = true;

        private HttpClient _httpClient = new();

        public NukiDriver(IDriverContext driverContext) : base(driverContext)
        {
            _logger = driverContext.Logger;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var pollTime = GetPropertyValueInt("poll");
            _pollTime = pollTime;

            _bridgeAddress = $"http://{GetPropertyValueString("ip")}:{GetPropertyValueInt("port")}";
            _token = GetPropertyValueString("token");
            _hashedToken = GetProperty("hashed_token")?.ValueBool ?? true;

            if (!_hashedToken)
            {
                _logger.LogWarning("Nuki bridge token is sent in plain text, enable the hashed token if the bridge firmware supports it (1.13+)");
            }
            return base.Init(token);
        }

        private async void TimeElapsed(object state)
        {
            if (await _semaphore.WaitAsync(TimeSpan.FromSeconds(1)))
            {
                try
                {
                    await ReadValues();
                }
                catch (Exception ex)
                {
                    DriverContext.Logger.LogError(ex, "Error read values...");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer = new Timer(TimeElapsed, this, _pollTime * 1000, _pollTime * 1000);

            _logger.LogInformation($"Start polling every {_pollTime}s");
            await ReadValues(token);

            return await base.Start(token);
        }

      
        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await ReadValues(token);
            return true;
        }

        private async Task ReadValues(CancellationToken token = default)
        {
            _logger.LogDebug($"Poll values...");

            using var listResponse = await _httpClient.GetAsync($"{_bridgeAddress}/list?{NukiTokenAuth.CreateQuery(_token, _hashedToken)}", token);

            var response = await listResponse.Content.ReadAsStringAsync(token);
            _logger.LogDebug($"Response: {response}");

            if (!listResponse.IsSuccessStatusCode)
            {
                if (listResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized && _hashedToken)
                {
                    _logger.LogError("Nuki bridge rejected the hashed token. Check the token and the bridge clock, or disable the hashed token for bridge firmware older than 1.13");
                }
                else
                {
                    _logger.LogError($"Error reading values: {listResponse.StatusCode}");
                }
                return;
            }

            var listResponseObj = JsonConvert.DeserializeObject<List<NukiObject>>(response);

            foreach (var nuki in listResponseObj)
            {
                var nukiDriver = _smartLocks.FirstOrDefault(x => x.NukiId == nuki.NukiId);

                if (nukiDriver != null)
                {
                    nukiDriver.UpdateState(nuki);
                }
            }

        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            return base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var smartLock = new NukiSmartLock(ctx);

            _smartLocks.Add(smartLock);

            return smartLock;
        }
    }
}
