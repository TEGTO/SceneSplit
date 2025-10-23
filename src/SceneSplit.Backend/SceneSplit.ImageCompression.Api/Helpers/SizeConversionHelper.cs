namespace SceneSplit.ImageCompression.Api.Helpers;

public static class SizeConversionHelper
{
    public static int ToMB(int bytes) => bytes / (1024 * 1024);
    public static int ToBytes(int mb) => mb * 1024 * 1024;
}