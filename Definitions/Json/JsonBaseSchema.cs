using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RoflanArchives.Core.Definitions.Json;

public abstract class JsonBaseSchema : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;



    public virtual void OnPropertyChanged(
        [CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this,
            new PropertyChangedEventArgs(propertyName));
    }
}
