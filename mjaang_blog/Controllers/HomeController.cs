using mjaang_blog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;

namespace mjaang_blog.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult Index()
        {
            return View(db.Posts.ToList());
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }
        // get Method //
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        // post Method //
        [HttpPost] //should be designated for post Method //
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Contact([Bind(Include = "Id,Name,Email,Message,Phone,MessageSent")]Contact2 contact)
        {
            contact.MessageSent = DateTime.Now;

            var svc = new EmailService();
            var msg = new IdentityMessage();
            msg.Subject = "Contact From Portfolio";
            msg.Body = contact.Message;
            await svc.SendAsync(msg);
            ViewBag.msg = " *** Your Message has been sent *** ";
            return View(contact);
        }

        [HttpGet]
        public JsonResult SearchNumber(string SearchKeyword)
        {
            List<string> rtnStr = new List<string>();
            foreach(var item in db.Posts.Where(p=>p.Title.Contains(SearchKeyword)))
            {
                rtnStr.Add(item.Title);
            }

            return Json(rtnStr, JsonRequestBehavior.AllowGet);
        }
    }
}
