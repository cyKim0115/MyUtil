using UnityEngine;

[System.Serializable]
public struct DoubleColor
{
    [SerializeField] private Color _top;
    [SerializeField] private Color _bottom;

    public Color top
    {
        get => _top;
        set => _top = value;
    }

    public Color bottom
    {
        get => _bottom;
        set => _bottom = value;
    }

    public DoubleColor(Color top, Color bottom)
    {
        _top = top;
        _bottom = bottom;
    }
}