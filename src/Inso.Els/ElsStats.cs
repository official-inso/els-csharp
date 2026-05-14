namespace Inso.Els
{
    /// <summary>
    /// Runtime metrics for an <see cref="IElsClient"/>. All counters are
    /// updated atomically and safe to read concurrently.
    /// </summary>
    public readonly struct ElsStats
    {
        /// <summary>Total entries accepted into the in-memory queue.</summary>
        public long Enqueued { get; }

        /// <summary>Entries dropped because the queue was full (oldest are dropped first).</summary>
        public long Dropped { get; }

        /// <summary>Entries successfully delivered to the server.</summary>
        public long Sent { get; }

        /// <summary>Entries that failed to send and were buffered to disk (or lost if disk write also failed).</summary>
        public long Failed { get; }

        /// <summary>Entries dropped by the sampling filter.</summary>
        public long Sampled { get; }

        /// <summary>Current size of the on-disk buffer in bytes.</summary>
        public long BufferedBytes { get; }

        /// <summary>Creates a stats snapshot.</summary>
        public ElsStats(long enqueued, long dropped, long sent, long failed, long sampled, long bufferedBytes)
        {
            Enqueued = enqueued;
            Dropped = dropped;
            Sent = sent;
            Failed = failed;
            Sampled = sampled;
            BufferedBytes = bufferedBytes;
        }
    }
}
