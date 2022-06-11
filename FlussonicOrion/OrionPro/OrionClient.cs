using FlussonicOrion.OrionPro.Enums;
using FlussonicOrion.OrionPro.Models;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonicOrion.OrionPro
{
    public class OrionClient : IOrionClient
    {
        #region Fields
        private readonly ILogger<IOrionClient> _logger;
        private OrionProClient _client;
        private EndpointAddress _remoteAddress;
        private OrionSettings _settings;
        private Timer _timer;
        private string _token;

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

            InitializeClient();
            await InitializeToken(_settings.TokenLifetime);

            _logger.LogInformation($"OrionClient инициализирован. Token: {_token}");
        }
        private void InitializeClient()
        {
            var binding = CreateBinding();

            _client = new OrionProClient(binding, _remoteAddress);
            if (_settings.ModuleUserName != null && _settings.ModulePassword != null)
            {
                _client.ClientCredentials.UserName.UserName = _settings.ModuleUserName;
                _client.ClientCredentials.UserName.Password = _settings.ModulePassword;
            }
        }
        private async Task InitializeToken(int tokenLifetime)
        {
            StopTokenExpirationExtending();
            var hash = GetMd5Hash(_settings.EmployeePassword);
            _token = await Execute<GetLoginTokenResponse, string>((GetLoginTokenDel)_client.GetLoginTokenAsync, false, _settings.EmployeeUserName, hash);
            StartTokenExpirationExtending(tokenLifetime);
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
            _timer = new Timer();
            _timer.Elapsed += Timer_Elapsed;
            _timer.Interval = (tokenLifetime - 1) * 1000;
            _timer.Start();
        }
        private void StopTokenExpirationExtending()
        {
            if (_timer == null)
                return;

            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            await ExtendTokenExpiration();
            _timer.Enabled = true;
        }
        private async Task ExtendTokenExpiration()
        {
            await Execute<ExtendTokenExpirationResponse, string>((ExtendTokenExpirationDel)_client.ExtendTokenExpirationAsync, false);
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
            return await Execute<GetVisitsResponse, TVisitData[]>((GetVisitsDel)_client.GetVisitsAsync, false);
        }
        public async Task<TPersonData> GetPersonById(int id)
        {
            return await Execute<GetPersonByIdResponse, TPersonData>((GetPersonByIdDel)_client.GetPersonByIdAsync, false, id, true);
        }
        public async Task<string[]> GetPersonPassList(TPersonData personData)
        {
            return await Execute<GetPersonPassListResponse, string[]>((GetPersonPassListDel)_client.GetPersonPassListAsync, false, personData);
        }
        public async Task<TPersonData[]> GetPersons(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors)
        {
            return await Execute<GetPersonsResponse, TPersonData[]>((GetPersonsDel)_client.GetPersonsAsync, false, withoutPhoto, offset, count, filter, isEmployees, isVisitors);
        }
        public async Task<int> GetPersonsCount(string[] filter, bool isEmployees, bool isVisitors)
        {
            return await Execute<GetPersonsCountResponse, int>((GetPersonsCountDel)_client.GetPersonsCountAsync, false, filter, isEmployees, isVisitors);
        }
        public async Task<TTimeWindow[]> GetTimeWindows()
        {
            return await Execute<GetTimeWindowsResponse, TTimeWindow[]>((GetTimeWindowsDel)_client.GetTimeWindowsAsync, false);
        }
        public async Task<TTimeWindow> GetTimeWindowById(int id)
        {
            return await Execute<GetTimeWindowByIdResponse, TTimeWindow>((GetTimeWindowByIdDel)_client.GetTimeWindowByIdAsync, false, id);
        }
        public async Task<TKeyData> GetKeyData(string code, int codeType)
        {
            return await Execute<GetKeyDataResponse, TKeyData>((GetKeyDataDel)_client.GetKeyDataAsync, false, code, codeType);
        }
        public async Task<TKeyData[]> GetKeys(int codeType, int personId, int offset, int count)
        {
            return await Execute<GetKeysResponse, TKeyData[]>((GetKeysDel)_client.GetKeysAsync, false, codeType, personId, offset, count);
        }
        public async Task<int> GetKeysCount(int codeType, int personId)
        {
            return await Execute<GetKeysCountResponse, int>((GetKeysCountDel)_client.GetKeysCountAsync, false, codeType, personId);
        }
        public async Task<TAccessLevel> GetAccessLevelById(int id)
        {
            return await Execute<GetAccessLevelByIdResponse, TAccessLevel>((GetAccessLevelByIdDel)_client.GetAccessLevelByIdAsync, false, id);
        }
        public async Task<int> GetAccessLevelsCount()
        {
            return await Execute<GetAccessLevelsCountResponse, int>((GetAccessLevelsCountDel)_client.GetAccessLevelsCountAsync, false);
        }
        public async Task<TAccessLevel[]> GetAccessLevels(int offset, int count)
        {
            return await Execute<GetAccessLevelsResponse, TAccessLevel[]>((GetAccessLevelsDel)_client.GetAccessLevelsAsync, false, offset, count);
        }

        public async Task<TEntryPoint[]> GetEntryPoints(int offset, int count)
        {
            return await Execute<GetEntryPointsResponse, TEntryPoint[]>((GetEntryPointsDel)_client.GetEntryPointsAsync, false, offset, count);
        }
        public async Task<TAccessZone[]> GetAccessZones()
        {
            return await Execute<GetAccessZonesResponse, TAccessZone[]>((GetAccessZonesDel)_client.GetAccessZonesAsync, false);
        }
        public async Task<TEvent[]> GetEvents(DateTime beginTime, DateTime endTime, int[] eventTypeIds, int offset, int count, TPersonData[] persons, int[] entryPointIds, TSection[] sections, TSectionsGroup[] sectionGroups)
        {
            var eventTypes = eventTypeIds.Select(x => new TEventType { Id = x }).ToArray();
            var entryPoints = entryPointIds.Select(x => new TEntryPoint { Id = x }).ToArray();
            return await Execute<GetEventsResponse, TEvent[]>((GetEventsDel)_client.GetEventsAsync, false, beginTime, endTime, eventTypes, offset, count, persons, entryPoints, sections, sectionGroups);
        }

        #endregion

        #region Commands
        public async Task AddExternalEvent(int id, int itemId, ItemType itemType, int eventTypeId, int keyId, int personId, string text)
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

            await Execute<AddExternalEventResponse, TExternalEvent>((AddExternalEventDel)_client.AddExternalEventAsync, false, externalEvent);
        }
        public async Task ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId)
        {
            var accesspoint = new TItem()
            {
                ItemId = accesspointId,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Timestamp = DateTime.Now
            };
            
            await Execute<ControlItemsResponse, TItem[]>((ControlItemsDel)_client.ControlItemsAsync, false, new[] { accesspoint }, (int)commandId, (int)action, personId);
        }
        #endregion

        #region Utils

        private async Task<T> Execute<Y,T>(Delegate @delegate, bool isRepeat, params object[] args)
        { 
            try
            {
                Y result = await (@delegate.DynamicInvoke(args.Append(_token).ToArray()) as Task<Y>);
                var parsedResult = ParseResult<T>(result);

                if (!parsedResult.IsSuccess && parsedResult.ServiceError != null)
                {
                    if (parsedResult.ServiceError.InnerExceptionMessage == "Token not found" && !isRepeat)
                    {
                        await InitializeToken(_settings.TokenLifetime);
                        return await Execute<Y,T>(@delegate, true, args);
                    }

                    var message = $"Ошибка сервера (код {parsedResult.ServiceError.ErrorCode}): {parsedResult.ServiceError.Description}";
                    var innerException = new Exception(parsedResult.ServiceError.InnerExceptionMessage);
                    throw new InvalidOperationException(message, innerException);
                }

                return parsedResult.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при выполнении метода {@delegate.Method.Name}:");
                return default;
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
            if (_timer != null)
            {
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            }
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