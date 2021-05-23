using FlussonnicOrion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Api
{
    public class FlussonicApi
    {
        public FlussonicApi()
        { 
            
        }

        public IEnumerable<FlussonicEvent> GetEvents()
        {
            return new List<FlussonicEvent>();
            //localhost/vsaas/api/v2/events?type=activity
        }

        private void ExecuteRequest(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
        }
    }
}
