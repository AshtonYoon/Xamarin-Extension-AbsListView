using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Utility;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aurender.Core.Player.DeviceControl
{

    public class AurenderSAMBAServerSetting : DeviceControlBase, INotifyPropertyChanged
    {
        private string password;

        public AurenderSAMBAServerSetting(IAurender aurender) : base(aurender) { }

        public string Password
        {
            get => password;
            set
            {
                SetProperty(ref password, value);
                OnPropertyChanged("Password");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName]string name = null)
        {
            if (!value.Equals(field))
            {
                field = value;
                OnPropertyChanged(name);
            }
        }

        public override async Task<bool> LoadInformation()
        {
            String url = "/php/smbPwd";

            var result = await GetResponse(url).ConfigureAwait(false);

            if (result.isSucess)
            {
                var regex = new Regex(@"<pre class=pd>(.*)\s*</pre>");
                var match = regex.Match(result.responseString);
                if (match.Success && match.Groups.Count > 1)
                {
                    this.Password = match.Groups[1].Value;
                }

            }

            return result.isSucess;
        }

        public async Task<bool> UpdatePassword(String newPassword)
        {
            String url = $"/php/smbConfig?newPwd={newPassword.URLEncodedString()}&type=rw";

            var result = await GetResponse(url).ConfigureAwait(false);

            return result.isSucess;
        }
    }

}