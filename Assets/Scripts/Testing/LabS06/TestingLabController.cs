using UnityEngine;

/// <summary>Manual, one-shot Play Mode controls for the S06 assertion evidence scene.</summary>
public class TestingLabController : MonoBehaviour
{
    public HealthAssertion health;
    public ActiveObjectAssertion activeObject;
    public PositionAssertion position;
    public PlayerTagAssertion playerTag;
    public ReferenceAssertion reference;

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 360, 290), "Testing Lab - Tesis CyberDefense");
        GUILayout.BeginArea(new Rect(28, 46, 336, 250));
        GUILayout.Label("Assertions manuales en Play Mode");
        if (GUILayout.Button("E1 Health: validar health != 0")) health.ValidateHealth();
        if (GUILayout.Button("E2 Critical Object: validar activo")) activeObject.ValidateActive();
        if (GUILayout.Button("E3 Position: validar XYZ aprox. 0")) position.ValidatePosition();
        if (GUILayout.Button("E4 Player Tag: validar Player")) playerTag.ValidateTag();
        if (GUILayout.Button("E5 Reference: validar null")) reference.ValidateReferenceIsNull();
        if (GUILayout.Button("E5 CreateReference + validar no null")) { reference.CreateReference(); reference.ValidateReferenceIsNotNull(); }
        GUILayout.Label("Las fallas aparecen una vez en Console.");
        GUILayout.EndArea();
    }
}
