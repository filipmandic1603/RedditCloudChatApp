using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace HealthMonitoringService
{
    public class HealthRedditServer
    {
        private ServiceHost serviceHost;
        public HealthRedditServer()
        {
            Start();
        }
        public void Start()
        {
            serviceHost = new ServiceHost(typeof(HealthMonitoring));
            NetTcpBinding binding = new NetTcpBinding();
            serviceHost.AddServiceEndpoint(typeof(IHealthMonitoring), binding, new
            Uri("net.tcp://localhost:8080/HealthMonitoring"));
            serviceHost.Open();
            Console.WriteLine("Reddit Server ready and waiting for requests.");
        }
    }
}
