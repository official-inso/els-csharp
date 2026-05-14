using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inso.Els.AspNetCore.Tests
{
    public class ConfigurationParseTests
    {
        [Fact]
        public void Bind_AcceptsSizeStrings()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Els:Endpoint"] = "https://els.example",
                    ["Els:ApiKey"] = "k",
                    ["Els:MaxBufferFileSize"] = "50MB",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddEls(config.GetSection("Els"));
            using var sp = services.BuildServiceProvider();

            sp.GetRequiredService<IElsClient>().Should().NotBeNull();
        }

        [Fact]
        public void Bind_ThrowsOnInvalidMinLevel()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Els:Endpoint"] = "https://els.example",
                    ["Els:ApiKey"] = "k",
                    ["Els:MinLevel"] = "nope",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddEls(config.GetSection("Els"));
            using var sp = services.BuildServiceProvider();

            Action act = () => sp.GetRequiredService<IElsClient>();
            act.Should().Throw<ElsConfigurationException>().WithMessage("*MinLevel*");
        }

        [Fact]
        public void Bind_ThrowsOnInvalidSizeString()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Els:Endpoint"] = "https://els.example",
                    ["Els:ApiKey"] = "k",
                    ["Els:MaxBufferFileSize"] = "huge",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddEls(config.GetSection("Els"));
            using var sp = services.BuildServiceProvider();

            Action act = () => sp.GetRequiredService<IElsClient>();
            act.Should().Throw<ElsConfigurationException>().WithMessage("*MaxBufferFileSize*");
        }

        [Fact]
        public void AddElsFromOptions_UsesIOptions()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Els:Endpoint"] = "https://els.example",
                    ["Els:ApiKey"] = "k",
                })
                .Build();

            var services = new ServiceCollection();
            services.Configure<ElsOptions>(config.GetSection("Els"));
            services.AddElsFromOptions();
            using var sp = services.BuildServiceProvider();

            sp.GetRequiredService<IElsClient>().Should().NotBeNull();
        }
    }
}
