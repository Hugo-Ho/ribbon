﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rabbit.Feign;
using Ribbon.Client;
using Ribbon.Client.Http;
using Ribbon.LoadBalancer.ConsulDiscovery;
using Steeltoe.Discovery.Consul.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClientByConsul
{
    [FeignClient(Name = "timeService", FallbackType = typeof(TimeServiceFallback))]
    public interface ITimeService
    {
        [GoGet("/time")]
        Task<DateTime> GetNowAsync();
    }

    public class TimeServiceFallback : ITimeService
    {
        #region Implementation of ITimeService

        /// <inheritdoc/>
        public Task<DateTime> GetNowAsync()
        {
            return Task.FromResult(DateTime.MinValue);
        }

        #endregion Implementation of ITimeService
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceCollection = new ServiceCollection()
                .AddOptions()
                .AddHttpClient()
                .AddSingleton<IConfiguration>(configuration)
                .AddConsulDiscoveryClient(configuration)
                .AddRibbonClient(b => b.AddHttpClient().AddConsulDiscovery());

            // use HttpClient
            {
                var services = serviceCollection.BuildServiceProvider();

                var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("timeService");
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var responseMessage = await client.GetAsync("/time");
                        var uri = responseMessage.RequestMessage.RequestUri;
                        Console.WriteLine($"From Server: {uri.Host}:{uri.Port}");
                        Console.WriteLine("Content: " + await responseMessage.Content.ReadAsStringAsync());
                        Console.ReadLine();
                    }
                }).Wait();
            }
        }
    }
}