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

using Quartz.Spi;

namespace Quartz.Impl.AdoJobStore;

/// <summary>
/// <see cref="JobStoreTX" /> is meant to be used in a standalone environment.
/// Both commit and rollback will be handled by this class.
/// </summary>
/// <author><a href="mailto:jeff@binaryfeed.org">Jeffrey Wescott</a></author>
/// <author>James House</author>
/// <author>Marko Lahma (.NET)</author>
public class JobStoreTX : JobStoreSupport
{
    /// <summary>
    /// Called by the QuartzScheduler before the <see cref="IJobStore"/> is
    /// used, in order to give it a chance to Initialize.
    /// </summary>
    public override async ValueTask Initialize(
        ITypeLoadHelper loadHelper,
        ISchedulerSignaler signaler,
        CancellationToken cancellationToken = default)
    {
        await base.Initialize(loadHelper, signaler, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("JobStoreTX initialized.");
    }

    /// <summary>
    /// For <see cref="JobStoreTX" />, the non-managed TX connection is just
    /// the normal connection because it is not CMT.
    /// </summary>
    /// <seealso cref="JobStoreSupport.GetConnection(bool)" />
    protected override ValueTask<ConnectionAndTransactionHolder> GetNonManagedTXConnection(bool doTransaction = true)
    {
        return GetConnection(doTransaction);
    }
}