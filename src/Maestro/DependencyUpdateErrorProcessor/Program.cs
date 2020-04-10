// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Reflection;
using Maestro.Data;
using Microsoft.DncEng.Configuration.Extensions;
using Microsoft.Dotnet.GitHub.Authentication;
using Microsoft.DotNet.GitHub.Authentication;
using Microsoft.DotNet.ServiceFabric.ServiceHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Status.Web.Controllers;
using Octokit;

namespace DependencyUpdateErrorProcessor
{
    public static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            ServiceHost.Run(
                host =>
                {
                    host.RegisterStatefulService<DependencyUpdateErrorProcessor>("DependencyUpdateErrorProcessorType");
                    host.ConfigureServices(Configure);
                });
        }

        public static void Configure(IServiceCollection services)
        {
            services.AddDefaultJsonConfiguration();
            services.AddBuildAssetRegistry((provider, options) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                options.UseSqlServer(config.GetSection("BuildAssetRegistry")["ConnectionString"]);
            });
            services.AddGitHubTokenProvider();
            services.Configure<GitHubClientOptions>(o =>
            {
                o.ProductHeader = new ProductHeaderValue("Maestro", Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion);
            });
            services.Configure<GitHubTokenProviderOptions>("GitHub", (o, s) => s.Bind(o));
            services.Configure<DependencyUpdateErrorProcessorOptions>(
                (options, provider) =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    options.IsEnabled = bool.Parse(config["EnableDependencyUpdateErrorProcessor"]);
                    options.GithubUrl = config["GithubUrl"];
                    options.FyiHandle = config["FyiHandle"];
                });
            services.AddSingleton<GitHubClientFactory>();
        }
    }
}