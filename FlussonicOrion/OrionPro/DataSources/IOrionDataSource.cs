using Orion;

namespace FlussonicOrion.OrionPro.DataSources
{
    public interface IOrionDataSource
    {
        void Initialize(int employeeInterval, int visitorsInterval);
        void Dispose();

        TVisitData GetActualVisitByRegNumber(string regNumber);
        TKeyData GetKeyByCode(string code);
        TPersonData GetPerson(int id);
        TAccessLevel GetAccessLevel(int id);
        TTimeWindow GetTimeWindow(int id);
        TCompany GetCompany(int id);
        TKeyData GetKeyByPersonIdAndComment(int personId, string comment);
    }
}
