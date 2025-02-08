# Usa l'immagine ufficiale .NET 8 come base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Installa LibreOffice
RUN apt-get update && apt-get install -y libreoffice && rm -rf /var/lib/apt/lists/*

# Copia l'applicazione pubblicata
COPY bin/Release/net8.0/publish /app

# Imposta la porta
ENV ASPNETCORE_URLS=http://+:8080

# Avvia l'applicazione
CMD ["dotnet", "docx_to_pdf_dotnet_docker.dll"]
