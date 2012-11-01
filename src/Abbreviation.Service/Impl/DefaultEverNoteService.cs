//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System;
using System.Collections.Generic;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
using Evernote.EDAM.UserStore;
using Thrift.Protocol;
using Thrift.Transport;

namespace Abbreviation.Service
{
    public class DefaultEverNoteService : IEverNoteService
    {
        private string _userStoreUrl;

        public DefaultEverNoteService(string userStoreUrl)
        {
            _userStoreUrl = userStoreUrl;
        }

        public IEnumerable<EverNote> GetDefaultNoteBookNotes(string authToken)
        {
            var noteStoreUrl = GetNoteStoreUrl(authToken);
            var transport = new THttpClient(new Uri(noteStoreUrl));
            var protocol = new TBinaryProtocol(transport);
            var noteStore = new NoteStore.Client(protocol);
            var notes = new List<EverNote>();

            var notebooks = noteStore.listNotebooks(authToken);

            foreach (Notebook notebook in notebooks)
            {
                if (notebook.DefaultNotebook)
                {
                    var findResult = noteStore.findNotes(authToken, new NoteFilter { NotebookGuid = notebook.Guid }, 0, int.MaxValue);
                    foreach (var note in findResult.Notes)
                    {
                        notes.Add(new EverNote(note.Guid, note.Title));
                    }
                    break;
                }
            }

            return notes;
        }
        public string GetNoteContent(string authToken, string noteId)
        {
            var noteStoreUrl = GetNoteStoreUrl(authToken);
            var transport = new THttpClient(new Uri(noteStoreUrl));
            var protocol = new TBinaryProtocol(transport);
            var noteStore = new NoteStore.Client(protocol);

            var note = noteStore.getNote(authToken, noteId, false, false, false, false);
            var body = noteStore.getNoteSearchText(authToken, noteId, false, false);

            if (!string.IsNullOrEmpty(body))
            {
                var index = body.LastIndexOf(note.Title);
                if (index > 0)
                {
                    body = body.Substring(0, index);
                }
                body = body.Trim();
            }

            return body;
        }

        private string GetNoteStoreUrl(string authToken)
        {
            var transport = new THttpClient(new Uri(_userStoreUrl));
            var protocol = new TBinaryProtocol(transport);
            var userStore = new UserStore.Client(protocol);

            return userStore.getNoteStoreUrl(authToken);
        }
    }
}
