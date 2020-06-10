// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McMaster.AspNetCore.Kestrel.Certificates;
using Microsoft.Extensions.Hosting;

namespace LettuceEncrypt.Internal
{
    internal class StartupCertificateLoader : IHostedService
    {
        private readonly IEnumerable<ICertificateSource> _certSources;
        private readonly IServerCertificateSelector _selector;

        public StartupCertificateLoader(
            IEnumerable<ICertificateSource> certSources,
            IServerCertificateSelector selector)
        {
            _certSources = certSources;
            _selector = selector;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var certSource in _certSources)
            {
                var certs = await certSource.GetCertificatesAsync(cancellationToken);

                foreach (var cert in certs)
                {
                    _selector.Add(cert);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
