using FlussonicOrion.OrionPro.Enums;
using Orion;
using System.Collections.Generic;
using System.Linq;

namespace FlussonicOrion.OrionPro.DataSources
{
    public class OrionClientDataSource : IOrionDataSource
    {
        private IOrionClient _orionClient;
        public OrionClientDataSource(IOrionClient orionClient)
        {
            _orionClient = orionClient;
        }

        public string[] GetPersonPassList(int personId)
        {
            var personData = new TPersonData();
            personData.Id = personId;
            return _orionClient.GetPersonPassList(personData).Result;
        }

        public TAccessLevel GetAccessLevel(int id)
        {
            return _orionClient.GetAccessLevelById(id).Result;
        }

        public TCompany GetCompany(int id)
        {
            return _orionClient.GetCompany(id).Result;
        }

        public TKeyData GetKeysByCode(string code)
        {
            return _orionClient.GetKeyData(code, (int)CodeType.CarNumber).Result;
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
