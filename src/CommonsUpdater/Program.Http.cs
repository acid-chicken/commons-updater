using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
#if !utf8json
using Utf8Json;
using Utf8Json.Resolvers;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#endif

using static System.Console;
using static System.Text.Encoding;

namespace AcidChicken.CommonsUpdater
{
    using Models;

    partial class Program
    {
        static Uri _uri = new Uri("https://nicovideo.jp/");

        static HttpClientHandler _handler = new HttpClientHandler
        {
            UseCookies = true
        };

        static HttpClient _http = new HttpClient(_handler);

#if !utf8json
#else
        static JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
#endif

        static async Task<bool> CheckLoginAsync()
        {
            try
            {
                using (var response = await _http.GetAsync("https://public.api.nicovideo.jp/v1/user/followees/niconico-users/51391221.json"))
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
#if !utf8json
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            var json = await JsonSerializer.DeserializeAsync<Following>(stream, StandardResolver.SnakeCase);
                            if (!json?.Data?.Following ?? true)
                                WriteMessage("差し支えなければ作者のアカウントをフォローして下さると幸いです。 https://www.nicovideo.jp/user/51391221", WriteType.Warning);
                        }
#else

                        var raw = await response.Content.ReadAsStringAsync();
                        var json = JsonConvert.DeserializeObject<Following>(raw, _settings);
                        if (!json?.Data?.Following ?? true)
                            WriteMessage("差し支えなければ作者のアカウントをフォローして下さると幸いです。 https://www.nicovideo.jp/user/51391221", WriteType.Warning);
#endif
                        return true;

                    case HttpStatusCode.Unauthorized:
                        WriteMessage("ログインを開始します。ウィザードに従ってログインして下さい。");

                        WriteMessage("メールアドレス or 電話番号: ", WriteType.Input, true);
                        var mailTel = ReadLine();

                        WriteMessage("パスワード: ", WriteType.Input, true);
                        var password = ReadPassword();

                        using (var challenge = await _http.PostAsync("https://account.nicovideo.jp/api/v1/login", new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "mail_tel", mailTel },
                            { "password", password }
                        })))
                            if (_handler.CookieContainer.GetCookies(_uri)["user_session"] is Cookie &&
                                _handler.CookieContainer.GetCookies(_uri)["user_session_secure"] is Cookie)
                            {
                                WriteMessage("ログインに成功しました。", WriteType.Success);
                                return true;
                            }
                            else
                            {
                                WriteMessage("ログインに失敗しました。いずれかのキーを押して再試行します。", WriteType.Failure);
                                return false;
                            }
                }
            }
            catch (HttpRequestException e)
            {
                WriteMessage("APIの接続に失敗しました。インターネットに接続されていないか、ニコニコ動画がメンテナンス中の可能性があります。いずれかのキーを押して再試行します。", WriteType.Error);
                WriteStack(e);
            }
            catch (Exception e)
            {
                WriteMessage("不明なエラーが発生しました。", WriteType.Error);
                WriteStack(e);
            }
            return false;
        }

        static async Task<ContentInfo> CheckIdAsync(string id)
        {
            string GetTitle(ReadOnlySpan<byte> source)
            {
                var span = source;

                {
                    ReadOnlySpan<byte> prefix = stackalloc byte[]
                    {
                        0x3c, 0x64, 0x69, 0x76, 0x20, 0x63, 0x6c, 0x61,
                        0x73, 0x73, 0x3d, 0x22, 0x64, 0x73, 0x63, 0x22,
                        0x3e
                    };

                    var i = span.IndexOf(prefix);
                    if (i != -1)
                        span = span.Slice(prefix.Length + i);
                }

                {
                    ReadOnlySpan<byte> suffix = stackalloc byte[]
                    {
                        0x3c, 0x2f, 0x64, 0x69, 0x76, 0x3e
                    };

                    var i = span.IndexOf(suffix);
                    if (i != -1)
                        span = span.Slice(0, i);
                }

                Span<char> buffer = stackalloc char[4096];

                return buffer.Slice(0, UTF8.GetChars(span, buffer)).ToString();
            }

            if (id == "none")
                return new ContentInfo
                {
                    Id = id
                };

            try
            {
                using (var response = await _http.PostAsync("http://commons.nicovideo.jp/cpp/ajax/item/search", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "id", id }
                })))
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    return bytes.Length == 0 ? null : new ContentInfo
                    {
                        Id = id,
                        Title = GetTitle(bytes)
                    };
                }
            }
            catch (HttpRequestException e)
            {
                WriteMessage("APIの接続に失敗しました。インターネットに接続されていないか、ニコニコ動画がメンテナンス中の可能性があります。", WriteType.Error);
                WriteStack(e);
            }
            catch (Exception e)
            {
                WriteMessage("不明なエラーが発生しました。", WriteType.Error);
                WriteStack(e);
            }

            return null;
        }

        static async Task<Mylist[]> CheckMylists()
        {
            try
            {
#if !utf8json
                using (var stream = await _http.GetStreamAsync("https://www.nicovideo.jp/api/mylistgroup/list"))
                {
                    var json = await JsonSerializer.DeserializeAsync<MylistGroup>(stream, StandardResolver.SnakeCase);
                    return json?.Mylistgroup ?? Array.Empty<Mylist>();
                }
#else
                var raw = await _http.GetStringAsync("https://www.nicovideo.jp/api/mylistgroup/list");
                var json = JsonConvert.DeserializeObject<MylistGroup>(raw, _settings);
                return json?.Mylistgroup ?? Array.Empty<Mylist>();
#endif
            }
            catch (HttpRequestException e)
            {
                WriteMessage("APIの接続に失敗しました。インターネットに接続されていないか、ニコニコ動画がメンテナンス中の可能性があります。", WriteType.Error);
                WriteStack(e);
            }
            catch (Exception e)
            {
                WriteMessage("不明なエラーが発生しました。", WriteType.Error);
                WriteStack(e);
            }

            return Array.Empty<Mylist>();
        }

        static async Task<ItemData[]> CheckMylistContents(string id)
        {
            try
            {
#if !utf8json
                using (var stream = await _http.GetStreamAsync($"https://www.nicovideo.jp/api/mylist/list?group_id={id}"))
                {
                    var json = await JsonSerializer.DeserializeAsync<Mylist>(stream, StandardResolver.SnakeCase);
                    return json?.Mylistitem?.Select(x => x.ItemData).Where(x => x.Deleted == "0").ToArray() ?? Array.Empty<ItemData>();
                }
#else
                var raw = await _http.GetStringAsync($"https://www.nicovideo.jp/api/mylist/list?group_id={id}");
                var json = JsonConvert.DeserializeObject<Mylist>(raw, _settings);
                return json?.Mylistitem?.Select(x => x.ItemData).Where(x => x.Deleted == "0").ToArray() ?? Array.Empty<ItemData>();
#endif
            }
            catch (HttpRequestException e)
            {
                WriteMessage("APIの接続に失敗しました。インターネットに接続されていないか、ニコニコ動画がメンテナンス中の可能性があります。", WriteType.Error);
                WriteStack(e);
            }
            catch (Exception e)
            {
                WriteMessage("不明なエラーが発生しました。", WriteType.Error);
                WriteStack(e);
            }

            return Array.Empty<ItemData>();
        }

        static async Task<UserInfo> CheckUserAsync(string id)
        {
            try
            {
                var response = await _http.GetStringAsync($"https://www.nicovideo.jp/user/{id}");
                var lines = response.Split('\n').Select(x => x.Trim()).Where(x => x.Length != 0).ToArray();
                var user = new UserInfo
                {
                    Nickname = string.Join('"', lines.FirstOrDefault(x => x.StartsWith("var nickname = \"")).Split('"').Skip(1).SkipLast(1))
                        .Replace('\\', '\0')
                        .Replace("\\\"", "\"")
                        .Replace('\0', '\\')
#if use_global_hash
                    ,
                    GlobalsHash = lines.FirstOrDefault(x => x.StartsWith("Globals.hash = '")).Split('\'')[1]
#endif
                };

#if use_global_hash
                if (user.GlobalsHash is string)
#endif
                {
                    WriteMessage($"{user.Nickname ?? "undefined"} さん、ようこそ。");

                    return user;
                }
            }
            catch (HttpRequestException e)
            {
                WriteMessage("APIの接続に失敗しました。インターネットに接続されていないか、ニコニコ動画がメンテナンス中の可能性があります。", WriteType.Error);
                WriteStack(e);
            }
            catch (Exception e)
            {
                WriteMessage("不明なエラーが発生しました。", WriteType.Error);
                WriteStack(e);
            }

            WriteMessage("トークンを取得できませんでした。", WriteType.Failure);
            return null;
        }

        static async Task UpdateAsync(string id, params string[] ids)
        {
            try
            {
                var page = await _http.GetStringAsync($"https://commons.nicovideo.jp/tree/edit/{id}");
                var token = page.Split('\n').Select(x => x.Trim()).Where(x => x.Length != 0).FirstOrDefault(x => x.StartsWith("var token = \""))?.Split('"')[1];

                using (var response = await _http.PostAsync("https://commons.nicovideo.jp/tree/update", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "parent", string.Join(',', ids) },
                    { "id", id },
                    { "_t", token }
                })))
                    if (response.StatusCode == HttpStatusCode.Found)
                        WriteMessage("コンテンツツリーの編集に成功しました。", WriteType.Success);
                    else
                        WriteMessage("コンテンツツリーの編集に失敗しました。", WriteType.Failure);
            }
            catch (HttpRequestException e)
            {
                WriteMessage("APIの接続に失敗しました。インターネットに接続されていないか、ニコニコ動画がメンテナンス中の可能性があります。", WriteType.Error);
                WriteStack(e);
            }
            catch (Exception e)
            {
                WriteMessage("不明なエラーが発生しました。", WriteType.Error);
                WriteStack(e);
            }
        }

        static string SetHeaders()
        {
            _http.DefaultRequestHeaders.Add("x-niconico-authflag", "0");

            var session = _handler.CookieContainer.GetCookies(_uri)["user_session"]?.Value.Split('_');

            if (session?.Length > 2)
            {
                var id = session[2];

                _http.DefaultRequestHeaders.Add("x-niconico-id", id);

                return id;
            }

            return null;
        }
    }
}