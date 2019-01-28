using System.IO;
using System.Net;
using System.Threading.Tasks;

using static System.Environment;

namespace AcidChicken.CommonsUpdater
{
    partial class Program
    {
        static async Task GetCookiesAsync()
        {
            var directory = Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "CommonsUpdater");
            Directory.CreateDirectory(directory);

            var read = await ReadFileAsync(Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "CommonsUpdater", "nicovideo.jp"));
            if (read is string)
                foreach (var item in read.Split(';'))
                {
                    var pair = item.Split('=');
                    _handler.CookieContainer.Add(new Cookie(pair[0].Trim(), pair[1].Trim(), "/", ".nicovideo.jp"));
                }
        }

        static async Task SetCookiesAsync()
        {
            var directory = Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "CommonsUpdater");
            Directory.CreateDirectory(directory);

            var file = Path.Combine(directory, "nicovideo.jp");
            using (var stream = File.Open(file, FileMode.Create))
            using (var writer = new StreamWriter(stream))
                await writer.WriteLineAsync(_handler.CookieContainer.GetCookieHeader(_uri));
        }

        static async Task<string> ReadFileAsync(string path)
        {
            if (File.Exists(path))
                using (var file = File.OpenText(path))
                    return await file.ReadToEndAsync();
            else
                return null;
        }
    }
}
