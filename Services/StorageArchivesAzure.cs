using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace MoviesAPI.Services
{
    public class StorageArchivesAzure : IStorageFiles
    {
        private readonly string _connectionString;
        private readonly ILogger<StorageArchivesAzure> _logger;

        public StorageArchivesAzure(
            IConfiguration configuration,
            ILogger<StorageArchivesAzure> logger)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection")
                ?? throw new ArgumentNullException("AzureStorageConnection no está configurado");

            _logger = logger;
        }

        public async Task<string> Store(string container, IFormFile archive)
        {
            _logger.LogInformation("📦 Iniciando subida de archivo al contenedor {Container}", container);

            try
            {
                _logger.LogInformation("📄 Nombre original del archivo: {FileName}", archive.FileName);
                _logger.LogInformation("📄 Content-Type: {ContentType}", archive.ContentType);

                var client = new BlobContainerClient(_connectionString, container);

                _logger.LogInformation("🔧 Creando contenedor si no existe...");
                await client.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var extension = Path.GetExtension(archive.FileName);
                var archiveName = $"{Guid.NewGuid()}{extension}";

                _logger.LogInformation("🆕 Nombre generado del archivo: {ArchiveName}", archiveName);

                var blob = client.GetBlobClient(archiveName);

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = archive.ContentType
                };

                _logger.LogInformation("⬆️ Subiendo archivo a Azure Blob Storage...");
                await blob.UploadAsync(archive.OpenReadStream(), blobHttpHeaders);

                _logger.LogInformation("✅ Archivo subido correctamente. URL: {Url}", blob.Uri);

                return blob.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al subir archivo a Azure Blob Storage");
                throw; // IMPORTANTE: no ocultar el error
            }
        }

        public async Task Delete(string? route, string container)
        {
            _logger.LogInformation("🗑️ Iniciando eliminación de archivo. Ruta: {Route}", route);

            if (string.IsNullOrEmpty(route))
            {
                _logger.LogWarning("⚠️ Ruta vacía, no se elimina nada");
                return;
            }

            try
            {
                var client = new BlobContainerClient(_connectionString, container);
                await client.CreateIfNotExistsAsync();

                var archiveName = Path.GetFileName(route);

                _logger.LogInformation("🗂️ Archivo a eliminar: {ArchiveName}", archiveName);

                var blob = client.GetBlobClient(archiveName);
                await blob.DeleteIfExistsAsync();

                _logger.LogInformation("✅ Archivo eliminado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al eliminar archivo de Azure Blob Storage");
                throw;
            }
        }
    }
}
