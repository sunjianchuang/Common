using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Common.Helper
{
    public class BitmapHelper
    {
        public static void SaveTo(BitmapSource source, string destUri)
        {
            var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = System.IO.Path.Combine(myDocuments, destUri + ".jpg");

            var encoder = new JpegBitmapEncoder() { QualityLevel = 100 };
            encoder.Frames.Add(BitmapFrame.Create(source));
            using (FileStream stream = new FileStream(path, FileMode.Create))
                encoder.Save(stream);
        }

        public static BitmapSource LoadBitmapByPath(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            //这句话一定要有，否则会有创建的图片不是path对应的图片的问题
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
            return new WriteableBitmap(bitmap);
        }
    }

    public class JsonHelper
    {
        public static string Serialize<T>(T obj)
        {
            string content = JsonConvert.SerializeObject(obj,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                });
            return content;
        }

        public static T DeSerialize<T>(string content)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            });
        }
    }


    /// <summary>  
    /// 有关HTTP请求的辅助类  
    /// </summary>  
    public class HttpWebHelper
    {
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        /// <summary>  
        /// 创建GET方式的HTTP请求  
        /// </summary>  
        /// <param name="url">请求的URL</param>  
        /// <param name="timeout">请求的超时时间</param>  
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>  
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>  
        /// <returns>返回HttpWebResponse对象，一定需要关闭，否则在多线程或者循环情况下会出错(连接数不够用)</returns>  
        public static HttpWebResponse CreateGetHttpResponse(string url, int? timeout, bool keepAlive, string userAgent, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            GC.Collect();
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.AllowWriteStreamBuffering = false;
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Default);
            request.UserAgent = DefaultUserAgent;
            request.KeepAlive = keepAlive;
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            return request.GetResponse() as HttpWebResponse;
        }
        /// <summary>  
        /// 创建POST方式的HTTP请求  
        /// </summary>  
        /// <param name="url">请求的URL</param>  
        /// <param name="parameters">随同请求POST的参数名称及参数值字典</param>  
        /// <param name="timeout">请求的超时时间</param>  
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>  
        /// <param name="requestEncoding">发送HTTP请求时所用的编码</param>  
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>  
        /// <returns></returns>  
        public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters, int? timeout, string userAgent, Encoding requestEncoding, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            if (requestEncoding == null)
            {
                throw new ArgumentNullException("requestEncoding");
            }
            HttpWebRequest request = null;
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            else
            {
                request.UserAgent = DefaultUserAgent;
            }

            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            //如果需要POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, parameters[key]);
                    }
                    i++;
                }
                byte[] data = requestEncoding.GetBytes(buffer.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            return request.GetResponse() as HttpWebResponse;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

        public static HttpWebResponse CreatePostHttpResponseEx(string url, string parameters, int? timeout, string userAgent, Encoding requestEncoding, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            if (requestEncoding == null)
            {
                throw new ArgumentNullException("requestEncoding");
            }
            HttpWebRequest request = null;
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            else
            {
                request.UserAgent = DefaultUserAgent;
            }

            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            //如果需要POST数据
            if (parameters != null)
            {
                StringBuilder buffer = new StringBuilder();
                buffer.Append(parameters);
                byte[] data = requestEncoding.GetBytes(buffer.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            return request.GetResponse() as HttpWebResponse;
        }
    }

    public class MathHelper
    {
        public static Point Center(Rect rect)
        {
            return new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
        }
        public static Point Center(Point p1, Point p2)
        {
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }

        public static double DistanceBetweenTwoPoints(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        public static bool LeftOfLine(Point p, Point p1, Point p2)
        {
            double tmpx = (p1.X - p2.X) / (p1.Y - p2.Y) * (p.Y - p2.Y) + p2.X;
            if (tmpx > p.X)//当tmpx>p.x的时候，说明点在线的左边，小于在右边，等于则在线上。
                return true;
            return false;
        }

        /// <summary>
        /// Rect坐标系变换，变换到指定角度的坐标系下
        /// </summary>
        /// <param name="inputRect"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Rect RectToRect(Rect inputRect, double angle)
        {
            var rt = new RotateTransform(angle);
            var leftTop = rt.Transform(inputRect.TopLeft);
            var bottomRight = rt.Transform(inputRect.BottomRight);
            var newRect = new Rect(leftTop, bottomRight);

            var vector = Center(inputRect) - Center(newRect);
            newRect.Offset(vector);
            return newRect;
        }
    }

    public class TPLHelper
    {
        public static bool Timeout(int timeoutMS, Action action)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var start = DateTime.Now;
            var task = Task.Factory.StartNew(() =>
            {
                action();
                if (token.IsCancellationRequested)
                {
                    //
                }
                token.ThrowIfCancellationRequested();
            });

            //timeout时间到了就取消该线程任务，并返回false。如果操作在3秒内完成了，那么久返回true
            if (!task.Wait(timeoutMS, token))
            {
                tokenSource.Cancel();
                return false;
            }
            var span = DateTime.Now - start;
            return true;

            //时间到了并不会停止任务执行，而是等待任务执行完成
            task.Wait(timeoutMS);
        }
    }
    /// <summary>
    /// Contains extension methods for enumerating the children of an element.    /// 
    /// </summary>
    public static class DependencyObjectHelper
    {
        private static DependencyObject GetParent(this DependencyObject element)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent == null)
            {
                FrameworkElement element2 = element as FrameworkElement;
                if (element2 != null)
                {
                    parent = element2.Parent;
                }
            }
            return parent;
        }

        public static IEnumerable<DependencyObject> GetParents(this DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            while (true)
            {
                element = element.GetParent();
                if (element == null)
                {
                    yield break;
                }
                yield return element;
            }
        }

        /// <summary>
        /// 返回第一个指定类型的父亲
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        public static T ParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            if (element == null)
            {
                return default(T);
            }
            return element.GetParents().OfType<T>().FirstOrDefault<T>();
        }

        /// <summary>
        /// Finds child element of the specified type. Uses breadth-first search.
        /// 
        /// </summary>
        /// <typeparam name="T">The type of the child that will be searched in the object hierarchy. The type should be 
        /// <see cref="T:System.Windows.DependencyObject"/>.
        ///             </typeparam><param name="element">The target 
        ///             <see cref="T:System.Windows.DependencyObject"/> which children will be traversed.</param>
        /// <returns>
        /// The first child element that is of the specified type.
        /// </returns>
        public static T FindFirstChildByType<T>(this DependencyObject element) where T : DependencyObject
        {
            return element.ChildrenOfType<T>().FirstOrDefault<T>();
        }

        /// <summary>
        /// 返回最后一个符合指定类型的孩子
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        public static T FindLastChildByType<T>(this DependencyObject element) where T : DependencyObject
        {
            return element.ChildrenOfType<T>().LastOrDefault<T>();
        }

        /// <summary>
        /// Gets all child elements recursively from the visual tree by given type.
        /// </summary>
        public static IEnumerable<T> ChildrenOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            return element.GetChildrenRecursive().OfType<T>();
        }

        private static IEnumerable<DependencyObject> GetChildrenRecursive(this DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(element, i);
                yield return child;
                foreach (DependencyObject iteratorVariable2 in child.GetChildrenRecursive())
                {
                    yield return iteratorVariable2;
                }
            }
        }

    }
