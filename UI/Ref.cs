using System;

namespace lstwoMODS_Core.UI;

public class Ref<T>
{
    private T _value;

    /// <summary>Fired on the calling thread whenever Value is set from code.</summary>
    public event Action<T> Changed;

    public T Value
    {
        get => _value;
        set { _value = value; Changed?.Invoke(value); }
    }

    public Ref(T value = default)
    {
        _value = value;
    }
}
