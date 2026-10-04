using System.IO;
using System.Text;
using System.Threading.Tasks;
using Chaptarr.Http.Authentication;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Chaptarr.Core.Test.Http
{
    [TestFixture]
    public class SeerrApiKeyAccessPolicyFixture
    {
        [TestCase("GET", "/api/v1/system/capabilities", true)]
        [TestCase("GET", "/api/v1/book/paged", true)]
        [TestCase("GET", "/api/v1/book/21", true)]
        [TestCase("GET", "/api/v1/command/21", true)]
        [TestCase("GET", "/api/v1/MediaCover/21/cover.jpg", true)]
        [TestCase("GET", "/api/v1/MediaCover/author/21/poster.webp", true)]
        [TestCase("GET", "/MediaCover/21/cover.jpg", true)]
        [TestCase("GET", "/MediaCover/author/21/poster.jpg", true)]
        [TestCase("GET", "/readarr/hc/ebook/api/v1/book/paged", true)]
        [TestCase("GET", "/readarr/gr/audiobook/api/v1/book/21", true)]
        [TestCase("POST", "/api/v1/book", true)]
        [TestCase("POST", "/readarr/hc/audiobook/api/v1/book", true)]
        [TestCase("PUT", "/api/v1/book/21", true)]
        [TestCase("PUT", "/readarr/gr/ebook/api/v1/book/21", true)]
        [TestCase("DELETE", "/api/v1/pendingauthorimport/21", true)]
        [TestCase("DELETE", "/readarr/hc/audiobook/api/v1/pendingauthorimport/21", true)]
        [TestCase("GET", "/api/v1/config/host", false)]
        [TestCase("GET", "/readarr/hc/ebook/api/v1/config/host", false)]
        [TestCase("POST", "/api/v1/command", false)]
        [TestCase("PUT", "/api/v1/author/21", false)]
        [TestCase("DELETE", "/api/v1/book/21", false)]
        [TestCase("GET", "/api/v1/book/21/editions", false)]
        [TestCase("GET", "/api/v1/bookish", false)]
        [TestCase("GET", "/api/v1/command/0", false)]
        [TestCase("POST", "/api/v1/author", false)]
        [TestCase("GET", "/readarr/other/ebook/api/v1/book", false)]
        [TestCase("GET", "/api/v1/MediaCover/../config.xml", false)]
        public async Task should_allow_only_book_service_routes(string method, string path, bool expected)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;

            var allowed = await SeerrApiKeyAccessPolicy.IsAllowedAsync(context.Request);

            Assert.That(allowed, Is.EqualTo(expected));
        }

        [Test]
        public async Task should_allow_only_a_single_book_search_command()
        {
            Assert.That(await IsAllowedCommandAsync("{\"name\":\"BookSearch\",\"bookIds\":[42]}"), Is.True);
            Assert.That(await IsAllowedCommandAsync("{\"name\":\"ManualImport\",\"bookIds\":[42]}"), Is.False);
            Assert.That(await IsAllowedCommandAsync("{\"name\":\"BookSearch\",\"bookIds\":[42,43]}"), Is.False);
            Assert.That(await IsAllowedCommandAsync("{\"name\":\"BookSearch\",\"bookIds\":[0]}"), Is.False);
            Assert.That(await IsAllowedCommandAsync("{\"name\":\"BookSearch\",\"Name\":\"RefreshBook\",\"bookIds\":[42]}"), Is.False);
            Assert.That(await IsAllowedCommandAsync("{\"name\":\"BookSearch\",\"bookIds\":[42],\"BookIds\":[43]}"), Is.False);
        }

        private static async Task<bool> IsAllowedCommandAsync(string json)
        {
            var context = new DefaultHttpContext();
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Request.Method = "POST";
            context.Request.Path = "/api/v1/command";
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = bytes.Length;
            context.Request.Body = new MemoryStream(bytes);

            return await SeerrApiKeyAccessPolicy.IsAllowedAsync(context.Request);
        }
    }
}
