using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inso.Els.AspNetCore.Tests
{
    public class ServiceCollectionTests
    {
        [Fact]
        public void AddEls_RegistersClientAsSingleton()
        {
            var services = new ServiceCollection();
            services.AddEls(b =>
            {
                b.Endpoint = "https://els.example";
                b.ApiKey = "k";
                b.AppSlug = "svc";
            });

            using var provider = services.BuildServiceProvider();
            var client1 = provider.GetRequiredService<IElsClient>();
            var client2 = provider.GetRequiredService<IElsClient>();
            client1.Should().BeSameAs(client2);

            client1.SessionId.Should().StartWith("els-");
        }

        [Fact]
        public void AddEls_FromConfiguration_BindsOptions()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Els:Endpoint"] = "https://els.example",
                    ["Els:ApiKey"] = "k",
                    ["Els:AppSlug"] = "svc",
                    ["Els:MinLevel"] = "warning",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddEls(config.GetSection("Els"));

            using var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IElsClient>();
            client.Should().NotBeNull();
        }
    }
}
