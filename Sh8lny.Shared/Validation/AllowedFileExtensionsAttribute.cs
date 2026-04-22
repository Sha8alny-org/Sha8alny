using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.Validation;

public sealed class AllowedFileExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;

    public AllowedFileExtensionsAttribute(params string[] extensions)
    {
        _extensions = extensions ?? Array.Empty<string>();
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not string filePath || string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        return _extensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase));
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must end with one of the following extensions: {string.Join(", ", _extensions)}.";
    }
}
