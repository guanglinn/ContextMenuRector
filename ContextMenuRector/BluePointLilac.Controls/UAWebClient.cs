using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.ComponentModel;

namespace ContextMenuRector.BluePointLilac.Controls
{
    public sealed class UAWebClient : IDisposable
    {
        private readonly HttpClient client;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public UAWebClient()
        {
            var handler = new HttpClientHandler();
            client = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36 Edg/90.0.818.66");
            this.Encoding = Encoding.UTF8;
        }

        public Encoding Encoding { get; set; }

        public event ProgressChangedEventHandler DownloadProgressChanged;
        public event AsyncCompletedEventHandler DownloadFileCompleted;

        public void Dispose()
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
            client.Dispose();
        }

        /// <summary>同步获取网页文本（包装 HttpClient）</summary>
        public string DownloadString(string url)
        {
            try
            {
                var str = client.GetStringAsync(url).GetAwaiter().GetResult();
                return str?.Replace("\n", Environment.NewLine);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>保留旧 API 名称</summary>
        public string GetWebString(string url) => DownloadString(url);

        /// <summary>同步获取二进制数据</summary>
        public byte[] DownloadData(string url)
        {
            try
            {
                return client.GetByteArrayAsync(url).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>异步下载文件并报告进度</summary>
        public void DownloadFileAsync(Uri address, string fileName)
        {
            var token = cts.Token;
            _ = Task.Run(async () =>
            {
                Exception error = null;
                bool cancelled = false;
                try
                {
                    using var resp = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                    var total = resp.Content.Headers.ContentLength ?? -1L;
                    using var stream = await resp.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    var buffer = new byte[8192];
                    long read = 0;
                    int len;
                    while ((len = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, len, token).ConfigureAwait(false);
                        read += len;
                        if (total > 0)
                        {
                            int percent = (int)(read * 100L / total);
                            DownloadProgressChanged?.Invoke(this, new ProgressChangedEventArgs(percent, null));
                        }
                    }
                    if (total <= 0)
                    {
                        DownloadProgressChanged?.Invoke(this, new ProgressChangedEventArgs(100, null));
                    }
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(error, cancelled, null));
                }
            }, token);
        }

        /// <summary>请求取消当前异步下载</summary>
        public void CancelAsync()
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch { }
            // create a fresh token source for future operations
            cts = new CancellationTokenSource();
        }

        /// <summary>将网络文本写入本地文件</summary>
        public bool WebStringToFile(string filePath, string fileUrl)
        {
            var contents = DownloadString(fileUrl);
            var flag = contents != null;
            if (flag) File.WriteAllText(filePath, contents, Encoding.Unicode);
            return flag;
        }

        /// <summary>获取网页Json文本并加工为Xml</summary>
        public XmlDocument GetWebJsonToXml(string url)
        {
            try
            {
                byte[] bytes = DownloadData(url);
                if (bytes == null) return null;
                using (XmlReader xReader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xReader);
                    return doc;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
