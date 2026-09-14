namespace Bricker.Api.Storage;

public sealed class UploadStorage
{
    private const string PublicPrefix = "/uploads/";

    public UploadStorage(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public async Task<string> SaveListingImageAsync(IFormFile image, string extension, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(RootPath, "listings");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = File.Create(Path.Combine(folder, fileName));
        await image.CopyToAsync(stream, cancellationToken);
        return $"{PublicPrefix}listings/{fileName}";
    }

    public void Delete(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith(PublicPrefix, StringComparison.OrdinalIgnoreCase)) return;

        var relativePath = imageUrl[PublicPrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(RootPath, relativePath));
        var rootPrefix = RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)) return;

        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public void CopyDemoAssets(string contentRootPath)
    {
        var source = Path.Combine(contentRootPath, "DemoAssets", "Listings");
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"Os arquivos de demonstração não foram encontrados em '{source}'.");

        var destination = Path.Combine(RootPath, "demo");
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*.webp"))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
    }
}
