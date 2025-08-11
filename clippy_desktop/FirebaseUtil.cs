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

        public async static Task<ClipItem?> CreateClip(FirebaseClient client, Clippy.viewmodels.User user, ClipItem clip)
        {
            try
            {
                if (clip.Id == "")
                {
                    clip.Id = Guid.NewGuid().ToString();
                }
                var res = await client.Child($"clips/{user.Id}/{clip.Id}").PostAsync(clip.ToJSONString(), false);
                return ClipItem.FromJSONString(res.Object);
            }
            catch (Exception ex)
            {

            }
            return null;
        } 
        public async static Task<bool> UpdateClip(FirebaseClient client, Clippy.viewmodels.User user, ClipItem clip)
        {
            try
            {
                await client.Child($"clips/{user.Id}/{clip.Id}").PatchAsync(clip.ToJSONString());
                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public async static Task<ClipItem?> FindClip(FirebaseClient client, Clippy.viewmodels.User user, string id)
        {
            try
            {
                var res = await client.Child($"clips/{user.Id}/{id}").OnceSingleAsync<ClipItem>();
                return res;
            }
            catch (Exception ex)
            {

            }
            return null;
        }
        public async static Task<bool> DeleteClip(FirebaseClient client, Clippy.viewmodels.User user, string id)
        {
            try
            {
                await client.Child($"clips/{user.Id}/{id}").DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public async static Task<List<ClipItem>> ListClips(FirebaseClient client, Clippy.viewmodels.User user)
        {
            try
            {
                var res = await client.Child($"clips/{user.Id}").OnceAsJsonAsync();
                var data = (JToken.Parse(res) as JArray).Select(e => ClipItem.FromJSON((e as JObject).Properties().First().Value as JObject)).ToList();
                return data;
            }
            catch (Exception ex)
            {

            }
            return [];
        }
    }
}
