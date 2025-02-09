using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DocxToPdf.Controllers;

[ApiController]
[Route("[controller]")]
public class ConvertController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Convert()
    {
        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
        {
            return BadRequest(new { success = false, message = "Nessun file caricato." });
        }

        var file = Request.Form.Files[0];

        if (file.ContentType != "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            return BadRequest(new { success = false, message = "Devi caricare un file DOCX." });
        }

        string tempFolder = Path.Combine(Path.GetTempPath(), "tmp");
        Directory.CreateDirectory(tempFolder);

        string docxPath = Path.Combine(tempFolder, $"{Guid.NewGuid()}.docx");
        string pdfPath = Path.ChangeExtension(docxPath, ".pdf");

        using (var stream = new FileStream(docxPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        try
        {
            bool success = ConvertToPDF(docxPath, pdfPath);
            if (!success)
            {
                return StatusCode(500, new { success = false, message = "Errore durante la conversione." });
            }

            var pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
            return File(pdfBytes, "application/pdf", "converted.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Errore interno: {ex.Message}" });
        }
        finally
        {
            if (System.IO.File.Exists(docxPath)) System.IO.File.Delete(docxPath);
            if (System.IO.File.Exists(pdfPath)) System.IO.File.Delete(pdfPath);
        }
    }

    private bool ConvertToPDF(string inputPath, string outputPath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "libreoffice",
                    Arguments = $"--headless --convert-to pdf --outdir \"{Path.GetDirectoryName(outputPath)}\" \"{inputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0 && System.IO.File.Exists(outputPath);
        }
        catch
        {
            return false;
        }
    }
}