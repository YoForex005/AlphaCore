using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TraderIntelligence.Fix.CTrader.Services;

/// <summary>
/// Ensures there is a single active instance allowed to place/accept execution intents
/// for the TRADE FIX session.
///
/// In production, the lock implementation should be backed by Redis and return a
/// monotonically increasing fencing token (to prevent split-brain execution).
/// </summary>
public sealed class FixSessionOwnership
{
    public interface IDistributedLockWithFencing
    {
        /// <returns>
        /// (acquired: true if lock is held by the caller, fencingToken: monotonically increasing token)
        /// </returns>
        Task<(bool acquired, long fencingToken)> TryAcquireAsync(
            string lockKey,
            string ownerId,
            TimeSpan ttl,
            CancellationToken cancellationToken);

        Task ReleaseAsync(
            string lockKey,
            string ownerId,
            long fencingToken,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Simple fallback implementation for development/unit tests.
    /// Replace with a Redis-backed lock in real deployments.
    /// </summary>
    public sealed class InMemoryDistributedLockWithFencing : IDistributedLockWithFencing
    {
        private readonly ConcurrentDictionary<string, (string ownerId, long fencingToken, DateTimeOffset expiresAt)> _locks = new();
        private long _globalToken;

        public Task<(bool acquired, long fencingToken)> TryAcquireAsync(
            string lockKey,
            string ownerId,
            TimeSpan ttl,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTimeOffset.UtcNow;
            _locks.TryGetValue(lockKey, out var current);
            var expired = current.expiresAt != default && current.expiresAt <= now;

            if (!expired && current.ownerId != null)
                return Task.FromResult((false, current.fencingToken));

            var fencing = Interlocked.Increment(ref _globalToken);
            _locks[lockKey] = (ownerId, fencing, now.Add(ttl));
            return Task.FromResult((true, fencing));
        }

        public Task ReleaseAsync(
            string lockKey,
            string ownerId,
            long fencingToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_locks.TryGetValue(lockKey, out var current) && current.ownerId == ownerId && current.fencingToken == fencingToken)
            {
                _locks.TryRemove(lockKey, out _);
            }

            return Task.CompletedTask;
        }
    }

    private readonly IDistributedLockWithFencing _lock;
    private readonly string _lockKey;
    private readonly string _ownerId;
    private readonly TimeSpan _ttl;

    private long _fencingToken;
    private bool _hasOwnership;
    private bool _reconciled;

    public FixSessionOwnership(
        IDistributedLockWithFencing lockProvider,
        string ownerId,
        string lockKey,
        TimeSpan ttl)
    {
        _lock = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        _ownerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        _lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
        _ttl = ttl;
    }

    public bool HasOwnership => _hasOwnership;

    /// <summary>
    /// Monotonically increasing fencing token (Redis in production).
    /// </summary>
    public long FencingToken => _fencingToken;

    /// <summary>
    /// Set to true only after the caller has completed reconciliation for this owner.
    /// </summary>
    public bool ExecutionIntentsAllowed => _hasOwnership && _reconciled;

    public async Task AcquireAsync(CancellationToken cancellationToken)
    {
        var (acquired, fencing) = await _lock.TryAcquireAsync(_lockKey, _ownerId, _ttl, cancellationToken).ConfigureAwait(false);
        _hasOwnership = acquired;
        _fencingToken = fencing;
    }

    public void MarkReconciled()
    {
        _reconciled = true;
    }

    public async Task ReleaseAsync(CancellationToken cancellationToken)
    {
        if (!_hasOwnership) return;
        await _lock.ReleaseAsync(_lockKey, _ownerId, _fencingToken, cancellationToken).ConfigureAwait(false);
        _hasOwnership = false;
        _fencingToken = 0;
        _reconciled = false;
    }
}

