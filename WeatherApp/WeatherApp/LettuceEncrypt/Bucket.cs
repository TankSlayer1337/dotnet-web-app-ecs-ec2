using Amazon.S3.Model;
using Amazon.S3;
using System.Security.Cryptography.X509Certificates;
using WeatherApp.Utilities;
using Amazon;

namespace WeatherApp.LettuceEncrypt
{
    public class Bucket
    {
        private readonly AmazonS3Client _client;
        private readonly string _bucketName;
        private const string CertificateKey = "lettuce-encrypt-certificate.pfx";
        private const string AccountModelKey = "account-model.json";

        public Bucket()
        {
            var region = EnvironmentVariableGetter.Get("BUCKET_REGION");
            _client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
            _bucketName = EnvironmentVariableGetter.Get("BUCKET_NAME");
        }

        public async Task<PutObjectResponse> PutCertificateAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            byte[] certData = certificate.Export(X509ContentType.Pfx);
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = CertificateKey,
                InputStream = new MemoryStream(certData)
            };
            return await _client.PutObjectAsync(request, cancellationToken);
        }

        public async Task<GetObjectResponse> GetCertificateAsync(CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = CertificateKey
            };
            return await _client.GetObjectAsync(request, cancellationToken);
        }

        public async Task<PutObjectResponse> PutAccountAsync(AccountModelSerializable accountModel, CancellationToken cancellationToken)
        {
            var body = accountModel.ToJson();
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = AccountModelKey,
                ContentBody = body
            };
            return await _client.PutObjectAsync(request, cancellationToken);
        }

        public async Task<GetObjectResponse> GetAccountAsync(CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = AccountModelKey
            };
            return await _client.GetObjectAsync(request, cancellationToken);
        }
    }
}
