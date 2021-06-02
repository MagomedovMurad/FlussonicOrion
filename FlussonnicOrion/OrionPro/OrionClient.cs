﻿using FlussonnicOrion.OrionPro.Enums;
using FlussonnicOrion.OrionPro.Models;
using Orion;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonnicOrion.OrionPro
{
    public class OrionClient
    {
        private OrionProClient _client;
        private EndpointAddress _remoteAddress;
        private OrionSettings _settings;
        private string _token;
        private Timer _timer;

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
            if (_settings.ModuleUserName!= null && _settings.ModulePassword != null)
            {
                _client.ClientCredentials.UserName.UserName = _settings.ModuleUserName;
                _client.ClientCredentials.UserName.Password = _settings.ModulePassword;
            }
        }
        private async Task InitializeToken(int tokenLifetime)
        {
            try
            {
                var hash = GetMd5Hash(_settings.EmployeePassword);
                var response = await _client.GetLoginTokenAsync(_settings.EmployeeUserName, hash);
                if (!CheckResponse(response.@return.Success, response.@return.ServiceError))
                    return;

                _token = response.@return.OperationResult;
                StartTokenExpirationExtending(tokenLifetime);
            }
            catch (Exception ex)
            { 
                
            }
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
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ExtendTokenExpiration();
        }
        private async Task ExtendTokenExpiration()
        {
            var response = await _client.ExtendTokenExpirationAsync(_token);
            if (CheckResponse(response.@return.Success, response.@return.ServiceError))
                _token = response.@return.OperationResult;
        }
        private string GetMd5Hash(string data)
        {
            using (var md5 = MD5.Create())
            {
                var sourceBytes = Encoding.UTF8.GetBytes(data);
                var hashBytes = md5.ComputeHash(sourceBytes);
                var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
                return hash;
            }
        }

        #endregion

        #region Utils
        private bool CheckResponse(bool isSucces, TServiceError error)
        {
            if (isSucces)
                return true;

            Console.WriteLine($"Error code: {error.ErrorCode}. {error.Description}. InnerException: {error.InnerExceptionMessage}");
            return false;
        }
        #endregion

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            }
        }

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
            return await Execute<GetKeyDataResponse, TKeyData> (_client.GetKeyDataAsync(code, codeType, _token));
        }

        public async Task<TAccessLevel> GetAccessLevelById(int id)
        {
            return await Execute<GetAccessLevelByIdResponse, TAccessLevel>(_client.GetAccessLevelByIdAsync(id, _token));
        }

        private async Task<Y> Execute<T,Y>(Task<T> task)
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

        public async Task Test()
        {
            try
            {
                await GetVisits();
                //var g = await _client.GetTimeWindowsAsync(_token);

                //var key = _client.GetKeyDataAsync("Е340РХ126", 5, _token).Result;
                //var person = _client.GetPersonByPassAsync("Е340РХ126", true, 5, _token).Result;
                //var tt = _client.GetPersonByIdAsync(2, true, _token).Result;
                //var tpersondata = new TPersonData()
                //{
                //    Id = 1,
                //    Photo = new byte[0]
                //};
                //var t = _client.GetPersonPassListAsync(tpersondata, _token).Result;
                //var cars = await _client.GetCarsAsync(_token);
                //var items = await _client.GetItemsForLoginAsync(_token, "admin123", null);
                //var items3 = await _client.GetItemsAsync(_token);
                //var i1111temsStates = await _client.GetItemsStatesAsync(_token, null);
                //await AddExternalEvent();
                // await ControlAccesspoint(1, AccesspointCommand.ProvisionOfAccess, ActionType.Passage, 2);
                // await AddExternalEvent(2, 2, "Тестовое событие");

                // var tt2 = _client.GetPersonsCountAsync(null).Result;
                //var tt1 = _client.GetPersonsAsync(true, 0, 0, null, false, false, null).Result;
                //var tt = _client.GetCarsAsync(null).Result;
                //var t7 = _client.GetKeysAsync(0, 50, null).Result;
            }
            catch (Exception ex)
            { 
            
            }
        }


        public async Task AddExternalEvent()
        {
            var externalEvent = new TExternalEvent()
            {
                Id = 300,
                ItemId = 1,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Event = 1,
                KeyId = 0,
                PersonId = 0,
                TimeStamp = DateTime.Now,
                Text = "Тестовое внешнее событие"
            };
            var tt = await _client.AddExternalEventAsync(externalEvent, _token);
            var yy = tt.@return.OperationResult;
        }

        public async Task<bool> ControlAccesspoint(int accesspointId, AccesspointCommand commandId, ActionType action, int personId)
        {
            var accesspoint = new TItem()
            {
                ItemId = accesspointId,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Timestamp = DateTime.Now
            };
            var items = await _client.GetItemsAsync(_token);
            var response = await _client.ControlItemsAsync(_token, new[] { accesspoint }, (int)commandId, (int)action, personId);
            var result = response.@return.OperationResult;
            return response.@return.Success && result.Select(x => x.ItemId).Contains(accesspointId);
        }
    }
}