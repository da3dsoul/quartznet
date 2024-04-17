#region License

/*
 * All content copyright Marko Lahma, unless otherwise indicated. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy
 * of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 */

#endregion

using Microsoft.Extensions.Logging;

using Quartz.Logging;

namespace Quartz.Impl.AdoJobStore;

/// <summary>
/// Internal in-memory lock handler for providing thread/resource locking in
/// order to protect resources from being altered by multiple threads at the
/// same time.
/// </summary>
/// <author>James House</author>
/// <author>Marko Lahma (.NET)</author>
internal sealed class ReaderWriterLockSemaphore : ISemaphore
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

    private readonly ILogger<ReaderWriterLockSemaphore> _logger;

    public ReaderWriterLockSemaphore()
    {
        _logger = LogProvider.CreateLogger<ReaderWriterLockSemaphore>();
    }

    /// <summary>
    /// Grants a lock on the identified resource to the calling thread (blocking
    /// until it is available).
    /// </summary>
    /// <returns>True if the lock was obtained.</returns>
    public ValueTask<bool> ObtainWriteLock(
        Guid requestorId,
        ConnectionAndTransactionHolder? conn,
        string lockName,
        CancellationToken cancellationToken = default)
    {
        var isDebugEnabled = _logger.IsEnabled(LogLevel.Debug);

        if (isDebugEnabled)
        {
            _logger.LogDebug("Lock '{LockName}' is desired by: {RequestorId}", lockName, requestorId);
            _logger.LogDebug("Lock '{LockName}' is being obtained: {RequestorId}", lockName, requestorId);
        }

        _lock.EnterWriteLock();

        if (isDebugEnabled)
        {
            _logger.LogDebug("Lock '{LockName}' given to: {RequestorId}", lockName, requestorId);
        }

        return new ValueTask<bool>(true);
    }

    /// <summary> Release the lock on the identified resource if it is held by the calling
    /// thread.
    /// </summary>
    public ValueTask ReleaseWriteLock(
        Guid requestorId,
        string lockName,
        CancellationToken cancellationToken = default)
    {
        _lock.ExitWriteLock();
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Lock '{LockName}' returned by: {RequestorId}", lockName, requestorId);
        }

        return default;
    }

    public ValueTask<bool> ObtainReadLock(Guid requestorId, ConnectionAndTransactionHolder? conn, string lockName, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        return new ValueTask<bool>(true);
    }

    public ValueTask ReleaseReadLock(Guid requestorId, string lockName, CancellationToken cancellationToken = default)
    {
        _lock.ExitReadLock();
        return default;
    }

    /// <summary>
    /// Whether this Semaphore implementation requires a database connection for
    /// its lock management operations.
    /// </summary>
    /// <value></value>
    /// <seealso cref="ObtainWriteLock"/>
    /// <seealso cref="ReleaseWriteLock"/>
    public bool RequiresConnection => false;
}