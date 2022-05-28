using Orion;
using System.Collections.Generic;
using System.Linq;

namespace FlussonicOrion.OrionPro
{
    public class OrionClientDataSource : IOrionDataSource
    {
        private IOrionClient _orionClient;
        public OrionClientDataSource(IOrionClient orionClient)
        {
            _orionClient = orionClient;
        }


        public TAccessLevel GetAccessLevel(int id)
        {
            return _orionClient.GetAccessLevelById(id).Result;
        }

        public TCompany GetCompany(int id)
        {
            return _orionClient.GetCompany(id).Result;
        }

        public IEnumerable<TKeyData> GetKeysByRegNumber(string regNumber)
        {
            var key = _orionClient.GetKeyData(regNumber, 5).Result;
            if (key != null)
                return new[] { key };
            else
                return new List<TKeyData>();
        }

        public TPersonData GetPerson(int id)
        {
            return _orionClient.GetPersonById(id).Result;
        }

        public TTimeWindow GetTimeWindow(int id)
        {
            return _orionClient.GetTimeWindowById(id).Result;
        }

        public IEnumerable<TVisitData> GetVisitsByRegNumber(string regNumber)
        {
            var visits = _orionClient.GetVisits().Result;
            return visits.Where(x => x.CarNumber == regNumber).ToArray();
        }

        public void Initialize(int employeeInterval, int visitorsInterval)
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}
