using UnityEngine;

/// <summary>
/// Scene-resident MonoBehaviour that holds serialized AudioClip references.
/// Created during F5 by AudioClipHolderSetup. AudioSetup reads clips at runtime.
/// </summary>
public class AudioClipHolder : MonoBehaviour
{
    [System.Serializable]
    public struct AudioClipEntry
    {
        public string Name;
        public AudioClip Clip;
    }

    public AudioClipEntry[] Clips;
}
