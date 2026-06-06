using UnityEngine;

/// <summary>
/// Tags runtime tile duplicates spawned by <see cref="ParallaxBackground2D"/>.
/// </summary>
[DisallowMultipleComponent]
public class ParallaxTileCloneMarker : MonoBehaviour
{
    [SerializeField] ParallaxBackground2D owner;
    [SerializeField] int layerIndex = -1;

    public ParallaxBackground2D Owner => owner;
    public int LayerIndex => layerIndex;

    public void Initialize(ParallaxBackground2D parallax, int index)
    {
        owner = parallax;
        layerIndex = index;
    }
}
