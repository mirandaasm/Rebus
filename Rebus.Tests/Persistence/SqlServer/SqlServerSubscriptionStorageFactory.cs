﻿using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(Categories.SqlServer)]
    public class BasicSubscriptionOperations : BasicSubscriptionOperations<SqlServerSubscriptionStorageFactory>
    {
    }

    public class SqlServerSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        const string TableName = "RebusSubscriptions";
        
        public ISubscriptionStorage Create()
        {
            var storage = new SqlServerSubscriptionStorage(new DbConnectionProvider(SqlTestHelper.ConnectionString), TableName, true);

            storage.EnsureTableIsCreated();
            
            return storage;
        }

        public void Cleanup()
        {
            SqlTestHelper.DropTable(TableName);
        }
    }
}