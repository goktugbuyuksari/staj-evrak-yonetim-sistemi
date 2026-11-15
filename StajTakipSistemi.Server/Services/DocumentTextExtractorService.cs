using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StajTakipSistemi.Server.Services
{
    public interface IDocumentTextExtractor
    {
        string ExtractText(Stream fileStream, string fileExtension);
    }

    /// <summary>
    /// Hoca Soru-Cevap 1. Madde:
    /// Belgenin (.docx / PDF / TXT) taranmasıyla elde edilen metni okur ve
    /// kullanıcının formda girdiği bilgiler ile karşılaştırılmasını sağlar.
    /// </summary>
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

                // 1. Düz Metin Dosyaları (.txt, .csv, vb.)
                if (ext == ".txt" || ext == ".csv")
                {
                    using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
                    return reader.ReadToEnd();
                }

                // 2. Word Belgesi (.docx): Zip formatında word/document.xml içerir
                if (ext == ".docx")
                {
                    using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
                    var entry = archive.GetEntry("word/document.xml");
                    if (entry != null)
                    {
                        using var entryStream = entry.Open();
                        var doc = XDocument.Load(entryStream);
                        var sb = new StringBuilder();
                        foreach (var node in doc.Descendants())
                        {
                            if (node.Name.LocalName == "t") // Word XML metin düğümü
                            {
                                sb.Append(node.Value).Append(" ");
                            }
                        }
                        return sb.ToString();
                    }
                }

                // 3. PDF Dosyası (.pdf): PDF akışındaki metinleri basitçe ayrıştır
                if (ext == ".pdf")
                {
                    using var ms = new MemoryStream();
                    fileStream.CopyTo(ms);
                    byte[] bytes = ms.ToArray();
                    string raw = Encoding.UTF8.GetString(bytes);

                    // PDF text objeleri (BT ... ET blokları veya parantez içindeki metinler)
                    var matches = Regex.Matches(raw, @"\(([^)]+)\)\s*Tj", RegexOptions.IgnoreCase);
                    if (matches.Count > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (Match m in matches)
                        {
                            sb.Append(m.Groups[1].Value).Append(" ");
                        }
                        return sb.ToString();
                    }

                    // Basit metin eşleme fallback
                    return raw;
                }
            }
            catch (Exception)
            {
                // Okuma hatası durumunda boş döner
            }

            return string.Empty;
        }
    }
}
