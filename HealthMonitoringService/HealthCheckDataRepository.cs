using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMonitoringService
{

    public class HealthCheckDataRepository
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;

        public HealthCheckDataRepository()
        {
            _storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
            _table = tableClient.GetTableReference("HealthCheck");
            _table.CreateIfNotExists();
        }

        public void LogHealtCheck(HealthCheckEntity health)
        {
            TableOperation insertOperation = TableOperation.Insert(health);
            _table.Execute(insertOperation);
        }
        public IQueryable<HealthCheckEntity> RetrieveAllLogsData()
        {
            var results = from g in _table.CreateQuery<HealthCheckEntity>()
                          where g.PartitionKey == "AvailabilityServices"
                          select g;
            return results;
        }
        public void DeleteAllLogsData(string partitionKey)
        {
            partitionKey = "AvailabilityServices";
            var results = from g in _table.CreateQuery<HealthCheckEntity>()
                          where g.PartitionKey == partitionKey
                          select g;

            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach (var entity in results)
            {
                batchOperation.Delete(entity);

                // Azure Table Storage batch operations can only contain up to 100 operations.
                // So, if we reach 100 operations, we need to execute the batch and start a new one.
                if (batchOperation.Count == 100)
                {
                    _table.ExecuteBatch(batchOperation);
                    batchOperation = new TableBatchOperation();
                }
            }

            // Execute the last batch if there are any remaining operations
            if (batchOperation.Count > 0)
            {
                _table.ExecuteBatch(batchOperation);
            }
        }

    }
}
