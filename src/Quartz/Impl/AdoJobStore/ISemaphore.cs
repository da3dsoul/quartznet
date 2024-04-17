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

namespace Quartz.Impl.AdoJobStore;

/// <summary>
/// An interface for providing thread/resource locking in order to protect
/// resources from being altered by multiple threads at the same time.
/// </summary>
/// <author>James House</author>
/// <author>Marko Lahma (.NET)</author>
public interface ISemaphore
{
    /// <summary>
    /// Grants a write lock on the identified resource to the calling thread (blocking
    /// until it is available).
    /// </summary>
    /// <returns> true if the lock was obtained.
    /// </returns>
    ValueTask<bool> ObtainWriteLock(
        Guid requestorId,
        ConnectionAndTransactionHolder? conn,
        string lockName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a read lock on the identified resource to the calling thread (blocking
    /// until it is available).
    /// </summary>
    /// <returns> true if the lock was obtained.
    /// </returns>
    ValueTask<bool> ObtainReadLock(
        Guid requestorId,
        ConnectionAndTransactionHolder? conn,
        string lockName,
        CancellationToken cancellationToken = default);

    /// <summary> Release the write lock on the identified resource if it is held by the calling
    /// thread.
    /// </summary>
    ValueTask ReleaseWriteLock(
        Guid requestorId,
        string lockName,
        CancellationToken cancellationToken = default);

    /// <summary> Release the read lock on the identified resource if it is held by the calling
    /// thread.
    /// </summary>
    ValueTask ReleaseReadLock(
        Guid requestorId,
        string lockName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this Semaphore implementation requires a database connection for
    /// its lock management operations.
    /// </summary>
    /// <seealso cref="ObtainWriteLock" />
    /// <seealso cref="ReleaseWriteLock" />
    bool RequiresConnection { get; }
}