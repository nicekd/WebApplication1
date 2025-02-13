using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;

public class AllowedFileExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;

    public AllowedFileExtensionsAttribute(string[] extensions)
    {
        _extensions = extensions;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var file = value as IFormFile;
        if (file != null)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_extensions.Contains(extension))
            {
                return new ValidationResult($"Only {string.Join(", ", _extensions)} files are allowed.");
            }
        }
        return ValidationResult.Success;
    }
}
