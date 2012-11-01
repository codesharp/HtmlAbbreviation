//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Majestic13;

namespace Abbreviation.Service
{
    /// <summary>
    /// HtmlParser基类
    /// </summary>
    public abstract class HtmlParserBase : IHtmlParser
    {
        protected string _tagStart;
        protected string _tagEnd;
        protected List<string> _filterTags;
        protected string _currentUrlDomain;
        protected ILog _logger;

        public HtmlParserBase()
        {
            _tagStart = "<{0}>";
            _tagEnd = "</{0}>";
            _filterTags = new List<string>();
            _filterTags.AddRange(new string[] { "html", "script", "head", "link", "style", "title", "meta", "input", "select", "option", "textarea", "img", "button" });
            _currentUrlDomain = null;
            _logger = DependencyResolver.Resolve<ILoggerFactory>().Create(GetType());
        }

        public virtual string ParseUrl(string url)
        {
            try
            {
                var formattedUrl = FormatUrl(url);
                SetUrlDomain(formattedUrl);
                var html = DownloadUrl(formattedUrl);
                if (!string.IsNullOrEmpty(html))
                {
                    return ProcessHtml(html);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("解析URL:{0}时遇到异常。", url), ex);
            }

            return null;
        }
        /// <summary>
        /// 处理指定的html内容，子类应该分析该指定的html内容，返回处理后的格式
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected virtual string ProcessHtml(string html)
        {
            return html;
        }
        /// <summary>
        /// 格式化指定的url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected string FormatUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                return "http://" + url;
            }

            return url;
        }
        /// <summary>
        /// 获取url的domain部分
        /// </summary>
        /// <param name="url"></param>
        protected void SetUrlDomain(string url)
        {
            var uri = new Uri(url);
            _currentUrlDomain = string.Format("{0}://{1}", uri.Scheme, uri.Authority);
        }
        /// <summary>
        /// Download the content of the given url.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected string DownloadUrl(string url)
        {
            string content = "";
            ServicePointManager.ServerCertificateValidationCallback = (srvPoint, certificate, chain, errors) => true;
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Encoding encoding = Encoding.UTF8;
            string charset = response.CharacterSet;
            if (charset != null)
            {
                encoding = Encoding.GetEncoding(charset);
            }

            //read response into memory stream
            MemoryStream memoryStream;
            using (Stream responseStream = response.GetResponseStream())
            {
                memoryStream = new MemoryStream();

                byte[] buffer = new byte[1024];
                int byteCount;
                do
                {
                    byteCount = responseStream.Read(buffer, 0, buffer.Length);
                    memoryStream.Write(buffer, 0, byteCount);
                } while (byteCount > 0);
            }

            //set stream position to beginning
            memoryStream.Seek(0, SeekOrigin.Begin);

            StreamReader streamReader = new StreamReader(memoryStream, encoding);
            content = streamReader.ReadToEnd();

            try
            {
                //check real charset meta-tag in HTML
                string realCharset = GetCharSet(content);
                if (!string.IsNullOrWhiteSpace(realCharset) && realCharset.ToLower() != charset.ToLower())
                {
                    //get correct encoding
                    Encoding correctEncoding = Encoding.GetEncoding(realCharset);

                    //reset stream position to beginning
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    //reread response stream with the correct encoding
                    StreamReader reader = new StreamReader(memoryStream, correctEncoding);
                    content = reader.ReadToEnd();
                    reader.Close();
                }
            }
            finally
            {
                //dispose the first stream reader object
                streamReader.Close();
            }

            return content;
        }
        /// <summary>
        /// 从返回的html内容中分析出charset
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string GetCharSet(string content)
        {
            int charsetStart = content.IndexOf("charset=", StringComparison.OrdinalIgnoreCase);
            int charsetEnd = 0;

            if (charsetStart > 0)
            {
                charsetStart += 8;

                var nextChar = content.Substring(charsetStart, 1);
                if (nextChar == "'")
                {
                    charsetStart += 1;
                    charsetEnd = content.IndexOf('\'', charsetStart);
                }
                else if (nextChar == "\"")
                {
                    charsetStart += 1;
                    charsetEnd = content.IndexOf('\"', charsetStart);
                }
                else
                {
                    charsetEnd = content.IndexOfAny(new[] { ' ', '\"', '\'', ';' }, charsetStart);
                }

                if (charsetEnd > charsetStart)
                {
                    return content.Substring(charsetStart, charsetEnd - charsetStart);
                }
            }

            return null;
        }
    }
    public static class Extensions
    {
        public static List<T> Clone<T>(this List<T> list)
        {
            var items = new List<T>();
            list.ForEach(x => items.Add(x));
            return items;
        }
        /// <summary>
        /// 判断指定的Tag是否是一个空标签
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool IsEmptyTag(this HtmlNode.Tag tag)
        {
            if (tag.Children == null || tag.Children.Count == 0)
            {
                return true;
            }
            if (tag.Children.All(x =>
                (x is HtmlNode.Text && string.IsNullOrWhiteSpace((x as HtmlNode.Text).Value))
                || (x is HtmlNode.Comment && string.IsNullOrWhiteSpace((x as HtmlNode.Comment).Value))
                || x is HtmlNode.Script))
            {
                return true;
            }

            return false;
        }
    }
}
