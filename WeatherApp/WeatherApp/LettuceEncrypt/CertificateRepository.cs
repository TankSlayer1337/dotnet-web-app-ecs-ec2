using LettuceEncrypt;
using System.Security.Cryptography.X509Certificates;

namespace WeatherApp.LettuceEncrypt
{
    public class CertificateRepository : ICertificateRepository
    {
        private readonly Bucket _bucketClient;

        public CertificateRepository(Bucket bucketClient)
        {
            _bucketClient = bucketClient;
        }

        public async Task SaveAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            await _bucketClient.PutCertificateAsync(certificate, cancellationToken);
        }
    }
}
