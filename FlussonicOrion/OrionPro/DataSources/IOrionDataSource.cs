using Orion;

namespace FlussonicOrion.OrionPro.DataSources
{
    public interface IOrionDataSource
    {
        void Initialize();
        void Dispose();

        TVisitData GetActualVisitByRegNumber(string regNumber);
        TKeyData GetKeyByCode(string code);
        TPersonData GetPerson(int id);
        TAccessLevel GetAccessLevel(int id);
        TTimeWindow GetTimeWindow(int id);
        TKeyData GetKeyByPersonId(int personId);
    }
}
