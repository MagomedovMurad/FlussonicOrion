using FlussonicOrion.OrionPro.Enums;
using FlussonicOrion.OrionPro.Models;
using Orion;
using System;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro
{
    public interface IOrionClient
    {
        #region Initialize/Dispose
        Task Initialize(OrionSettings settings);
        void Dispose();
        #endregion

        #region Queries
        Task<TVisitData[]> GetVisits();
        Task<int> GetPersonsCount(string[] filter, bool isEmployees, bool isVisitors);
        Task<TPersonData[]> GetPersons(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors);
        Task<string[]> GetPersonPassList(TPersonData personData);
        Task<TPersonData> GetPersonById(int id);
        Task<TPersonData> GetPersonByTabNum(string tabNum);
        Task<TTimeWindow[]> GetTimeWindows();
        Task<TTimeWindow> GetTimeWindowById(int id);
        Task<TKeyData> GetKeyData(string code, int codeType);
        Task<TKeyData[]> GetKeys(int codeType, int personId, int offset, int count);
        Task<int> GetKeysCount(int codeType, int personId);
        Task<TAccessLevel> GetAccessLevelById(int id);
        Task<int> GetAccessLevelsCount();
        Task<TAccessLevel[]> GetAccessLevels(int offset, int count);
        Task<TEntryPoint[]> GetEntryPoints(int offset, int count);
        Task<TAccessZone[]> GetAccessZones();
        Task<TEvent[]> GetEvents(DateTime beginTime, DateTime endTime, int[] eventTypes, int offset, int count, TPersonData[] persons, int[] entryPoints, TSection[] sections, TSectionsGroup[] sectionGroups);
        #endregion

        #region Commands
        Task<TExternalEvent> AddExternalEvent(int id, int itemId, ItemType itemType, int eventTypeId, int keyId, int personId, string text);
        Task<TItem[]> ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId);
        #endregion
    }
}
