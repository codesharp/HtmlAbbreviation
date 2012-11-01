using System.Web.Mvc;
using Abbreviation.Service;

namespace Abbreviation.Web.Controllers
{
    public class HtmlController : Controller
    {
        public ISnippetTextService _snippetTextService;

        public HtmlController(ISnippetTextService snippetTextService)
        {
            _snippetTextService = snippetTextService;
        }

        public ActionResult Abbreviate(string url)
        {
            var html = _snippetTextService.GetHtmlSnippetText(url);
            return new ContentResult { Content = html };
        }
    }
}
