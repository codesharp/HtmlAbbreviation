//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System;
using System.Text.RegularExpressions;

namespace Abbreviation.Service
{
    public interface IHtmlParserManager
    {
        /// <summary>
        /// 注册URL类型及其对应的URL纯文本解析HtmlParser
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parser"></param>
        void RegisterHtmlParser(HtmlParserKey key, IHtmlParser parser);
        /// <summary>
        /// 根据指定的url返回一个适当的IHtmlParser
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        IHtmlParser GetParser(string url);
    }

    public class HtmlParserKey
    {
        public string UrlRegexPattern { get; set; }

        public bool IsUrlMatch(string url)
        {
            if (!string.IsNullOrEmpty(UrlRegexPattern))
            {
                return new Regex(UrlRegexPattern).IsMatch(url);
            }
            return false;
        }
    }
}
