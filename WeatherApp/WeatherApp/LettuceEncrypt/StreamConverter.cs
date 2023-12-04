namespace WeatherApp.LettuceEncrypt
{
    public static class StreamConverter
    {
        public static byte[] ToByteArray(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
