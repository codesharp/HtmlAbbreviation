//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System;
using System.Linq;
using Raven.Client;

namespace Abbreviation.Service
{
    public class RavendbSnippetTextRepository : ISnippetTextRepository
    {
        private IDocumentStore _documentStore;

        public RavendbSnippetTextRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public SnippetText FindBy(SnippetTextType type, string key)
        {
            using (var session = _documentStore.OpenSession())
            {
                return session.Query<SnippetText>().Where(x => x.Type == type && x.Key == key).FirstOrDefault();
            }
        }
        public void Add(SnippetTextType type, string key, string snippetText)
        {
            using (var session = _documentStore.OpenSession())
            {
                var existingSnippet = session.Query<SnippetText>().Where(x => x.Type == type && x.Key == key).FirstOrDefault();
                if (existingSnippet == null)
                {
                    var snippet = new SnippetText
                    {
                        UniqueId = Guid.NewGuid().ToString(),
                        Type = type,
                        Key = key,
                        Text = snippetText
                    };
                    session.Store(snippet);
                }
                else
                {
                    existingSnippet.Text = snippetText;
                }
                session.SaveChanges();
            }
        }
        public void Remove(string id)
        {
            using (var session = _documentStore.OpenSession())
            {
                var snippets = session.Query<SnippetText>().Where(x => x.UniqueId == id);
                if (snippets != null && snippets.Count() > 0)
                {
                    foreach (var snippet in snippets)
                    {
                        session.Delete(snippet);
                    }
                    session.SaveChanges();
                }
            }
        }
    }
}
