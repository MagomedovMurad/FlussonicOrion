using Orion;

namespace FlussonicOrion.OrionPro.DataSources
{
    public interface IOrionDataSource
    {
        void Initialize();
        void Dispose();

        TVisitData GetActualVisitByRegNumber(string regNumber);
        TKeyData GetKeyByCode(string code);
        TPersonData GetPersonById(int id);
        TPersonData GetPersonByTabNum(string tabNum);
        TAccessLevel GetAccessLevel(int id);
        TTimeWindow GetTimeWindow(int id);
        TKeyData GetKeyByPersonId(int personId);
    }
}
