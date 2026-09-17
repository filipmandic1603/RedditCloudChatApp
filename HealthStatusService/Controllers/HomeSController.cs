using HealthMonitoringService;
using HealthStatusService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HealthStatusService.Controllers
{
    public class HomeSController : Controller
    {
        HealthCheckDataRepository healthCheckDataRepository = new HealthCheckDataRepository();
        public ActionResult Index()
        {
            DateTime last24Hours = DateTime.Now.AddHours(-24);
            DateTime lastHour = DateTime.Now.AddHours(-3);

          
            List<HealthCheckEntity> last24HourhealthChecks = GetHealthChecks(last24Hours);
            List<HealthCheckEntity> lastHourhealthChecks = GetHealthChecks(lastHour);

            
            var uptimePercentage24h = CalculateUptimePercentage(last24HourhealthChecks);
            var uptimePercentage1h = CalculateUptimePercentage(lastHourhealthChecks);

            HealthStatusEntity model = new HealthStatusEntity()
            {
                UptimePercentage24h = uptimePercentage24h,
                UptimePercentage1h = uptimePercentage1h,
                HealthChecks = last24HourhealthChecks
            };

            return View(model);
        }
        public List<HealthCheckEntity> GetHealthChecks(DateTime from)
        {
           
            List<HealthCheckEntity> healthCheckEntities = healthCheckDataRepository.RetrieveAllLogsData().ToList();
            List<HealthCheckEntity> result = healthCheckEntities.Where(x => x.DateTime >= from).ToList();
            
            return result;
        }
        private double CalculateUptimePercentage(List<HealthCheckEntity> healthChecks)
        {
            int totalChecks = healthChecks.Count();
            int successfulChecks = 0;
            foreach (var item in healthChecks)
            {
                string[] parts = item.Status.Split('_');
                if (parts[1].Equals("OK"))
                {
                    successfulChecks++;
                }
            }
            if (totalChecks == 0) return 0;
            return (double)successfulChecks / totalChecks * 100;
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}