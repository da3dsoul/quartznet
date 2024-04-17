#region License

/*
 * Copyright 2009- Marko Lahma
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

using System.Data.Common;

using Microsoft.Extensions.Logging;

using Quartz.Logging;
using Quartz.Spi;

namespace Quartz.Impl.AdoJobStore;

/// <summary>
/// A SQL Server specific driver delegate.
/// </summary>
/// <author>Marko Lahma</author>
public class SqlServerDelegate : StdAdoDelegate
{
    public override void Initialize(IJobStore jobStore, DelegateInitializationArgs args)
    {
        base.Initialize(jobStore, args);

        ILogger<SqlServerDelegate> logger = LogProvider.CreateLogger<SqlServerDelegate>();
        if (jobStore is JobStoreSupport support && (support.Clustered || support.UseDBLocks))
        {
            const string defaultLockSql = "SELECT * FROM {0}LOCKS WITH (UPDLOCK,ROWLOCK) WHERE " + ColumnSchedulerName + " = @schedulerName AND LOCK_NAME = @lockName";
            logger.LogInformation("Detected usage of SqlServerDelegate - defaulting 'selectWithLockSQL' to '{DefaultLockSql}'.", defaultLockSql);
            lockHandler = new StdRowLockSemaphore(args.TablePrefix, args.InstanceName, selectWithLockSQL: null, DbProvider);
        }
        else
        {
            logger.LogInformation("Using thread monitor-based data access locking (synchronization).");
            lockHandler = new SimpleSemaphore();
        }

        logger.LogWarning("Detected usage of SqlServerDelegate and UpdateLockRowSemaphore, removing 'quartz.jobStore.lockHandler.type' would allow more efficient SQL Server specific (UPDLOCK,ROWLOCK) row access");
    }

    /// <summary>
    /// Gets the select next trigger to acquire SQL clause.
    /// SQL Server specific version with TOP functionality
    /// </summary>
    /// <returns></returns>
    protected override string GetSelectNextTriggerToAcquireSql(int maxCount)
    {
        string sqlSelectNextTriggerToAcquire = SqlSelectNextTriggerToAcquire;

        // add limit clause to correct place
        sqlSelectNextTriggerToAcquire = "SELECT TOP " + maxCount + " " + sqlSelectNextTriggerToAcquire.Substring(6);

        return sqlSelectNextTriggerToAcquire;
    }

    protected override string GetSelectNextMisfiredTriggersInStateToAcquireSql(int count)
    {
        if (count != -1)
        {
            var sqlSelectHasMisfiredTriggersInState = SqlSelectHasMisfiredTriggersInState;

            // add limit clause to correct place
            sqlSelectHasMisfiredTriggersInState = "SELECT TOP " + count + " " + sqlSelectHasMisfiredTriggersInState.Substring(6);

            return sqlSelectHasMisfiredTriggersInState;
        }
        return base.GetSelectNextMisfiredTriggersInStateToAcquireSql(count);
    }

    public override void AddCommandParameter(
        DbCommand cmd,
        string paramName,
        object? paramValue,
        Enum? dataType = null,
        int? size = null)
    {
        // deeded for SQL Server CE
        if (paramValue is bool && dataType == null)
        {
            paramValue = (bool) paramValue ? 1 : 0;
        }

        // varbinary support
        if (size == null && dataType != null && dataType.Equals(DbProvider.Metadata.DbBinaryType))
        {
            size = -1;
        }

        // avoid size inferred from value that cause multiple query plans
        if (size == null && paramValue is string)
        {
            size = 4000;
        }

        base.AddCommandParameter(cmd, paramName, paramValue, dataType, size);
    }
}