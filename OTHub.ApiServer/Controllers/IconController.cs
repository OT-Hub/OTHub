using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Jdenticon;
using Jdenticon.Rendering;
using Microsoft.AspNetCore.Mvc;
using OTHub.APIServer.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class IconController : Controller
    {
        private static readonly ConcurrentDictionary<String, int> letterToHueDictionary;

        static IconController()
        {
            letterToHueDictionary = new ConcurrentDictionary<string, int>();

            letterToHueDictionary.TryAdd("1", 20);
            letterToHueDictionary.TryAdd("0", 40);
            letterToHueDictionary.TryAdd("6", 60);
            letterToHueDictionary.TryAdd("4", 80);
            letterToHueDictionary.TryAdd("f", 100);
            letterToHueDictionary.TryAdd("b", 120);
            letterToHueDictionary.TryAdd("e", 140);
            letterToHueDictionary.TryAdd("c", 160);
            letterToHueDictionary.TryAdd("3", 180);
            letterToHueDictionary.TryAdd("8", 200);
            letterToHueDictionary.TryAdd("5", 220);
            letterToHueDictionary.TryAdd("7", 240);
            letterToHueDictionary.TryAdd("a", 260);
            letterToHueDictionary.TryAdd("2", 280);
            letterToHueDictionary.TryAdd("d", 300);
            letterToHueDictionary.TryAdd("9", 320);
        }

        [HttpGet("node/{identity}/{theme}/{size}")]
        [SwaggerOperation(
            Summary = "Gets the URL of the unique icon for an identity",
            Description = @"The image will always be returned in a .png format.

The following theme options are supported:
- light
- dark

The following sizes in pixels are supported:
- 16
- 24
- 32
- 48
- 64"
        )]
        [SwaggerResponse(200, type: typeof(ContractAddress))]
        [SwaggerResponse(500, "Internal server error")]
        public IActionResult Get([FromRoute, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity, [FromRoute, SwaggerParameter("The theme (mainly used for the website). Options are: light, dark", Required = true)]string theme, [FromRoute, SwaggerParameter("The size of the image in pixels. Options are: 16, 24, 32, 48 and 64", Required = true)] int size)
        {
            if (theme != "dark" && theme != "light")
            {
                theme = "light";
            }

            if (identity.Length != 42 || !identity.StartsWith("0x") || !identity.All(Char.IsLetterOrDigit))
                return BadRequest();

            if (size > 64)
            {
                size = 64;
            }
            else if (size != 16 && size != 48 && size != 24 && size != 32 && size != 64)
            {
                size = 16;
            }

            Response.Headers["Cache-Control"] = "public,max-age=604800";

            string path;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = $@"icons\node\{theme}\{size}\{identity}.png";
            }
            else
            {
                path = $@"icons/node/{theme}/{size}/{identity}.png";
            }

            if (System.IO.File.Exists(path))
            {
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                return File(fs, "image/png");
            }

            string firstChar = identity.Substring(2, 1);

            if (!letterToHueDictionary.TryGetValue(firstChar, out var val))
            {
                val = 216;
            }

            var style = new IdenticonStyle
            {
                Hues = new HueCollection { { val, HueUnit.Degrees } },
                Padding = 0.1F,
                BackColor = theme == "light" ? Color.FromRgb(241, 242, 247) : Color.FromRgb(89, 99, 114),
                ColorSaturation = 1.0f,
                GrayscaleSaturation = 0.2f,
                ColorLightness = Range.Create(0.1f, 0.9f),
                GrayscaleLightness = Range.Create(0.1f, 0.5f),
            };

            var icon = Identicon.FromValue("identity:" + identity, size);
            icon.Style = style;

            var folder = Directory.GetParent(path);
            if (!folder.Exists)
            {
                folder.Create();
            }

            using (var ms = new MemoryStream())
            using (var fs = System.IO.File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                icon.SaveAsPng(fs);
                icon.SaveAsPng(ms);
                return File(ms.ToArray(), "image/png");
            }
        }
    }
}