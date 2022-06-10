using Orion;
using System.Collections.Generic;

namespace FlussonicOrion.OrionPro.DataSources
{
    public interface IOrionDataSource
    {
        void Initialize(int employeeInterval, int visitorsInterval);
        void Dispose();

        IEnumerable<TVisitData> GetVisitsByRegNumber(string regNumber);
        TKeyData GetKeysByCode(string code);
        TPersonData GetPerson(int id);
        TAccessLevel GetAccessLevel(int id);
        TTimeWindow GetTimeWindow(int id);
        TCompany GetCompany(int id);
        string[] GetPersonPassList(int personId);
    }
}
