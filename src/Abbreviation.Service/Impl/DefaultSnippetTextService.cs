//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System;

namespace Abbreviation.Service
{
    /// <summary>
    /// 提供提取文本缩略信息，以及缓存文本缩略信息的服务
    /// </summary>
    public class DefaultSnippetTextService : ISnippetTextService
    {
        private IHtmlParserManager _htmlParserManager;
        private IEverNoteService _everNoteService;
        private ISnippetTextRepository _snippetTextRepository;

        public DefaultSnippetTextService(IHtmlParserManager htmpParserManager, IEverNoteService everNoteService, ISnippetTextRepository snippetTextRepository)
        {
            _htmlParserManager = htmpParserManager;
            _everNoteService = everNoteService;
            _snippetTextRepository = snippetTextRepository;
        }

        public string GetHtmlSnippetText(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var snippetText = _snippetTextRepository.FindBy(SnippetTextType.Html, url);
            if (snippetText != null)
            {
                return snippetText.Text;
            }

            var text = _htmlParserManager.GetParser(url).ParseUrl(url);

            if (!string.IsNullOrEmpty(text))
            {
                _snippetTextRepository.Add(SnippetTextType.Html, url, text);
            }

            return text;
        }
        public string GetEverNoteSnippetText(string authToken, string noteId)
        {
            if (string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(noteId))
            {
                return null;
            }

            var snippetText = _snippetTextRepository.FindBy(SnippetTextType.EverNote, noteId);
            if (snippetText != null)
            {
                return snippetText.Text;
            }

            var content = _everNoteService.GetNoteContent(authToken, noteId);

            if (!string.IsNullOrEmpty(content))
            {
                _snippetTextRepository.Add(SnippetTextType.EverNote, noteId, content);
            }

            return content;
        }
        public void ClearSnippetText(string id)
        {
            _snippetTextRepository.Remove(id);
        }
    }
}
