using LettuceEncrypt.Accounts;
using Newtonsoft.Json;

namespace WeatherApp.LettuceEncrypt
{
    public class AccountModelSerializable
    {
        public string PrivateKey;
        public int Id;
        public string[] EmailAddresses;

        public AccountModelSerializable(AccountModel accountModel)
        {
            PrivateKey = Convert.ToBase64String(accountModel.PrivateKey);
            Id = accountModel.Id;
            EmailAddresses = accountModel.EmailAddresses;
        }

        [JsonConstructor]
        public AccountModelSerializable(string privateKey, int id, string[] emailAddresses)
        {
            PrivateKey = privateKey;
            Id = id;
            EmailAddresses = emailAddresses;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public AccountModel ToAccountModel()
        {
            return new AccountModel
            {
                PrivateKey = Convert.FromBase64String(PrivateKey),
                Id = Id,
                EmailAddresses = EmailAddresses
            };
        }
    }
}
