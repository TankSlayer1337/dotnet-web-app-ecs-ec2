using Amazon.S3;
using LettuceEncrypt;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace WeatherApp.LettuceEncrypt
{
    public class CertificateSource : ICertificateSource
    {
        private readonly Bucket _bucket;

        public CertificateSource(Bucket bucket)
        {
            _bucket = bucket;
        }

        public async Task<IEnumerable<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _bucket.GetCertificateAsync(cancellationToken);
                var bytes = StreamConverter.ToByteArray(response.ResponseStream);
                var certificate = new X509Certificate2(bytes);
                return new[] { certificate };
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return Enumerable.Empty<X509Certificate2>();
                }
                throw;
            }
        }
    }
}
