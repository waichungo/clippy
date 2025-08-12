using Avalonia.Logging;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database.Query;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using Clippy.viewmodels;
using Newtonsoft.Json;
using System.Reactive.Linq;

namespace Clippy
{
    public class HttpClientUtil
    {

        private static HttpClient? _httpClient = null;

        public static string MapToQuery(Dictionary<string, string> parameters)
        {
            var query = "";
            foreach (var param in parameters)
            {
                var q = $"{param.Key}=${WebUtility.UrlEncode(param.Value)}";
                query += (query.Length > 0) ? $"&{q}" : q;
            }
            return WebUtility.UrlEncode(query);
        }
        static HttpClientHandler? _httpHandler = null;
        public static HttpClient DefaultClient
        {
            get
            {
                if (_httpClient == null)
                {
                    _httpHandler ??= new HttpClientHandler { UseCookies = true };
                    _httpClient = new HttpClient(_httpHandler) { };
                }
                return _httpClient;
            }
        }
        public static void Dispose()
        {
            _httpHandler?.Dispose();
            _httpHandler = null;
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }
    public class FirebaseUtil
    {

        public async static Task<FirebaseClient> GetFirebaseClient(string token)
        {
            var firebaseClient = new FirebaseClient(
            "https://clippy-61292-default-rtdb.europe-west1.firebasedatabase.app/",
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => { return token; }
            });

            return firebaseClient;
        }
        public static FirebaseAuthClient CreateClient()
        {
            var client = new FirebaseAuthClient(GetFirebaseAuthConfig());
            return client;
        }

        public static FirebaseAuthConfig GetFirebaseAuthConfig()
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = "AIzaSyCor5tZ-T4b5pXpGr9HJ58TdWfEiSWeWXY",
                AuthDomain = "clippy-61292.firebaseapp.com",

                Providers =
                [
                    new EmailProvider()
                ],
                UserRepository = new FileUserRepository("Clippy"),// persist data into %AppData%\FirebaseSample

            };
            return config;
        }

        public async static Task<ClipItem?> CreateClip(FirebaseClient client, Clippy.viewmodels.User user, ClipItem clip, string? machineId = null)
        {
            try
            {
                if (clip.Id == "")
                {
                    clip.Id = Guid.CreateVersion7().ToString();
                }
                await client.Child($"clips/{user.Id}/{machineId ?? Utils.GetSystemUuid()}/{clip.Id}").PutAsync(clip.ToJSONString());
                clip.Synced = true;
                await UpdateClip(client, user, clip, machineId);
                return clip;
            }
            catch (Exception ex)
            {

            }
            return null;
        }
        public static string getClipItemRefString(viewmodels.User user, string? machineId = null)
        {
            return $"clips/{user.Id}/{machineId ?? Utils.GetSystemUuid()}";
        }
        public static string getDevicesRefString(viewmodels.User user)
        {
            return $"devices/{user.Id}";
        }
        public async static Task<bool> UpdateClip(FirebaseClient client, Clippy.viewmodels.User user, ClipItem clip, string? machineId = null)
        {
            try
            {
                await client.Child($"{getClipItemRefString(user)}/{clip.Id}").PatchAsync(clip.ToJSONString());
                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public async static Task<ClipItem?> FindClip(FirebaseClient client, Clippy.viewmodels.User user, string firebaseItemId, string? machineId = null)
        {
            try
            {
                var res = await client.Child($"{getClipItemRefString(user, machineId)}/{firebaseItemId}").OnceSingleAsync<ClipItem>();
                return res;
            }
            catch (Exception ex)
            {

            }
            return null;
        }
        public async static Task<bool> DeleteClip(FirebaseClient client, Clippy.viewmodels.User user, string firebaseItemId, string? machineId = null)
        {
            try
            {
                await client.Child($"{getClipItemRefString(user, machineId)}/{firebaseItemId}").DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public async static Task<List<ClipItem>> ListClips(FirebaseClient client, Clippy.viewmodels.User user, string? machineId = null, string? fromId = null)
        {
            try
            {
                var q = client.Child(getClipItemRefString(user, machineId)).OrderByKey();
                var res = "";
                if (fromId != null)
                {
                    res = await q.StartAt(fromId).OnceAsJsonAsync();
                }
                else
                {
                    res = await q.OnceAsJsonAsync();
                }
                var data = (JToken.Parse(res) as JObject).Properties().Where(e => e.Name != fromId).Select(e => ClipItem.FromJSON(e.Value as JObject)).ToList();
                return data;
            }
            catch (Exception ex)
            {

            }
            return [];
        }
        public async static Task<List<Device>> ListDevices(FirebaseClient client, Clippy.viewmodels.User user)
        {
            try
            {
                var q = client.Child(getDevicesRefString(user));
                var res = "";
                res = await q.OnceAsJsonAsync();

                var data = (JToken.Parse(res) as JObject).Properties().Select(e => (e.Value as JObject).ToObject<Device>()).ToList();
                return data;
            }
            catch (Exception ex)
            {
            }
            return [];
        }
        public async static Task<bool> PostDevice(FirebaseClient client, Clippy.viewmodels.User user, Device device)
        {
            try
            {
                await client.Child($"{getDevicesRefString(user)}/{device.Id}").PutAsync(JsonConvert.SerializeObject(device));
                return true;
            }
            catch (Exception ex)
            {
            }
            return false;
        }
        
    }
}
