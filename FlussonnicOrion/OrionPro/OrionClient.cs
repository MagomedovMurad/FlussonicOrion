using FlussonnicOrion.OrionPro.Enums;
using FlussonnicOrion.OrionPro.Models;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonnicOrion.OrionPro
{
    public interface IOrionClient
    {
        Task Initialize(OrionSettings settings);
        void Dispose();

        #region Queries
        Task<TVisitData[]> GetVisits();
        Task<int> GetPersonsCount();
        Task<TPersonData[]> GetPersons(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors);
        Task<TTimeWindow[]> GetTimeWindows();
        Task<TKeyData> GetKeyData(string code, int codeType);
        Task<TKeyData[]> GetKeys(int offset, int count);
        Task<int> GetKeysCount();
        Task<TAccessLevel> GetAccessLevelById(int id);
        Task<int> GetAccessLevelsCount();
        Task<TAccessLevel[]> GetAccessLevels(int offset, int count);
        Task<TCompany[]> GetCompanies(bool isEmployees, bool isVisitors);
        Task<TCompany> GetCompany(int id);
        #endregion

        #region Commands
        Task AddExternalEvent(int id, int itemId, ItemType itemType, int eventTypeId, int keyId, int personId, string text);
        Task ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId);
        #endregion
    }

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
            _timer.Interval = tokenLifetime * 1000;
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
            _token = await Execute<ExtendTokenExpirationResponse, string>((ExtendTokenExpirationDel)_client.ExtendTokenExpirationAsync, false, _token);
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
        #region Delegetes
        private delegate Task<GetLoginTokenResponse> GetLoginTokenDel(string Login, string Md5Passw);
        private delegate Task<ExtendTokenExpirationResponse> ExtendTokenExpirationDel(string token);
        private delegate Task<GetVisitsResponse> GetVisitsDel(string token);
        private delegate Task<GetPersonsResponse> GetPersonsDel(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors, string token);
        private delegate Task<GetPersonsCountResponse> GetPersonsCountDel(string token);
        private delegate Task<GetTimeWindowsResponse> GetTimeWindowsDel(string token);
        private delegate Task<GetKeyDataResponse> GetKeyDataDel(string code, int codeType, string token);
        private delegate Task<GetKeysResponse> GetKeysDel(int offset, int count, string token);
        private delegate Task<GetKeysCountResponse> GetKeysCountDel(string token);
        private delegate Task<GetAccessLevelByIdResponse> GetAccessLevelByIdDel(int id, string token);
        private delegate Task<GetAccessLevelsCountResponse> GetAccessLevelsCountDel(string token);
        private delegate Task<GetAccessLevelsResponse> GetAccessLevelsDel(int offset, int count, string token);
        private delegate Task<GetCompaniesResponse> GetCompaniesDel(bool isEmployees, bool isVisitors, string token);
        private delegate Task<AddExternalEventResponse> AddExternalEventDel(TExternalEvent externalEvent, string token);
        private delegate Task<ControlItemsResponse> ControlItemsDel(string token, TItem[] item, int command, int action, int personId);
        private delegate Task<GetCompanyByIdResponse> GetCompanyByIdDel(int id, string token);
        #endregion
        public async Task<TVisitData[]> GetVisits()
        {
            return await Execute<GetVisitsResponse, TVisitData[]>((GetVisitsDel)_client.GetVisitsAsync, false, _token);
        }
        public async Task<TPersonData[]> GetPersons(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors)
        {
            return await Execute<GetPersonsResponse, TPersonData[]>((GetPersonsDel)_client.GetPersonsAsync, false, withoutPhoto, offset, count, filter, isEmployees, isVisitors, _token);
        }
        public async Task<int> GetPersonsCount()
        {
            return await Execute<GetPersonsCountResponse, int>((GetPersonsCountDel)_client.GetPersonsCountAsync, false, _token);
        }
        public async Task<TTimeWindow[]> GetTimeWindows()
        {
            return await Execute<GetTimeWindowsResponse, TTimeWindow[]>((GetTimeWindowsDel)_client.GetTimeWindowsAsync, false, _token);
        }
        public async Task<TKeyData> GetKeyData(string code, int codeType)
        {
            return await Execute<GetKeyDataResponse, TKeyData>((GetKeyDataDel)_client.GetKeyDataAsync, false, code, codeType, _token);
        }
        public async Task<TKeyData[]> GetKeys(int offset, int count)
        {
            return await Execute<GetKeysResponse, TKeyData[]>((GetKeysDel)_client.GetKeysAsync, false, offset, count, _token);
        }
        public async Task<int> GetKeysCount()
        {
            return await Execute<GetKeysCountResponse, int>((GetKeysCountDel)_client.GetKeysCountAsync, false, _token);
        }
        public async Task<TAccessLevel> GetAccessLevelById(int id)
        {
            return await Execute<GetAccessLevelByIdResponse, TAccessLevel>((GetAccessLevelByIdDel)_client.GetAccessLevelByIdAsync, false, id, _token);
        }
        public async Task<int> GetAccessLevelsCount()
        {
            return await Execute<GetAccessLevelsCountResponse, int>((GetAccessLevelsCountDel)_client.GetAccessLevelsCountAsync, false, _token);
        }
        public async Task<TAccessLevel[]> GetAccessLevels(int offset, int count)
        {
            return await Execute<GetAccessLevelsResponse, TAccessLevel[]>((GetAccessLevelsDel)_client.GetAccessLevelsAsync, false, offset, count,_token);
        }
        public async Task<TCompany[]> GetCompanies(bool isEmployees, bool isVisitors)
        {
            return await Execute<GetCompaniesResponse, TCompany[]>((GetCompaniesDel)_client.GetCompaniesAsync, false, isEmployees, isVisitors, _token);
        }
        public async Task<TCompany> GetCompany(int id)
        {
            return await Execute<GetCompanyByIdResponse, TCompany>((GetCompanyByIdDel)_client.GetCompanyByIdAsync, false, id, _token);
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

            await Execute<AddExternalEventResponse, TExternalEvent>((AddExternalEventDel)_client.AddExternalEventAsync, false, externalEvent, _token);
        }
        public async Task ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId)
        {
            var accesspoint = new TItem()
            {
                ItemId = accesspointId,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Timestamp = DateTime.Now
            };
            await Execute<ControlItemsResponse, TItem[]>((ControlItemsDel)_client.ControlItemsAsync, false, _token, new[] { accesspoint }, (int)commandId, (int)action, personId);
        }
        #endregion

        #region Utils

        private RequestResult<T> ParseResult<T>(object result)
        {
            var @return = result.GetType().GetField("return").GetValue(result);
            var returnProps = @return.GetType().GetProperties();
            T operationResult = (T)returnProps.Single(x => x.Name.Equals("OperationResult")).GetValue(@return);

            bool success = (bool)returnProps.Single(x => x.Name.Equals("Success")).GetValue(@return);
            TServiceError serviceError = (TServiceError)returnProps.Single(x => x.Name.Equals("ServiceError")).GetValue(@return);

            return new RequestResult<T>() { Result = operationResult, IsSuccess = success, ServiceError = serviceError };
        }

        private async Task<T> Execute<Y,T>(Delegate @delegate, bool isRepeat, params object[] args)
        { 
            try
            {
                Y result = await (@delegate.DynamicInvoke(args) as Task<Y>);
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