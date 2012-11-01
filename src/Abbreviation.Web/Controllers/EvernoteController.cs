using System.Web.Mvc;
using Abbreviation.Service;

namespace Abbreviation.Web.Controllers
{
    public class EvernoteController : Controller
    {
        public ISnippetTextService _snippetTextService;
        public IEverNoteService _evernoteService;

        public EvernoteController(ISnippetTextService snippetTextService, IEverNoteService evernoteService)
        {
            _snippetTextService = snippetTextService;
            _evernoteService = evernoteService;
        }

        public ActionResult GetDefaultNotes(string token)
        {
            var notes = _evernoteService.GetDefaultNoteBookNotes(token);
            return Json(notes);
        }
        public ActionResult GetNoteContent(string token, string noteId)
        {
            var content = _snippetTextService.GetEverNoteSnippetText(token, noteId);
            return new ContentResult { Content = content };
        }
    }
}
