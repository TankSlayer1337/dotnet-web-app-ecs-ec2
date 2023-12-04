using Amazon.S3;
using LettuceEncrypt.Accounts;
using Newtonsoft.Json;
using System.Net;

namespace WeatherApp.LettuceEncrypt
{
    public class AccountStore : IAccountStore
    {
        private readonly Bucket _bucket;

        public AccountStore(Bucket bucket)
        {
            _bucket = bucket;
        }

        public async Task SaveAccountAsync(AccountModel account, CancellationToken cancellationToken)
        {
            var accountSerializable = new AccountModelSerializable(account);
            await _bucket.PutAccountAsync(accountSerializable, cancellationToken);
        }

        public async Task<AccountModel?> GetAccountAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _bucket.GetAccountAsync(cancellationToken);
                var reader = new StreamReader(response.ResponseStream);
                var serializedAccountModel = reader.ReadToEnd();
                var accountModelSerializable = JsonConvert.DeserializeObject<AccountModelSerializable>(serializedAccountModel) ?? throw new Exception("Failed to deserialize LettuceEncrypt Account data");
                return accountModelSerializable.ToAccountModel();
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw e;
            }
        }
    }
}
