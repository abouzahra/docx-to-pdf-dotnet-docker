using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/convert", async (HttpRequest request) =>
{
    if (!request.HasFormContentType || request.Form.Files.Count == 0)
    {
        return Results.BadRequest(new { success = false, message = "Nessun file caricato." });
    }

    var file = request.Form.Files[0];

    if (file.ContentType != "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
    {
        return Results.BadRequest(new { success = false, message = "Devi caricare un file DOCX." });
    }

    // Crea cartella temporanea se non esiste
    string tempFolder = Path.Combine(Path.GetTempPath(), "conversions");
    Directory.CreateDirectory(tempFolder);

    string docxPath = Path.Combine(tempFolder, $"{Guid.NewGuid()}.docx");
    string pdfPath = Path.ChangeExtension(docxPath, ".pdf");

    // Salva il file DOCX
    using (var stream = new FileStream(docxPath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    try
    {
        // Esegui la conversione con LibreOffice
        bool success = ConvertToPDF(docxPath, pdfPath);
        if (!success)
        {
            return Results.Json(new { success = false, message = "Errore durante la conversione." }, statusCode: 500);
        }

        // Leggi il file PDF e restituiscilo
        var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
        return Results.File(pdfBytes, "application/pdf", "converted.pdf");
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Errore interno: {ex.Message}" }, statusCode: 500);
    }
    finally
    {
        // Pulisce i file temporanei
        if (File.Exists(docxPath)) File.Delete(docxPath);
        if (File.Exists(pdfPath)) File.Delete(pdfPath);
    }
});

// Funzione per convertire DOCX -> PDF usando LibreOffice
static bool ConvertToPDF(string inputPath, string outputPath)
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

        return process.ExitCode == 0 && File.Exists(outputPath);
    }
    catch
    {
        return false;
    }
}

app.Run();
