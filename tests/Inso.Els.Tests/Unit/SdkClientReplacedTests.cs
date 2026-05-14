using System;
using FluentAssertions;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    /// <summary>
    /// These tests touch the static <see cref="Sdk"/> facade. Collection
    /// fixture isolates them from other static-state tests when the runner
    /// happens to parallelize.
    /// </summary>
    [Collection("Sdk-facade")]
    public class SdkClientReplacedTests
    {
        [Fact]
        public void Init_TwiceRaisesClientReplacedWithPreviousClient()
        {
            try
            {
                IElsClient? captured = null;
                Sdk.ClientReplaced += Handler;

                Sdk.Init(new ElsOptions
                {
                    Endpoint = "https://els.example",
                    ApiKey = "k",
                    AutoFlushOnExit = false,
                });
                var first = Sdk.Current;
                first.Should().NotBeNull();

                Sdk.Init(new ElsOptions
                {
                    Endpoint = "https://els.example",
                    ApiKey = "k",
                    AutoFlushOnExit = false,
                });

                captured.Should().NotBeNull();
                captured.Should().BeSameAs(first);

                void Handler(object? sender, IElsClient previous) => captured = previous;
            }
            finally
            {
                Sdk.ClientReplaced -= delegate { };
                Sdk.Close();
            }
        }
    }

    [CollectionDefinition("Sdk-facade", DisableParallelization = true)]
    public class SdkFacadeCollection { }
}
