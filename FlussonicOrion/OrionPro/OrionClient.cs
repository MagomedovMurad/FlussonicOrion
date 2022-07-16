using FlussonicOrion.OrionPro.Enums;
using FlussonicOrion.OrionPro.Models;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro
{
    public class OrionClient : IOrionClient
    {
        #region Fields
        private readonly ILogger<IOrionClient> _logger;
        private OrionProClient _client;
        private EndpointAddress _remoteAddress;
        private OrionSettings _settings;
        private string _token;
        private IDisposable _subscription;

        #endregion

        public OrionClient(ILogger<IOrionClient> logger)
        {
            _logger = logger;
        }

        #region Initialize

        public async Task Initialize(OrionSettings settings)
        {
            _settings = settings;
            _remoteAddress = new EndpointAddress($"http://{settings.IPAddress}:{settings.Port}/soap/IOrionPro");
            _client = CreateClient(settings.ModuleUserName, settings.ModulePassword);
            _token = await CreateToken();
            StartTokenExpirationExtending(settings.TokenLifetime);
            _logger.LogInformation($"OrionClient инициализирован. Token: {_token}");
        }

        private OrionProClient CreateClient(string moduleUserName, string modulePassword)
        {
            var binding = CreateBinding();

            var client = new OrionProClient(binding, _remoteAddress);
            if (moduleUserName != null && modulePassword != null)
            {
                client.ClientCredentials.UserName.UserName = moduleUserName;
                client.ClientCredentials.UserName.Password = modulePassword;
            }
            return client;
        }
        private async Task<string> CreateToken()
        {
            var hash = GetMd5Hash(_settings.EmployeePassword);
            return await Execute<GetLoginTokenResponse, string>((GetLoginTokenDel)_client.GetLoginTokenAsync, 0, _settings.EmployeeUserName, hash);
        }
        private BasicHttpBinding CreateBinding()
        {
            var binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.MaxBufferSize = int.MaxValue;
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            return binding;
        }
        private void StartTokenExpirationExtending(int tokenLifetime)
        {
            _subscription = Observable.Interval(TimeSpan.FromSeconds(tokenLifetime - 1))
                                      .Subscribe(async x => await ExtendTokenExpiration());
        }
        private async Task ExtendTokenExpiration()
        {
            await Execute<ExtendTokenExpirationResponse, string>((ExtendTokenExpirationDel)_client.ExtendTokenExpirationAsync, 0);
        }
        private string GetMd5Hash(string data)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    var sourceBytes = Encoding.UTF8.GetBytes(data);
                    var hashBytes = md5.ComputeHash(sourceBytes);
                    var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
                    return hash;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при расчете хэша: {ex}");
                return null;
            }
        }

        #endregion

        #region Queries
        #region Delegates
        private delegate Task<GetLoginTokenResponse> GetLoginTokenDel(string Login, string Md5Passw, string token);
        private delegate Task<ExtendTokenExpirationResponse> ExtendTokenExpirationDel(string token);
        private delegate Task<GetVisitsResponse> GetVisitsDel(string token);
        private delegate Task<GetPersonsResponse> GetPersonsDel(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors, string token);
        private delegate Task<GetPersonPassListResponse> GetPersonPassListDel(TPersonData personData, string token);
        private delegate Task<GetPersonByIdResponse> GetPersonByIdDel(int id, bool withoutPhoto, string token);
        private delegate Task<GetPersonByTabNumberResponse> GetPersonByTabNumberDel(string tabNumber, bool withoutPhoto, string token);
        private delegate Task<GetPersonsCountResponse> GetPersonsCountDel(string[] filter, bool isEmployees, bool isVisitors, string token);
        private delegate Task<GetTimeWindowsResponse> GetTimeWindowsDel(string token);
        private delegate Task<GetTimeWindowByIdResponse> GetTimeWindowByIdDel(int id, string token);
        private delegate Task<GetKeyDataResponse> GetKeyDataDel(string code, int codeType, string token);
        private delegate Task<GetKeysResponse> GetKeysDel(int codeType, int personId, int offset, int count, string token);
        private delegate Task<GetKeysCountResponse> GetKeysCountDel(int codeType, int personId, string token);
        private delegate Task<GetAccessLevelByIdResponse> GetAccessLevelByIdDel(int id, string token);
        private delegate Task<GetAccessLevelsCountResponse> GetAccessLevelsCountDel(string token);
        private delegate Task<GetAccessLevelsResponse> GetAccessLevelsDel(int offset, int count, string token);
        private delegate Task<AddExternalEventResponse> AddExternalEventDel(TExternalEvent externalEvent, string token);
        private delegate Task<ControlItemsResponse> ControlItemsDel(TItem[] items, int command, int action, int personId, string token);
        private delegate Task<GetEntryPointsResponse> GetEntryPointsDel(int offset, int count, string token);
        private delegate Task<GetAccessZonesResponse> GetAccessZonesDel(string token);
        private delegate Task<GetEventsResponse> GetEventsDel(DateTime beginTime, DateTime endTime, TEventType[] eventTypes, int offset, int count, TPersonData[] persons, TEntryPoint[] entryPoints, TSection[] sections, TSectionsGroup[] sectionGroups, string token);
        #endregion
        public async Task<TVisitData[]> GetVisits()
        {
            return await Execute<GetVisitsResponse, TVisitData[]>((GetVisitsDel)_client.GetVisitsAsync, 0);
        }
        public async Task<TPersonData> GetPersonById(int id)
        {
            return await Execute<GetPersonByIdResponse, TPersonData>((GetPersonByIdDel)_client.GetPersonByIdAsync, 0, id, true);
        }
        public async Task<TPersonData> GetPersonByTabNum(string tabNum)
        {
            return await Execute<GetPersonByTabNumberResponse, TPersonData>((GetPersonByTabNumberDel)_client.GetPersonByTabNumberAsync, 0, tabNum, true);
        }
        public async Task<string[]> GetPersonPassList(TPersonData personData)
        {
            return await Execute<GetPersonPassListResponse, string[]>((GetPersonPassListDel)_client.GetPersonPassListAsync, 0, personData);
        }
        public async Task<TPersonData[]> GetPersons(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors)
        {
            return await Execute<GetPersonsResponse, TPersonData[]>((GetPersonsDel)_client.GetPersonsAsync, 0, withoutPhoto, offset, count, filter, isEmployees, isVisitors);
        }
        public async Task<int> GetPersonsCount(string[] filter, bool isEmployees, bool isVisitors)
        {
            return await Execute<GetPersonsCountResponse, int>((GetPersonsCountDel)_client.GetPersonsCountAsync, 0, filter, isEmployees, isVisitors);
        }
        public async Task<TTimeWindow[]> GetTimeWindows()
        {
            return await Execute<GetTimeWindowsResponse, TTimeWindow[]>((GetTimeWindowsDel)_client.GetTimeWindowsAsync, 0);
        }
        public async Task<TTimeWindow> GetTimeWindowById(int id)
        {
            return await Execute<GetTimeWindowByIdResponse, TTimeWindow>((GetTimeWindowByIdDel)_client.GetTimeWindowByIdAsync, 0, id);
        }
        public async Task<TKeyData> GetKeyData(string code, int codeType)
        {
            return await Execute<GetKeyDataResponse, TKeyData>((GetKeyDataDel)_client.GetKeyDataAsync, 0, code, codeType);
        }
        public async Task<TKeyData[]> GetKeys(int codeType, int personId, int offset, int count)
        {
            return await Execute<GetKeysResponse, TKeyData[]>((GetKeysDel)_client.GetKeysAsync, 0, codeType, personId, offset, count);
        }
        public async Task<int> GetKeysCount(int codeType, int personId)
        {
            return await Execute<GetKeysCountResponse, int>((GetKeysCountDel)_client.GetKeysCountAsync, 0, codeType, personId);
        }
        public async Task<TAccessLevel> GetAccessLevelById(int id)
        {
            return await Execute<GetAccessLevelByIdResponse, TAccessLevel>((GetAccessLevelByIdDel)_client.GetAccessLevelByIdAsync, 0, id);
        }
        public async Task<int> GetAccessLevelsCount()
        {
            return await Execute<GetAccessLevelsCountResponse, int>((GetAccessLevelsCountDel)_client.GetAccessLevelsCountAsync, 0);
        }
        public async Task<TAccessLevel[]> GetAccessLevels(int offset, int count)
        {
            return await Execute<GetAccessLevelsResponse, TAccessLevel[]>((GetAccessLevelsDel)_client.GetAccessLevelsAsync, 0, offset, count);
        }

        public async Task<TEntryPoint[]> GetEntryPoints(int offset, int count)
        {
            return await Execute<GetEntryPointsResponse, TEntryPoint[]>((GetEntryPointsDel)_client.GetEntryPointsAsync, 0, offset, count);
        }
        public async Task<TAccessZone[]> GetAccessZones()
        {
            return await Execute<GetAccessZonesResponse, TAccessZone[]>((GetAccessZonesDel)_client.GetAccessZonesAsync, 0);
        }
        public async Task<TEvent[]> GetEvents(DateTime beginTime, DateTime endTime, int[] eventTypeIds, int offset, int count, TPersonData[] persons, int[] entryPointIds, TSection[] sections, TSectionsGroup[] sectionGroups)
        {
            var eventTypes = eventTypeIds.Select(x => new TEventType { Id = x }).ToArray();
            var entryPoints = entryPointIds.Select(x => new TEntryPoint { Id = x }).ToArray();
            return await Execute<GetEventsResponse, TEvent[]>((GetEventsDel)_client.GetEventsAsync, 0, beginTime, endTime, eventTypes, offset, count, persons, entryPoints, sections, sectionGroups);
        }

        #endregion

        #region Commands
        public async Task<TExternalEvent> AddExternalEvent(int id, int itemId, ItemType itemType, int eventTypeId, int keyId, int personId, string text)
        {
            var externalEvent = new TExternalEvent()
            {
                Id = id,
                ItemId = itemId,
                ItemType = itemType.ToString(),
                Event = eventTypeId,
                KeyId = keyId,
                PersonId = personId,
                TimeStamp = DateTime.Now,
                Text = text
            };

            return await Execute<AddExternalEventResponse, TExternalEvent>((AddExternalEventDel)_client.AddExternalEventAsync, 0, externalEvent);
        }
        public async Task<TItem[]> ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId)
        {
            var accesspoint = new TItem()
            {
                ItemId = accesspointId,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Timestamp = DateTime.Now
            };
            
            return await Execute<ControlItemsResponse, TItem[]>((ControlItemsDel)_client.ControlItemsAsync, 0, new[] { accesspoint }, (int)commandId, (int)action, personId);
        }
        #endregion

        #region Utils

        private async Task<T> Execute<Y,T>(Delegate @delegate, int attempt, params object[] args)
        { 
            try
            {
                Y result = await (@delegate.DynamicInvoke(args.Append(_token).ToArray()) as Task<Y>);
                var parsedResult = ParseResult<T>(result);

                if (!parsedResult.IsSuccess && parsedResult.ServiceError != null)
                {
                    if (parsedResult.ServiceError.InnerExceptionMessage == "Token not found")
                        _token = await CreateToken();

                    var message = $"Ошибка сервера (код {parsedResult.ServiceError.ErrorCode}): {parsedResult.ServiceError.Description}";
                    var innerException = new Exception(parsedResult.ServiceError.InnerExceptionMessage);
                    throw new InvalidOperationException(message, innerException);
                }

                return parsedResult.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при выполнении метода {@delegate.Method.Name}(попытка {attempt}):");
                return attempt >= 2? default : await Execute<Y, T>(@delegate, attempt+1, args);
            }
        }
        private RequestResult<T> ParseResult<T>(object result)
        {
            var @return = result.GetType().GetField("return").GetValue(result);
            var returnProps = @return.GetType().GetProperties();
            T operationResult = (T)returnProps.Single(x => x.Name.Equals("OperationResult")).GetValue(@return);

            bool success = (bool)returnProps.Single(x => x.Name.Equals("Success")).GetValue(@return);
            TServiceError serviceError = (TServiceError)returnProps.Single(x => x.Name.Equals("ServiceError")).GetValue(@return);

            return new RequestResult<T>() { Result = operationResult, IsSuccess = success, ServiceError = serviceError };
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _client?.Close();
        }
        #endregion
    }

    public class RequestResult<T>
    { 
        public T Result { get; set; }
        public bool IsSuccess { get; set; }
        public TServiceError ServiceError { get; set; }
    }
}