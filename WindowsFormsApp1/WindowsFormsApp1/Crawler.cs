using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Linq;

namespace WindowsFormsApp1
{
    public class Data
    {
        public string url { get; set; }
        public List<string> imagesLinks { get; set; }
        public List<string> subLinks { get; set; }
        public List<Data> childs { get; set; }
        public long time { get; set; }
    }

    class Crawler
    {
        public bool finished = false;
        public List<string> images = new List<string>();
        public List<string> subLinks = new List<string>();
        public List<Crawler> childs = new List<Crawler>();

        object imagesLocker = new object();
        object subLinksLocker = new object();
        object childLocker = new object();

        private readonly string _url;
        private readonly int _depth;

        Stopwatch stopwatch = new Stopwatch();
        long time;

        public Crawler(string url, int depth)
        {
            _url = url;
            _depth = depth;
        }

        private Data CreateDataToJson()
        {
            Data data = new Data()
            {
                url = this._url,
                imagesLinks = this.images,
                subLinks = this.subLinks,
                time = this.time,
            };

            List<Data> datas = new List<Data>();

            foreach (var child in this.childs)
            {
                datas.Add(child.CreateDataToJson());
            }

            data.childs = datas;

            return data;
        }

        public void SaveToJson(string path)
        {
            Data data = CreateDataToJson();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(data, options);
            try
            {
                File.WriteAllText(path, json);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied: {ex.Message}");
            }
        }

        public async Task Crawl()
        {
            if (_depth == 0) { return; }
            stopwatch.Start();
            await GetImageLinks();
            await GetSubLinks();
            await GetChilds();
            stopwatch.Stop();

            time = stopwatch.ElapsedMilliseconds;
            finished = true;
        }

        private async Task GetSubLinks()
        {
            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument htmlDoc;

            try
            {
                htmlDoc = await web.LoadFromWebAsync(_url);
            }
            catch (Exception ex) { return; }

            if (htmlDoc is null) { return; }

            var links = htmlDoc.DocumentNode.SelectNodes("//a");

            if (links is null) { return; }

            foreach (var link in links)
            {
                string src = link.Attributes["href"]?.Value;
                if (src is null) { continue; }
                if (!src.Contains("https://"))
                {
                    src = _url + src;
                }

                if (_url + '/' == src)
                {
                    continue;
                }

                lock (subLinksLocker)
                {
                    subLinks.Add(src);
                }
            }
        }

        private async Task GetChilds()
        {
            if (_depth == 1) { return; }

            var tasks = new List<Task>();

            foreach (var subLink in subLinks)
            {
                var child = new Crawler(subLink, _depth - 1);

                // Запускаем задачу для каждого подлинка
                tasks.Add(Task.Run(async () =>
                {
                    await child.Crawl();
                    lock (childLocker)
                    {
                        childs.Add(child);
                    }
                }));
            }

            // Ожидаем завершения всех задач
            await Task.WhenAll(tasks);

            Console.WriteLine("Ending collecting childs...");
        }

        private async Task GetImageLinks()
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc;
            try
            {
                htmlDoc = await web.LoadFromWebAsync(_url);
            }
            catch (Exception ex) { return; }

            var links = htmlDoc.DocumentNode.SelectNodes("//img");
            if (links is null) { return; }

            foreach (var link in links)
            {
                string src = link.Attributes["src"]?.Value;
                if (src is null) { continue; }
                if (!src.Contains("https://"))
                {
                    src = _url + src;
                }
                lock (imagesLocker)
                {
                    images.Add(src);
                }
            }
        }
    }
}
