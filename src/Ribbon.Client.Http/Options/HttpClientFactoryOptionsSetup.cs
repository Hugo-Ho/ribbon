﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;

namespace Ribbon.Client.Http.Options
{
    public class HttpClientFactoryOptionsSetup : IConfigureNamedOptions<HttpClientFactoryOptions>
    {
        private readonly IOptionsMonitor<LoadBalancerClientOptions> _loadBalancerClientOptionsMonitor;
        private readonly ILoggerFactory _loggerFactory;

        private readonly IConfiguration _configuration;

        public HttpClientFactoryOptionsSetup(IServiceProvider services, IOptionsMonitor<LoadBalancerClientOptions> loadBalancerClientOptionsMonitor, ILoggerFactory loggerFactory)
        {
            _loadBalancerClientOptionsMonitor = loadBalancerClientOptionsMonitor;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _configuration = services.GetService<IConfiguration>();
        }

        #region Implementation of IConfigureOptions<in HttpClientFactoryOptions>

        /// <inheritdoc/>
        public void Configure(HttpClientFactoryOptions options)
        {
            Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
        }

        #endregion Implementation of IConfigureOptions<in HttpClientFactoryOptions>

        #region Implementation of IConfigureNamedOptions<in HttpClientFactoryOptions>

        /// <inheritdoc/>
        public void Configure(string name, HttpClientFactoryOptions options)
        {
            var ribbonSection = _configuration?.GetSection(name)?.GetSection("ribbon");

            var config = ribbonSection == null ? new HttpClientFactoryConfig() : ribbonSection.Get<HttpClientFactoryConfig>() ?? HttpClientFactoryConfig.Default;

            options.HttpClientActions.Add(s =>
            {
                s.Timeout = config.Timeout;
                s.BaseAddress = new Uri("http://" + name);
            });

            options.HttpMessageHandlerBuilderActions.Add(s =>
            {
                var loadBalancerClientOptions = _loadBalancerClientOptionsMonitor.Get(name);
                s
                    .AdditionalHandlers
                    .Add(new LoadBalancerClientHandler(name, loadBalancerClientOptions, _loggerFactory.CreateLogger<LoadBalancerClientHandler>()));

                s.AdditionalHandlers.Add(new SameRetryHandler(name, loadBalancerClientOptions, _loggerFactory.CreateLogger<SameRetryHandler>()));
            });
        }

        #endregion Implementation of IConfigureNamedOptions<in HttpClientFactoryOptions>
    }
}