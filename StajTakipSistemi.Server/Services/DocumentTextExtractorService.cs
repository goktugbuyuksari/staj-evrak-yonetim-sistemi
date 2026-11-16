using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StajTakipSistemi.Server.Services
{
    // SERVİS ARAYÜZÜ: Belgeden metin ayıklama sözleşmesi
    public interface IDocumentTextExtractor
    {
        string ExtractText(Stream fileStream, string fileExtension);
    }

    // SERVİS & ÖN-OCR: Word (.docx), PDF ve metin dosyalarından metin içeriğini çıkaran servis
    public class DocumentTextExtractor : IDocumentTextExtractor
    {
        public string ExtractText(Stream fileStream, string fileExtension)
        {
            if (fileStream == null || fileStream.Length == 0)
                return string.Empty;

            string ext = fileExtension.ToLowerInvariant();
            try
            {
                fileStream.Position = 0;

                // 1. Düz Metin Dosyaları (.txt, .csv)
                if (ext == ".txt" || ext == ".csv")
                {
                    using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
                    return reader.ReadToEnd();
                }

                // 2. Word Belgesi (.docx): Zip formatında document.xml'den metinleri çeker
                if (ext == ".docx")
                {
                    return ExtractFromDocx(fileStream);
                }

                // 3. PDF Belgesi (.pdf): Akış içerisindeki metin bloklarını (BT...ET) ayıklar
                if (ext == ".pdf")
                {
                    return ExtractFromPdf(fileStream);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Metin çıkarma hatası ({ext}): {ex.Message}");
                return string.Empty;
            }
        }

        // WORD PARSER: .docx içindeki XML etiketlerini ayıklar
        private static string ExtractFromDocx(Stream stream)
        {
            var sb = new StringBuilder();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var entry = archive.GetEntry("word/document.xml");
            if (entry != null)
            {
                using var entryStream = entry.Open();
                var xdoc = XDocument.Load(entryStream);
                foreach (var element in xdoc.Descendants())
                {
                    if (element.Name.LocalName == "t")
                    {
                        sb.Append(element.Value).Append(" ");
                    }
                    else if (element.Name.LocalName == "p")
                    {
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString().Trim();
        }

        // PDF PARSER: PDF stream içerisindeki metin karakterlerini okur
        private static string ExtractFromPdf(Stream stream)
        {
            var sb = new StringBuilder();
            using var reader = new StreamReader(stream, Encoding.Latin1, leaveOpen: true);
            string content = reader.ReadToEnd();

            var matches = Regex.Matches(content, @"\((.*?)\)\s*Tj", RegexOptions.Singleline);
            foreach (Match m in matches)
            {
                sb.Append(m.Groups[1].Value).Append(" ");
            }

            if (sb.Length == 0)
            {
                var bracketMatches = Regex.Matches(content, @"\[(.*?)\]\s*TJ", RegexOptions.Singleline);
                foreach (Match m in bracketMatches)
                {
                    var textMatches = Regex.Matches(m.Groups[1].Value, @"\((.*?)\)");
                    foreach (Match tm in textMatches)
                    {
                        sb.Append(tm.Groups[1].Value).Append(" ");
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}
