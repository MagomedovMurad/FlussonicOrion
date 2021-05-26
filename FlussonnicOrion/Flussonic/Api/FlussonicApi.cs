using FlussonnicOrion.Models;
using FlussonnicOrion.Utils;
using System.Collections.Generic;
using System.Net;

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
            //curl localhost/vsaas/api/v2/events?type=activity -H 'x-vsaas-api-key: dfb21d1f-3e00-44a2-a706-36d99f9e9d73'
        }

        public void ExecuteRequest(string url)
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("x-vsaas-api-key", "7610d38b-2af8-43a2-aed5-9b194bdbbb94");
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Headers = headers;
            request.Method = "GET";
            WebResponse response = request.GetResponse();
            var json = response.ReadDataAsString();
            response.Close();

            // return JsonConvert.DeserializeObject<PledgeResponse>(json);
        }
    }
}
