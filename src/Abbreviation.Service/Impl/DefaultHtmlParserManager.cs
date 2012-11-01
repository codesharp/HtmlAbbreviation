//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System.Collections.Generic;
using System.Linq;
using CodeSharp.Core;

namespace Abbreviation.Service
{
    [Component(LifeStyle = LifeStyle.Singleton)]
    public class DefaultHtmlParserManager : IHtmlParserManager
    {
        private DefaultHtmlParser _defaultParser;
        private IDictionary<HtmlParserKey, IHtmlParser> _parserDictionary;

        public DefaultHtmlParserManager()
        {
            _defaultParser = new DefaultHtmlParser();
            _parserDictionary = new Dictionary<HtmlParserKey, IHtmlParser>();
        }

        public void RegisterHtmlParser(HtmlParserKey key, IHtmlParser parser)
        {
            _parserDictionary.Add(key, parser);
        }
        public IHtmlParser GetParser(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                var key = _parserDictionary.Keys.SingleOrDefault(x => x.IsUrlMatch(url));
                if (key != null)
                {
                    return _parserDictionary[key];
                }
            }

            return _defaultParser;
        }
    }
}
