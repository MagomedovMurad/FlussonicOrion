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
        public async Task<TKeyData> GetKeyByPersonId(int personId)
        {
            var personData = new TPersonData();
            personData.Id = personId;
            personData.Photo = new byte[0];
            var passList = await _orionClient.GetPersonPassList(personData);
            var tasks = passList?.Select(x => _orionClient.GetKeyData(x, 0));

            TKeyData[] keys = null;
            if (tasks != null)
                keys = Task.WhenAll(tasks).Result;

            var key = keys?.FirstOrDefault(x => x.Comment.Contains("flussonic", StringComparison.InvariantCultureIgnoreCase));
            if(key is null)
                key = keys?.FirstOrDefault();

            return key;
        }
        public async Task<TAccessLevel> GetAccessLevel(int id)
        {
            return await _orionClient.GetAccessLevelById(id);
        }
        public async Task<TKeyData> GetKeyByCode(string code)
        {
            return await _orionClient.GetKeyData(code, (int)CodeType.CarNumber);
        }
        public async Task<TPersonData> GetPersonById(int id)
        {
            return await _orionClient.GetPersonById(id);
        }
        public async Task <TPersonData> GetPersonByTabNum(string tabNum)
        {
            return await _orionClient.GetPersonByTabNum(tabNum);
        }
        public async Task<TTimeWindow> GetTimeWindow(int id)
        {
            return await _orionClient.GetTimeWindowById(id);
        }
        public async Task<TVisitData> GetActualVisitByRegNumber(string regNumber)
        {
            var visits = await _orionClient.GetVisits();
            if (visits == null)
                return null;
            return visits.Where(x => x.CarNumber.Equals(regNumber))
                         .FirstOrDefault(x => DateTime.Now >= x.VisitDate &&
                                              DateTime.Now <= x.VisitEndDateTime);
        }
        #endregion
    }
}
