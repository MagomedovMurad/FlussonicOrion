using FlussonicOrion.OrionPro.Enums;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro.DataSources
{
    public class OrionClientDataSource : IOrionDataSource
    {
        #region Fields
        private readonly IOrionClient _orionClient;
        private readonly ILogger<IOrionDataSource> _logger;
        #endregion

        #region Ctor
        public OrionClientDataSource(IOrionClient orionClient, ILogger<IOrionDataSource> logger)
        {
            _orionClient = orionClient;
            _logger = logger;
        }
        #endregion

        #region Initialize/Dispose
        public void Initialize()
        {

        }
        public void Dispose()
        {

        }
        #endregion

        #region IOrionDataSource
        public TKeyData GetKeyByPersonId(int personId)
        {
            var personData = new TPersonData();
            personData.Id = personId;
            personData.Photo = new byte[0];
            var passList = _orionClient.GetPersonPassList(personData).Result;
            var tasks = passList.Select(x => _orionClient.GetKeyData(x, 0));

            var keys = Task.WhenAll(tasks).Result;

            var key = keys.FirstOrDefault(x => x.Comment.Contains("flussonic", StringComparison.InvariantCultureIgnoreCase));
            if(key is null)
                key = keys.FirstOrDefault();

            return key;
        }
        public TAccessLevel GetAccessLevel(int id)
        {
            return _orionClient.GetAccessLevelById(id).Result;
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
        #endregion
    }
}
