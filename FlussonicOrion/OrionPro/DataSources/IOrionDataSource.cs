using Orion;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro.DataSources
{
    public interface IOrionDataSource
    {
        void Initialize();
        void Dispose();

        Task<TVisitData> GetActualVisitByRegNumber(string regNumber);
        Task<TKeyData> GetKeyByCode(string code);
        Task<TPersonData> GetPersonById(int id);
        Task<TPersonData> GetPersonByTabNum(string tabNum);
        Task<TAccessLevel> GetAccessLevel(int id);
        Task<TTimeWindow> GetTimeWindow(int id);
        Task<TKeyData> GetKeyByPersonId(int personId);
    }
}
