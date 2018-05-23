using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aurender.Core
{
    public abstract class BindingSourceObject : INotifyPropertyChanged
    {
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
    }
}
