using HealthMonitoringService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HealthStatusService.Models
{
    public class HealthStatusEntity
    {
        public double UptimePercentage24h { get; set; }
        public double UptimePercentage1h { get; set; }
        public List<HealthCheckEntity> HealthChecks { get; set; }

       
    }
}