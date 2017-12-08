using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using mjaang_blog.Models;
using Microsoft.AspNet.Identity;
using PagedList;
using PagedList.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace mjaang_blog.Controllers
{
    [RequireHttps]
    public class BlogPostsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        //Image Uploader Helper
        public static class ImageUploadValidator
        {
            public static bool IsWebFriendlyImage(HttpPostedFileBase file)
            {
                // check for actual object
                if (file == null)
                    return false;
                // check size - file must be less than 2 MB and greater than 1 KB
                if (file.ContentLength > 2 * 1024 * 1024 || file.ContentLength < 1024)
                    return false;
                
                try
                {
                    using (var img = Image.FromStream(file.InputStream))
                    {
                        return ImageFormat.Jpeg.Equals(img.RawFormat) ||
                               ImageFormat.Png.Equals(img.RawFormat) ||
                               ImageFormat.Gif.Equals(img.RawFormat);
                    }
                }
                catch
                {
                    return false;

                }
            }
        }
        // GET: BlogPosts
        public ActionResult Index(int? page)
        {
            int pageSize = 3;  // the number of posts shown per page
            int pageNumber = (page ?? 1);  // if the page number is null show page 1 and if there's page number go to that page
            ViewBag.Message = 1;
            return View(db.Posts.OrderByDescending(p => p.Created).ToPagedList(pageNumber, pageSize));
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Search(int? page, string query)
        {
            int pageSize = 3;  // the number of posts shown per page
            int pageNumber = (page ?? 1);  // if the page number is null show page 1 and if there's page number go to that page
            ViewBag.Query = query;
            var qposts = db.Posts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                qposts = qposts.Where(p => p.Title.Contains(query) || p.Body.Contains(query) || p.Comments.Any(c => c.Body.Contains(query) || c.Author.DisplayName.Contains(query)));
            }
            var posts = qposts.OrderByDescending(p => p.Created).ToPagedList(pageNumber, pageSize);
            return View("Index", posts);
        }

        public ActionResult SearchAuto(string term)
        {
            var qposts = db.Posts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                qposts = qposts.Where(p => p.Title.Contains(term) || p.Body.Contains(term) || p.Comments.Any(c => c.Body.Contains(term) || c.Author.DisplayName.Contains(term)));
            }
            var posts = qposts.OrderByDescending(p => p.Created).Select(p => p.Title);
            return Json(posts, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Admin()
        {
            return View(db.Posts.ToList());
        }

        // GET: BlogPosts/Details/5
        public ActionResult Details(string slug)
        {
            ViewBag.ReturnUrl = Request.Url.AbsolutePath;
            if (String.IsNullOrWhiteSpace(slug))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.FirstOrDefault(p => p.Slug == slug);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // GET: BlogPosts/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: BlogPosts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create([Bind(Include = "Id,Title,Body,MediaURL,Published")] BlogPost blogPost, HttpPostedFileBase Image)
        {
            if (ModelState.IsValid)
            {
                if (ImageUploadValidator.IsWebFriendlyImage(Image)) //restricting the valid file formats to images only
                {
                    var fileName = Path.GetFileName(Image.FileName);
         
                    Image.SaveAs(Path.Combine(Server.MapPath("~/img/blog/"), fileName));
                    blogPost.MediaURL = "~/img/blog/" + fileName;
                }

                var Slug = StringUtilities.URLFriendly(blogPost.Title);
                if (String.IsNullOrWhiteSpace(Slug))
                { ModelState.AddModelError("Title", "Invalid title.");
                    return View(blogPost);
                }
                if (db.Posts.Any(p => p.Slug == Slug))
                {
                    ModelState.AddModelError("Title", "The title must be unique.");
                    return View(blogPost);
                }
                blogPost.Slug = Slug;
                blogPost.Created = System.DateTimeOffset.Now;
                db.Posts.Add(blogPost);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(blogPost);
        }

        // GET: BlogPosts/Edit/5
        [Authorize (Roles ="Admin, Moderator")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            blogPost.Updated = System.DateTimeOffset.Now;
            return View(blogPost);
        }

        // POST: BlogPosts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Moderator")]
        public ActionResult Edit([Bind(Include = "Id,Created,Updated,Title,Slug,Body,MediaURL,Published")] BlogPost blogPost, HttpPostedFileBase Image)
        {
            if (ModelState.IsValid)
            {
                blogPost.Updated = System.DateTimeOffset.Now;
                db.Posts.Attach(blogPost);
                if (ImageUploadValidator.IsWebFriendlyImage(Image)) //restricting the valid file formats to images only
                {
                    var fileName = Path.GetFileName(Image.FileName);
                    Image.SaveAs(Path.Combine(Server.MapPath("~/img/blog/"), fileName));
                    blogPost.MediaURL = "~/img/blog/" + fileName;
                   

                }
                db.Entry(blogPost).Property("MediaURL").IsModified = true;
                db.Entry(blogPost).Property("Title").IsModified = true;
                db.Entry(blogPost).Property("Body").IsModified = true;
                db.Entry(blogPost).Property("Updated").IsModified = true;

                var Slug = StringUtilities.URLFriendly(blogPost.Title);

                if (String.IsNullOrWhiteSpace(Slug))
                {
                    ModelState.AddModelError("Title", "Invalid title.");
                    return View(blogPost);
                }

                var SlugAlreadyExists = db.Posts.Where(p => p.Id == blogPost.Id && p.Slug == Slug).Select(p => p.Slug);

                if (!SlugAlreadyExists.Any())
                {
                    if (db.Posts.Any(p => p.Slug == Slug))
                    {
                        ModelState.AddModelError("Title", "The title must be unique.");

                        return View(blogPost);
                    }
                }
                blogPost.Slug = Slug;
                db.Entry(blogPost).Property("Slug").IsModified = true;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(blogPost);
        }



        // GET: BlogPosts/Delete/5
        [Authorize (Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // POST: BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize (Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            BlogPost blogPost = db.Posts.Find(id);
            db.Posts.Remove(blogPost);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // GET: BlogPosts/Comments/Create/5
        [Authorize]
        public ActionResult Comment()
        {
            return View();
        }

        // POST: BlogPosts/Comments/Create/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CreateComment([Bind(Include = "PostId, Body")]Comment comment)
        {
            if (ModelState.IsValid)
            {
                comment.AuthorId = User.Identity.GetUserId();
                comment.Created = System.DateTimeOffset.Now;
                db.Comments.Add(comment);
                db.SaveChanges();
            }
            var blog = db.Posts.Find(comment.PostId);
            return RedirectToAction("Details", "BlogPosts", new { slug = blog.Slug });
        }


       // GET: BlogPosts/Comments/Edit/5
       [Authorize]
        public ActionResult EditComment(int? Id)
        {
            if (Id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Comment com = db.Comments.Find(Id);
            if (com == null)
            {
                return HttpNotFound();
            }
            return View(com);
        }
        
        // POST: BlogPosts/Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult EditComment([Bind(Include = "Id, PostId, Body")] Comment commentPost)
        {
            if (ModelState.IsValid)
            {
                commentPost.AuthorId = User.Identity.GetUserId();
                commentPost.Updated = System.DateTimeOffset.Now;
                db.Comments.Attach(commentPost);
                db.Entry(commentPost).Property("Body").IsModified = true;
                db.Entry(commentPost).Property("Updated").IsModified = true;
                db.SaveChanges();
            }
            var blog = db.Posts.Find(commentPost.PostId);
            return RedirectToAction("Details", "BlogPosts", new { slug = blog.Slug });
        }

     // GET: BlogPosts/Comments/Delete/5
     [Authorize (Roles = "Admin")]
        public ActionResult DeleteComment(int? Id)
        {
            if (Id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Comment comdel = db.Comments.Find(Id);
            if (comdel == null)
            {
                return HttpNotFound();
            }
            return View(comdel);
        }

        // POST: BlogPosts/Comments/Delete/5
        [HttpPost, ActionName("DeleteComment")]
        [ValidateAntiForgeryToken]
        [Authorize (Roles ="Admin")]
        public ActionResult DeleteConf(int Id)
        {
            Comment delcom = db.Comments.Find(Id);
            db.Comments.Remove(delcom);
            db.SaveChanges();
            var blog = db.Posts.Find(delcom.PostId);
            return RedirectToAction("Details", "BlogPosts", new { slug = blog.Slug });
        }
    }
}
