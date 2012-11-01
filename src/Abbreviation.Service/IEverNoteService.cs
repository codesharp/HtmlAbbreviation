//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System.Collections.Generic;

namespace Abbreviation.Service
{
    /// <summary>
    /// 提供提取指定EventNote账号中的笔记信息的服务
    /// </summary>
    public interface IEverNoteService
    {
        /// <summary>
        /// 返回默认笔记簿中的笔记
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        IEnumerable<EverNote> GetDefaultNoteBookNotes(string authToken);
        /// <summary>
        /// 返回指定笔记的内容
        /// </summary>
        /// <param name="authToken"></param>
        /// <param name="noteId"></param>
        /// <returns></returns>
        string GetNoteContent(string authToken, string noteId);
    }

    /// <summary>
    /// 表示EverNote的一条笔记数据
    /// </summary>
    public class EverNote
    {
        public string Id { get; private set; }
        public string Title { get; private set; }

        public EverNote(string id, string title)
        {
            Id = id;
            Title = title;
        }
    }
}
