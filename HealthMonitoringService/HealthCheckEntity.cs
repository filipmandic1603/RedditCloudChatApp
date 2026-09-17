using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMonitoringService
{
    public class HealthCheckEntity: TableEntity
    {
        public string Status { get; set; }

        public DateTime DateTime { get; set; }
        public HealthCheckEntity(DateTime timestamp)
        {
           PartitionKey = "AvailabilityServices";
           RowKey = timestamp.ToString("yyyy-MM-dd-HH:mm:ss");
            
        }

        public HealthCheckEntity() { }
    }
}
