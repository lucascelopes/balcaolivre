using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const int BusinessLogoMaximumDimension = 1200;
    private const long BusinessLogoMaximumSourceBytes = 25L * 1024 * 1024;
    private const int PublicBookingLogoMaximumDimension = 128;
    private const int PublicBookingLogoMaximumBytes = 96 * 1024;
    private static readonly HashSet<string> BusinessLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp"
    };

    private string? SelectBusinessLogoSourceFile(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var dialog = new OpenFileDialog
        {
            Title = "Escolher logo do estabelecimento",
            Filter = "Imagens compatíveis (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    private string PersistBusinessLogo(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Selecione uma imagem para usar como logo.", nameof(sourcePath));
        }

        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException("A imagem escolhida não foi encontrada.", fullSourcePath);
        }

        var extension = Path.GetExtension(fullSourcePath);
        if (!BusinessLogoExtensions.Contains(extension))
        {
            throw new InvalidDataException("Escolha uma imagem PNG, JPG, JPEG ou BMP.");
        }

        var sourceInfo = new FileInfo(fullSourcePath);
        if (sourceInfo.Length <= 0)
        {
            throw new InvalidDataException("A imagem escolhida está vazia.");
        }

        if (sourceInfo.Length > BusinessLogoMaximumSourceBytes)
        {
            throw new InvalidDataException("A imagem escolhida deve ter no máximo 25 MB.");
        }

        BitmapFrame sourceFrame;
        try
        {
            using var input = new FileStream(
                fullSourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var decoder = BitmapDecoder.Create(
                input,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            sourceFrame = decoder.Frames.FirstOrDefault()
                ?? throw new InvalidDataException("Não foi possível ler a imagem escolhida.");
            sourceFrame.Freeze();
        }
        catch (Exception exception) when (exception is NotSupportedException or FileFormatException)
        {
            throw new InvalidDataException("O arquivo escolhido não contém uma imagem válida.", exception);
        }

        if (sourceFrame.PixelWidth <= 0 || sourceFrame.PixelHeight <= 0)
        {
            throw new InvalidDataException("A imagem escolhida não possui dimensões válidas.");
        }

        BitmapSource normalizedSource = sourceFrame;
        var longestSide = Math.Max(sourceFrame.PixelWidth, sourceFrame.PixelHeight);
        if (longestSide > BusinessLogoMaximumDimension)
        {
            var scale = (double)BusinessLogoMaximumDimension / longestSide;
            normalizedSource = new TransformedBitmap(
                sourceFrame,
                new ScaleTransform(scale, scale));
            normalizedSource.Freeze();
        }

        var brandingDirectory = BusinessLogoDirectoryPath();
        Directory.CreateDirectory(brandingDirectory);
        var destinationPath = Path.Combine(
            brandingDirectory,
            $"business-logo-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.png");
        var temporaryPath = $"{destinationPath}.tmp";

        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(normalizedSource));
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                encoder.Save(output);
                output.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath);
            return destinationPath;
        }
        catch
        {
            TryDeleteFile(temporaryPath);
            TryDeleteFile(destinationPath);
            throw;
        }
    }

    private static BitmapSource? TryLoadBusinessLogoBitmap(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(logoPath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            using var input = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = input;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or FileFormatException or ArgumentException)
        {
            return null;
        }
    }

    private static string PublicBookingLogoFingerprint(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return "";
        }

        try
        {
            var fullPath = Path.GetFullPath(logoPath);
            if (!File.Exists(fullPath))
            {
                return "";
            }

            var file = new FileInfo(fullPath);
            return $"{fullPath}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return "";
        }
    }

    private static string BuildPublicBookingLogoDataUrl(string? logoPath)
    {
        var bitmap = TryLoadBusinessLogoBitmap(logoPath);
        if (bitmap is null)
        {
            return "";
        }

        try
        {
            BitmapSource thumbnail = bitmap;
            var longestSide = Math.Max(bitmap.PixelWidth, bitmap.PixelHeight);
            if (longestSide > PublicBookingLogoMaximumDimension)
            {
                var scale = (double)PublicBookingLogoMaximumDimension / longestSide;
                thumbnail = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
                thumbnail.Freeze();
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(thumbnail));
            using var output = new MemoryStream();
            encoder.Save(output);
            if (output.Length <= 0 || output.Length > PublicBookingLogoMaximumBytes)
            {
                return "";
            }

            return $"data:image/png;base64,{Convert.ToBase64String(output.ToArray())}";
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            return "";
        }
    }

    private static void ApplyBusinessLogoPreview(
        Image image,
        FrameworkElement placeholder,
        TextBlock? label,
        string? logoPath)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(placeholder);

        var bitmap = TryLoadBusinessLogoBitmap(logoPath);
        image.Source = bitmap;
        image.Visibility = bitmap is null ? Visibility.Collapsed : Visibility.Visible;
        placeholder.Visibility = bitmap is null ? Visibility.Visible : Visibility.Collapsed;
        if (label is not null)
        {
            label.Text = bitmap is null || string.IsNullOrWhiteSpace(logoPath)
                ? "Nenhuma logo selecionada"
                : Path.GetFileName(logoPath);
        }
    }

    private void DeleteManagedBusinessLogoIfUnused(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath) ||
            PathsReferToSameFile(logoPath, _data.Settings.BusinessLogoPath) ||
            !IsManagedBusinessLogoPath(logoPath))
        {
            return;
        }

        TryDeleteFile(logoPath);
    }

    private string BusinessLogoDirectoryPath() => Path.Combine(_store.DataRoot, "branding");

    private bool IsManagedBusinessLogoPath(string logoPath)
    {
        try
        {
            var fullLogoPath = Path.GetFullPath(logoPath);
            var fullBrandingDirectory = Path.GetFullPath(BusinessLogoDirectoryPath())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            return fullLogoPath.StartsWith(fullBrandingDirectory, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Path.GetExtension(fullLogoPath), ".png", StringComparison.OrdinalIgnoreCase) &&
                   Path.GetFileName(fullLogoPath).StartsWith("business-logo-", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool PathsReferToSameFile(string firstPath, string secondPath)
    {
        if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(firstPath),
                Path.GetFullPath(secondPath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A logo antiga não deve impedir que os dados atuais sejam salvos.
        }
    }
}
