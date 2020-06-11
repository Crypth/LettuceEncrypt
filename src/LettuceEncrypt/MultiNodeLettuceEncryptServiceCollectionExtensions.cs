// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using LettuceEncrypt;
using LettuceEncrypt.Acme;
using LettuceEncrypt.Internal;
using LettuceEncrypt.Internal.AcmeStates;
using LettuceEncrypt.Internal.IO;
using McMaster.AspNetCore.Kestrel.Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Helper methods for configuring Lettuce Encrypt services in a multi node environment.
    /// </summary>
    public static class MultiNodeLettuceEncryptServiceCollectionExtensions
    {
        /// <summary>
        /// Add services that will automatically generate HTTPS certificates for this server.
        /// By default, this uses Let's Encrypt (<see href="https://letsencrypt.org/">https://letsencrypt.org/</see>).
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ILettuceEncryptServiceBuilder AddNodeMasterLettuceEncrypt(this IServiceCollection services)
            => services.AddNodeMasterLettuceEncrypt(_ => { });

        /// <summary>
        /// Add services that will automatically generate HTTPS certificates for this server.
        /// By default, this uses Let's Encrypt (<see href="https://letsencrypt.org/">https://letsencrypt.org/</see>).
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ILettuceEncryptServiceBuilder AddNodeSlaveLettuceEncrypt(this IServiceCollection services)
            => services.AddNodeSlaveLettuceEncrypt(_ => { });

        /// <summary>
        /// Add services that will generate requests to the ACME authority in a multi node scenario.
        /// This differs from the original LettuceEcnrypt by not having dev cert loader, no default IServerCertificate and no http middleware.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure">A callback to configure options.</param>
        /// <returns></returns>
        public static ILettuceEncryptServiceBuilder AddNodeMasterLettuceEncrypt(this IServiceCollection services,
            Action<LettuceEncryptOptions> configure)
        {
            services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelOptionsSetup>();

            services.TryAddSingleton<ICertificateAuthorityConfiguration, DefaultCertificateAuthorityConfiguration>();

            services
                .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                .AddSingleton<IClock, SystemClock>()
                .AddSingleton<TermsOfServiceChecker>()
                .AddSingleton<IHostedService, StartupCertificateLoader>()
                .AddSingleton<IHostedService, AcmeCertificateLoader>()
                .AddSingleton<AcmeCertificateFactory>()
                .AddSingleton<AcmeClientFactory>()
                .AddSingleton<IHttpChallengeResponseStore, InMemoryHttpChallengeResponseStore>()
                .AddSingleton<TlsAlpnChallengeResponder>();

            services.AddSingleton<IConfigureOptions<LettuceEncryptOptions>>(s =>
            {
                var config = s.GetService<IConfiguration?>();
                return new ConfigureOptions<LettuceEncryptOptions>(options => config?.Bind("LettuceEncrypt", options));
            });

            services.Configure(configure);

            // The state machine should run in its own scope
            services.AddScoped<AcmeStateMachineContext>();

            services.AddSingleton(TerminalState.Singleton);

            // States should always be transient
            services
                .AddTransient<ServerStartupState>()
                .AddTransient<CheckForRenewalState>()
                .AddTransient<BeginCertificateCreationState>();

            return new LettuceEncryptServiceBuilder(services);
        }
        /// <summary>
        /// Add services that will generate requests to the ACME authority in a multi node scenario.
        /// This differs from the the master node that it will not it initiate communication with the acme servers. It will not try to refresh certs with ACME
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure">A callback to configure options.</param>
        /// <returns></returns>
        public static ILettuceEncryptServiceBuilder AddNodeSlaveLettuceEncrypt(this IServiceCollection services,
            Action<LettuceEncryptOptions> configure)
        {
            services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelOptionsSetup>();
            services
                .AddSingleton<IClock, SystemClock>()
                .AddSingleton<IHostedService, StartupCertificateLoader>()
                .AddSingleton<TlsAlpnChallengeResponder>();

            services.AddSingleton<IConfigureOptions<LettuceEncryptOptions>>(s =>
            {
                var config = s.GetService<IConfiguration?>();
                return new ConfigureOptions<LettuceEncryptOptions>(options => config?.Bind("LettuceEncrypt", options));
            });

            services.Configure(configure);

            return new LettuceEncryptServiceBuilder(services);
        }
    }
}
