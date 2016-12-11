﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Extensions.Options;
using PhotoArchiver.Services;

namespace PhotoArchiver.Controllers
{
    [Route("api/[controller]")]
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> logger;
        private readonly AppSettings appSettings;
        private readonly DateTakenExtractorService dateTakenExtractorService;

        public UploadController(ILogger<UploadController> logger, IOptions<AppSettings> appSettings, DateTakenExtractorService dateTakenExtractorService)
        {
            this.logger = logger;
            this.appSettings = appSettings.Value;
            this.dateTakenExtractorService = dateTakenExtractorService;
        }

        [HttpGet]
        public IActionResult Get(ICollection<IFormFile> files)
        {
            return Ok();
        }

        private IActionResult Error(string message)
        {
            return BadRequest(message);
        }

        [HttpPost]
        [Produces("text/plain")]
        public async Task<IActionResult> Post(ICollection<IFormFile> files)
        {
            IFormFile file = files.FirstOrDefault();

            if (file == null)
                return BadRequest();

            string ext = Path.GetExtension(file.FileName).TrimStart('.').ToLower();

            if (appSettings.AllowedExtensions != null)
            {
                if (appSettings.AllowedExtensions.Contains(ext, CaseInsensitiveStringEqualityComparer.Default) == false)
                    return Error("unsupported file type");
            }

            string realFilename = null;
            string workingFilename = Path.Combine(appSettings.TempAbsolutePath, file.FileName);
            logger.LogInformation($"=== workingFilename: '{workingFilename}'");

            if (System.IO.File.Exists(workingFilename))
                logger.LogWarning($"File '{workingFilename}' already exists.");

            FileStream fs = await DownloadToLocalFileStream(workingFilename, file);

            if (fs == null)
                return Error("download failed");

            bool result;
            DateTime dateTaken = DateTime.MinValue;

            using (fs)
            {
                RootDateTakenExtractor extractor = dateTakenExtractorService.GetObject();

                try
                {
                    result = extractor.ExtractDateTaken(fs, out dateTaken);
                }
                finally
                {
                    dateTakenExtractorService.PutObject(extractor);
                }

                logger.LogInformation($"Date taken for file '{file.FileName}': {dateTaken.ToString("yyyy.MM.dd_HH.mm.ss")}");
            }

            fs = null;

            if (result == false)
            {
                TryDeleteFile(workingFilename);
                return Error("cannot determine date taken");
            }

            var targetRelativeFilename = DateTakenToFullRelativePath(ext, dateTaken);
            realFilename = Path.Combine(appSettings.TargetAbsolutePath, targetRelativeFilename);
            logger.LogInformation($"realFilename: '{realFilename}'");

            string realFilePath = Path.GetDirectoryName(realFilename);
            if (Directory.Exists(realFilePath) == false)
            {
                try
                {
                    Directory.CreateDirectory(realFilePath);
                }
                catch (Exception ex)
                {
                    TryDeleteFile(workingFilename);
                    return Error($"exception: {ex.GetType().Name} - {ex.Message} (1)");
                }
            }

            try
            {
                realFilename = MakeUniqueFilename(realFilename);
                System.IO.File.Move(workingFilename, realFilename);
                Utility.ChOwn("root", "photo-readwrite", realFilename);

                logger.LogInformation($"Done with file '{realFilename}' !");
                return Ok(Path.GetFileName(realFilename));
            }
            catch (Exception ex)
            {
                TryDeleteFile(workingFilename);
                return Error($"exception: {ex.GetType().Name} - {ex.Message} (2)");
            }
        }

        private string MakeUniqueFilename(string fullFilename)
        {
            if (System.IO.File.Exists(fullFilename) == false)
                return fullFilename;

            string fullPath = Path.GetDirectoryName(fullFilename);
            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(fullFilename);
            string extension = Path.GetExtension(fullFilename);

            int tryCount = 1;
            string testFilename;

            do
            {
                testFilename = $"{fullPath}{Path.DirectorySeparatorChar}{filenameWithoutExtension}_({tryCount}){extension}";
                tryCount++;
            } while (System.IO.File.Exists(testFilename));

            return testFilename;
        }

        private async Task<FileStream> DownloadToLocalFileStream(string workingFilename, IFormFile file)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(workingFilename, FileMode.Create, FileAccess.ReadWrite);
                await file.CopyToAsync(fs);
                fs.Position = 0;
                return fs;
            }
            catch
            {
                if (fs != null)
                    fs.Dispose();

                TryDeleteFile(workingFilename);

                return null;
            }
        }

        private void TryDeleteFile(string filename)
        {
            try
            {
                System.IO.File.Delete(filename);
            }
            catch
            {
                logger.LogWarning($"Impossible to delete file '{filename}'");
            }
        }

        private string DateTakenToFullRelativePath(string fileExtension, DateTime dateTaken)
        {
            return Path.Combine(
                dateTaken.Year.ToString(),
                dateTaken.Month.ToString().PadLeft(2, '0'),
                dateTaken.ToString("yyyy.MM.dd_HH.mm.ss.") + fileExtension.TrimStart('.')
            );
        }
    }

    public class CaseInsensitiveStringEqualityComparer : IEqualityComparer<string>
    {
        public static readonly CaseInsensitiveStringEqualityComparer Default = new CaseInsensitiveStringEqualityComparer();

        public bool Equals(string x, string y)
        {
            return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj != null ? obj.GetHashCode() : 0;
        }
    }
}
