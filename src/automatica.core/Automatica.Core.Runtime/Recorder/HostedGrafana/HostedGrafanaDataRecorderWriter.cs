﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder.Base;
using Automatica.Core.Runtime.Telegraf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegraf.Infux.Client.Impl;

namespace Automatica.Core.Runtime.Recorder.HostedGrafana
{
    internal class HostedGrafanaDataRecorderWriter : BaseDataRecorderWriter
    {
        private readonly ISettingsCache _settingsCache;
        private readonly INodeInstanceCache _nodeCache;

        private TelegrafInfuxClient? _client;

        public HostedGrafanaDataRecorderWriter(IConfiguration config, ISettingsCache settingsCache, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.HostedGrafanaRecorder, "HostedGrafanaDataRecorderWriter", nodeCache, dispatcher, factory)
        {
            _settingsCache = settingsCache;
            _nodeCache = nodeCache;
        }

        public override Task Start()
        {
            var host = _settingsCache.GetByKey("hostedGrafanaHost").ValueText + "/api/v1/push/influx/write";
            var apiKey = _settingsCache.GetByKey("hostedGrafanaApiKey").ValueText;
            var userId = _settingsCache.GetByKey("hostedGrafanaUserId").ValueText;


            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(userId))
            {
                Logger.LogError($"Host, UserId or ApiKey is empty, cannot start {nameof(HostedGrafanaDataRecorderWriter)}");
                return Task.CompletedTask;
            }

            var uri = new Uri(host);
            var disposingWaitTaskTimeout = TimeSpan.FromSeconds(30);
            var httpTimeout = TimeSpan.FromSeconds(15);

            var httpChannel = new InternalHttpTelegrafChannel(uri, disposingWaitTaskTimeout, httpTimeout, authorizationHeaderValue: $"{userId}:{apiKey}");

            _client = new TelegrafInfuxClient(httpChannel, new Dictionary<string, string>());

            return base.Start();
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            var name = MetricName[nodeInstance.ObjId];

            Logger?.LogDebug($"Saving value for {name}: {trend.Value}");

            _client?.Send(name, f => f.Field("value", Math.Round(trend.Value, 2)), t => t.Tag("nodeId", nodeInstance.ObjId.ToString().Replace("-", "_")).Tag("source", trend.Source).Tag("name", nodeInstance.Name));
            return Task.CompletedTask;
        }
    }
}
