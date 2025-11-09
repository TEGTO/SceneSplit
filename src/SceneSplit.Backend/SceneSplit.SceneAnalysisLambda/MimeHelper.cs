namespace SceneSplit.SceneAnalysisLambda;

public static class MimeHelper
{
    public static string NormalizeMime(string? mime, string key)
    {
        if (string.IsNullOrWhiteSpace(mime))
        {
            mime = GuessFromExtension(key);
        }

        mime = mime.Trim().ToLowerInvariant();

        return mime switch
        {
            "image/jpg" => "image/jpeg",
            "image/pjpeg" => "image/jpeg",
            "image/x-png" => "image/png",
            "image/png" => "image/png",
            "image/jpeg" => "image/jpeg",
            "image/webp" => "image/webp",
            "image/gif" => "image/gif",
            _ => GuessFromExtension(key)
        };
    }

    private static string GuessFromExtension(string key)
    {
        var ext = Path.GetExtension(key).ToLowerInvariant();
        return ext switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }
}
