using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using static System.Console;

namespace AcidChicken.CommonsUpdater
{
    using Models;

    partial class Program
    {
        static async Task Main(string[] args)
        {
            await GetCookiesAsync();

            var id = SetHeaders();

            while (!await CheckLoginAsync())
                ReadKey(true);

            var user = await CheckUserAsync(id ?? SetHeaders());

            await SetCookiesAsync();

            var target = await GetTargetAsync();

            var list = await GetListsAsync(target.Id);

            await UpdateAsync(target.Id, list.Select(x => x.Id).Distinct().ToArray());

            ReadKey(true);
        }

        enum PreviousStatus
        {
            Initial = 0,
            Success = 1,
            Invalid = 2,
            Already = 3,
            Noexist = 4
        }

        static bool ValidChar(char x) =>
            '0' <= x && x <= '9' ||
            'A' <= x && x <= 'Z' ||
            'a' <= x && x <= 'z';

        static async Task<ContentInfo> GetTargetAsync()
        {
            while (true)
            {
                WriteMessage("対象の作品ID: ", WriteType.Input, true);

                var read = ReadLine().Trim();

                if (read.Length != 0 && read.All(ValidChar))
                {
                    var content = await CheckIdAsync(read);
                    if (content is ContentInfo)
                    {
                        WriteMessage($"{content} を対象の作品として指定しました。", WriteType.Success);
                        return content;
                    }

                    WriteMessage("コンテンツが存在しないか、コンテンツツリーを編集できません。", WriteType.Failure);
                }
                else
                    WriteMessage("IDの書式が間違っています。余分な文字が入っていないかを確認してもう一度入力して下さい。", WriteType.Failure);
            }
        }

        static async Task<IEnumerable<ContentInfo>> GetListsAsync(string id)
        {
            var list = new List<ContentInfo>();

            async Task<bool> StillAddingAsync()
            {
                WriteMessage("続行しますか？", WriteType.Select);

                switch (ReadChoice("リストを確認", "追加でリストに追加", "付け足して保存", "上書き保存"))
                {
                    case 0:
                    {
                        WriteMessage("リストには現在以下の作品が登録されています。");

                        foreach (var item in list)
                            WriteMessage(item.ToString(), WriteType.Memo);

                        return await StillAddingAsync();
                    }
                    case 1:
                        return true;
                    case 2:
                    {
                        list.AddRange(await CheckTreeAsync(id));

                        return false;
                    }
                    case 3:
                        return false;
                    default:
                        return await StillAddingAsync();
                }
            }

            do
                list.AddRange(await GetListAsync());
            while (await StillAddingAsync());

            return list.Distinct();
        }

        static async Task<IEnumerable<ContentInfo>> GetListAsync()
        {
            WriteMessage("親作品の設定方法を選択して下さい。", WriteType.Select);

            switch (ReadChoice("手入力で設定", "ファイルから設定", "マイリストから設定", "オリジナル作品として設定"))
            {
                case 0:
                {
                    var list = new List<ContentInfo>();
                    var previous = PreviousStatus.Initial;

                    while (true)
                    {
                        switch (previous)
                        {
                            case PreviousStatus.Success:
                                WriteMessage($"{list.LastOrDefault()} を登録しました。全てのIDを登録し終えたら「.」（ドット）を入力して完了します。", WriteType.Success);
                                break;
                            case PreviousStatus.Invalid:
                                WriteMessage("IDの書式が間違っています。余分な文字が入っていないかを確認してもう一度入力して下さい。", WriteType.Failure);
                                break;
                            case PreviousStatus.Already:
                                WriteMessage("そのIDは既に登録されています。全てのIDを登録し終えたら「.」（ドット）を入力して完了します。", WriteType.Warning);
                                break;
                            case PreviousStatus.Noexist:
                                WriteMessage("コンテンツが存在しないか、親作品に登録できません。", WriteType.Failure);
                                break;
                        }

                        WriteMessage("親作品ID: ", WriteType.Input, true);

                        var read = ReadLine().Trim();

                        if (read.Length == 0)
                            previous = PreviousStatus.Initial;
                        else if (read == ".")
                        {
                            WriteMessage($"{list.Count} 個のIDを登録しました。", WriteType.Success);
                            return list;
                        }
                        else if (!read.All(ValidChar))
                            previous = PreviousStatus.Invalid;
                        else if (list.Exists(x => x.Id == read))
                            previous = PreviousStatus.Already;
                        else
                        {
                            var info = await CheckIdAsync(read);
                            if (info is null)
                                previous = PreviousStatus.Noexist;
                            else
                            {
                                list.Add(info);
                                previous = PreviousStatus.Success;
                            }
                        }
                    }
                }
                case 1:
                    while (true)
                    {
                        WriteMessage("リストファイルのパス: ", WriteType.Input, true);

                        var file = ReadLine();

                        if (File.Exists(file))
                        {
                            var read = await ReadFileAsync(file);

                            var ids = read
                                .Where(ValidChar)
                                .Aggregate(
                                    Enumerable.Repeat(read, 1),
                                    (a, c) => a.SelectMany(x => x.Split(c, StringSplitOptions.RemoveEmptyEntries)))
                                .Distinct()
                                .ToArray();

                            WriteMessage($"ファイルから {ids.Length} 個のIDを取得しました。検証を開始します。", WriteType.Success);
                            WriteMessage("ニコニコ動画サーバーの負荷を軽減するため、それぞれ少し時間を空けてコンテンツを検証します。", WriteType.Memo);
                            WriteMessage("結果として少々時間がかかる場合がありますが、ご了承下さい。", WriteType.Memo);

                            var progress = CreateConsoleProgressBar(ids.Length, "接続中");
                            var list = new List<ContentInfo>(ids.Length);

                            foreach (var id in ids)
                            {
                                var content = await CheckIdAsync(id);
                                if (content is null)
                                {
                                    ForegroundColor = ConsoleColor.DarkRed;
                                    progress.Report($"[失敗] {id} は存在しないか、親作品に登録できません。スキップします。");
                                }
                                else
                                {
                                    list.Add(content);

                                    ForegroundColor = ConsoleColor.Green;
                                    progress.Report($"[成功] {content}");
                                }
                            }

                            WriteMessage($"{ids.Length} 個中 {list.Count} のコンテンツを登録しました。", WriteType.Success);
                            return list;
                        }
                        else
                            WriteMessage("ファイルが存在しません。パスが正しいかを確認してもう一度入力して下さい。", WriteType.Failure);
                    }
                case 2:
                {
                    var mylists = await CheckMylists();

                    if (mylists.Any())
                    {
                        WriteMessage("使用するマイリストを選択して下さい。", WriteType.Select);

                        var contents = await CheckMylistContents(mylists[ReadChoice(mylists.Select(x => x.ToString()).ToArray())].Id);

                        WriteMessage($"{contents.Length} 個のコンテンツを登録しました。", WriteType.Success);

                        foreach (var item in contents)
                            WriteMessage(item.ToString(), WriteType.Memo);

                        return contents.Select(x => new ContentInfo
                        {
                            Id = x.VideoId,
                            Title = x.Title
                        });
                    }
                    else
                        WriteMessage("マイリストがありません。", WriteType.Error);

                    return await GetListAsync();
                }
                case 3:
                    return Enumerable.Repeat(new ContentInfo
                    {
                        Id = "none"
                    }, 1);
                default:
                    return Enumerable.Empty<ContentInfo>();
            }
        }
    }
}
