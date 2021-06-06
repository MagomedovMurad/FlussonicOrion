using FlussonnicOrion.OrionPro.Enums;
using FlussonnicOrion.OrionPro.Models;
using Orion;
using System;
using System.Linq;
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
        #endregion

        #region Commands
        Task AddExternalEvent(int id, int itemId, ItemType itemType, int eventTypeId, int keyId, int personId, string text);
        Task ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId);
        #endregion
    }

    public class OrionClient: IOrionClient
    {
        #region Fields
        private OrionProClient _client;
        private EndpointAddress _remoteAddress;
        private OrionSettings _settings;
        private Timer _timer;
        private string _token;
        #endregion

        public OrionClient()
        {
        }

        #region Initialize

        public async Task Initialize(OrionSettings settings)
        {
            _settings = settings;
            _remoteAddress = new EndpointAddress($"http://{settings.IPAddress}:{settings.Port}/soap/IOrionPro");

            InitializeClient();
            await InitializeToken(_settings.TokenLifetime);
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
            var hash = GetMd5Hash(_settings.EmployeePassword);
            _token = await Execute<GetLoginTokenResponse, string>(_client.GetLoginTokenAsync(_settings.EmployeeUserName, hash));
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
        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            await ExtendTokenExpiration();
            _timer.Enabled = true;
        }
        private async Task ExtendTokenExpiration()
        {
            _token = await Execute<ExtendTokenExpirationResponse, string>(_client.ExtendTokenExpirationAsync(_token));
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

        public async Task<TVisitData[]> GetVisits()
        {
            return await Execute<GetVisitsResponse, TVisitData[]>(_client.GetVisitsAsync(_token));
        }

        public async Task<TPersonData[]> GetPersons(bool withoutPhoto, int offset, int count, string[] filter, bool isEmployees, bool isVisitors)
        {
            return await Execute<GetPersonsResponse, TPersonData[]>(_client.GetPersonsAsync(withoutPhoto, offset, count, filter, isEmployees, isVisitors, _token));
        }
        public async Task<int> GetPersonsCount()
        {
            return await Execute<GetPersonsCountResponse, int>(_client.GetPersonsCountAsync(_token));
        }

        public async Task<TTimeWindow[]> GetTimeWindows()
        {
            return await Execute<GetTimeWindowsResponse, TTimeWindow[]>(_client.GetTimeWindowsAsync(_token));
        }

        public async Task<TKeyData> GetKeyData(string code, int codeType)
        {
            return await Execute<GetKeyDataResponse, TKeyData>(_client.GetKeyDataAsync(code, codeType, _token));
        }

        public async Task<TKeyData[]> GetKeys(int offset, int count)
        {
            return await Execute<GetKeysResponse, TKeyData[]>(_client.GetKeysAsync(offset, count, _token));
        }

        public async Task<int> GetKeysCount()
        {
            return await Execute<GetKeysCountResponse, int>(_client.GetKeysCountAsync(_token));
        }

        public async Task<TAccessLevel> GetAccessLevelById(int id)
        {
            return await Execute<GetAccessLevelByIdResponse, TAccessLevel>(_client.GetAccessLevelByIdAsync(id, _token));
        }

        public async Task<int> GetAccessLevelsCount()
        {
            return await Execute<GetAccessLevelsCountResponse, int>(_client.GetAccessLevelsCountAsync(_token));
        }

        public async Task<TAccessLevel[]> GetAccessLevels(int offset, int count)
        {
            return await Execute<GetAccessLevelsResponse, TAccessLevel[]>(_client.GetAccessLevelsAsync(offset, count,_token));
        }

        public async Task<TCompany[]> GetCompanies(bool isEmployees, bool isVisitors)
        {
            return await Execute<GetCompaniesResponse, TCompany[]>(_client.GetCompaniesAsync(isEmployees, isVisitors, _token));
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

            await Execute<AddExternalEventResponse, TExternalEvent>(_client.AddExternalEventAsync(externalEvent, _token));
        }

        public async Task ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId)
        {
            var accesspoint = new TItem()
            {
                ItemId = accesspointId,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Timestamp = DateTime.Now
            };
            await Execute<ControlItemsResponse, TItem>(_client.ControlItemsAsync(_token, new[] { accesspoint }, (int)commandId, (int)action, personId));
        }
        #endregion

        #region Utils
        private async Task<Y> Execute<T, Y>(Task<T> task)
        {
            try
            {
                var result = await task;

                var @return = result.GetType().GetField("return").GetValue(result);
                var returnProps = @return.GetType().GetProperties();

                Y operationResult = (Y)returnProps.Single(x => x.Name.Equals("OperationResult")).GetValue(@return);
                bool success = (bool)returnProps.Single(x => x.Name.Equals("Success")).GetValue(@return);
                TServiceError serviceError = (TServiceError)returnProps.Single(x => x.Name.Equals("ServiceError")).GetValue(@return);

                if (!success && serviceError != null)
                {
                    var message = $"Ошибка сервера (код {serviceError.ErrorCode}): {serviceError.Description}";
                    var innerException = new Exception(serviceError.InnerExceptionMessage);
                    throw new InvalidOperationException(message, innerException);
                }

                return operationResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении метода: {ex}");
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
}