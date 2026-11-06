using UnityEngine;

[CreateAssetMenu(menuName = "CyberDefense/Mission presentation")]
public class MissionPresentationConfig : ScriptableObject
{
    public GameObject tabletPrefab;
    public IncidentUISkin pentesterSkin;
    public IncidentUISkin adminSkin;
    public AudioClip notification;
    public AudioClip click;
    [Range(.1f, 1)] public float transitionSeconds = .25f;
    public Vector3 tabletLocalPosition = new Vector3(.16f, -.22f, .45f);
    public Vector3 tabletLocalEuler = new Vector3(0, 180, 90);
    public float tabletScale = 1.4f;
    public bool adaptSecurityTablet;
    private static MissionPresentationConfig cached;
    public static MissionPresentationConfig Load()
    {
        if (cached == null) cached = Resources.Load<MissionPresentationConfig>("MissionPresentation");
        if (cached == null) cached = CreateInstance<MissionPresentationConfig>();
        return cached;
    }
}
