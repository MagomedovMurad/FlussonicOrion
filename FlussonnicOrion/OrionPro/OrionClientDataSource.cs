using Orion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.OrionPro
{
    public class OrionClientDataSource : IOrionCache
    {
        private IOrionClient _orionClient;
        public OrionClientDataSource(IOrionClient orionClient)
        {
            _orionClient = orionClient;
        }


        public async Task<TAccessLevel> GetAccessLevel(int id)
        {
            return await _orionClient.GetAccessLevelById(id);
        }

        public async Task<TCompany> GetCompany(int id)
        {
            return await _orionClient.GetCompany(id);
        }

        public async Task<IEnumerable<TKeyData>> GetKeysByRegNumber(string regNumber)
        {
            var key = await _orionClient.GetKeyData(regNumber, 5);
            return new[] { key };
        }

        public async Task<TPersonData> GetPerson(int id)
        {
            return await _orionClient.GetPersons().Result.First();
        }

        public TTimeWindow GetTimeWindow(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TVisitData> GetVisitsByRegNumber(string regNumber)
        {
            throw new NotImplementedException();
        }

        public void Initialize(int employeeInterval, int visitorsInterval)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
