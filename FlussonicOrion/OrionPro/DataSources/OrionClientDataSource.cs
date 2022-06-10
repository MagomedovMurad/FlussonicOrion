using FlussonicOrion.OrionPro.Enums;
using Orion;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro.DataSources
{
    public class OrionClientDataSource : IOrionDataSource
    {
        private IOrionClient _orionClient;
        public OrionClientDataSource(IOrionClient orionClient)
        {
            _orionClient = orionClient;
        }

        public TKeyData GetKeyByPersonIdAndComment(int personId, string comment)
        {
            var personData = new TPersonData();
            personData.Id = personId;
            var passList = _orionClient.GetPersonPassList(personData).Result;
            var tasks = passList.Select(x => _orionClient.GetKeyData(x, 0));

            var keys = Task.WhenAll(tasks).Result;
            return keys.FirstOrDefault(x => x.Comment.Contains(comment, StringComparison.InvariantCultureIgnoreCase));
        }

        public TAccessLevel GetAccessLevel(int id)
        {
            return _orionClient.GetAccessLevelById(id).Result;
        }

        public TCompany GetCompany(int id)
        {
            return _orionClient.GetCompany(id).Result;
        }

        public TKeyData GetKeyByCode(string code)
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

        public TVisitData GetActualVisitByRegNumber(string regNumber)
        {
            var visits = _orionClient.GetVisits().Result;
            return visits.Where(x => x.CarNumber.Equals(regNumber))
                         .FirstOrDefault(x => DateTime.Now >= x.VisitDate &&
                                              DateTime.Now <= x.VisitEndDateTime);
        }

        public void Initialize(int employeeInterval, int visitorsInterval)
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}
