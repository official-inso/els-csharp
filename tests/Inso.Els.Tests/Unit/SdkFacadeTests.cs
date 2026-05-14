using System;
using FluentAssertions;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class SdkFacadeTests
    {
        [Fact]
        public void IsRetryable_True_ForRetryableSendException()
        {
            var ex = new ElsSendException(500, isRetryable: true, message: "boom");
            Sdk.IsRetryable(ex).Should().BeTrue();
        }

        [Fact]
        public void IsRetryable_False_ForPermanentSendException()
        {
            var ex = new ElsSendException(400, isRetryable: false, message: "bad");
            Sdk.IsRetryable(ex).Should().BeFalse();
        }

        [Fact]
        public void IsRetryable_Unwraps_Aggregate()
        {
            var inner = new ElsSendException(503, isRetryable: true, message: "x");
            var agg = new AggregateException(inner);
            Sdk.IsRetryable(agg).Should().BeTrue();
        }

        [Fact]
        public void IsRetryable_Null_ReturnsFalse()
        {
            Sdk.IsRetryable(null).Should().BeFalse();
        }

        [Fact]
        public void Capture_BeforeInit_NoOp()
        {
            // Sanity: should not throw on a fresh process where Sdk.Init was not called.
            // (Test order may vary, so we don't assert on Current — see SdkInitTests.)
            Sdk.CaptureMessage("hello", ElsLevel.Info);
        }
    }
}
