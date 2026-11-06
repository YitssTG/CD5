using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Visual only. The one networked bit lives on the existing PlayerRole behaviour.
public class TabletPresentation : MonoBehaviour
{
    public static Canvas LocalScreen { get; private set; }
    private PlayerRole role;
    private Transform prop;
    private Transform upperLeft, upperRight;
    private Quaternion leftRest, rightRest;
    private float blend;
    private bool local;
    private MissionPresentationConfig config;
    private TMP_Text tabletStatus;
    private Image tabletScreen;
    private float focusBlend;
    private Canvas screenCanvas;
    public void Initialize(PlayerRole playerRole, bool isLocal)
    {
        role = playerRole; local = isLocal; config = MissionPresentationConfig.Load();
        GameObject go = config.tabletPrefab != null ? Instantiate(config.tabletPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (config.adaptSecurityTablet) go = AdaptSecurityTablet(go);
        go.name = "Tablet visual (local instance)";
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) { rb.isKinematic = true; rb.detectCollisions = false; }
        prop = go.transform;
        prop.SetParent(transform, false);
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            upperLeft = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            upperRight = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (upperLeft != null) leftRest = upperLeft.localRotation;
            if (upperRight != null) rightRest = upperRight.localRotation;
        }
        go.SetActive(false);
    }
    private void LateUpdate()
    {
        if (role == null || role.Object == null || !role.Object.IsValid || prop == null) return;
        bool raised = role.TabletRaised;
        blend = Mathf.MoveTowards(blend, raised ? 1 : 0, Time.unscaledDeltaTime / Mathf.Max(.1f, config.transitionSeconds));
        prop.gameObject.SetActive(blend > .001f);
        if (local)
        {
            var cam = Camera.main;
            if (cam == null) return;
            focusBlend = Mathf.MoveTowards(focusBlend, RoleTerminalUI.TabletInteractive ? 1 : 0, Time.unscaledDeltaTime * 5);
            Vector3 offset = Vector3.Lerp(config.tabletLocalPosition, new Vector3(0, -.015f, .42f), focusBlend) + Vector3.down * (1 - blend) * .5f;
            prop.position = cam.transform.TransformPoint(offset);
            prop.rotation = cam.transform.rotation * Quaternion.Euler(Vector3.Lerp(config.tabletLocalEuler, Vector3.zero, focusBlend) + Vector3.right * (1 - blend) * 35);
            prop.localScale = Vector3.one * config.tabletScale * Mathf.Lerp(1, 1.15f, focusBlend);
            if (screenCanvas != null) screenCanvas.worldCamera = cam;
        }
        else
        {
            prop.localPosition = new Vector3(0, 1.12f - (1-blend)*.35f, .32f);
            prop.localRotation = config.adaptSecurityTablet ? Quaternion.Euler(20, 180, 0) : Quaternion.Euler(20, 180, 90);
            prop.localScale = Vector3.one * config.tabletScale;
            // Procedural provisional holding pose. Original avatar and movement remain in use.
            if (blend > 0)
            {
                if (upperLeft != null) upperLeft.localRotation = Quaternion.Slerp(upperLeft.localRotation, leftRest * Quaternion.Euler(-35, 0, -35), blend);
                if (upperRight != null) upperRight.localRotation = Quaternion.Slerp(upperRight.localRotation, rightRest * Quaternion.Euler(-35, 0, 35), blend);
            }
        }
        if (tabletStatus != null && blend > .001f)
        {
            bool admin = role.MyRole == RoleType.Admin;
            Color accent = admin ? new Color(.15f,.65f,1) : new Color(.2f,1,.55f);
            tabletStatus.color = accent;
            tabletScreen.color = new Color(.015f,.025f,.04f,1);
            var mission = CooperativeIncident.Instance;
            // Remote props never display the owner's private draft or entered server ID.
            tabletStatus.text = "CYBER DEFENSE\n" + (admin ? "ADMINISTRADOR" : "PENTESTER") + "\n\n" +
                (local && mission != null && mission.IsOnline
                    ? "SESION CONECTADA\n" + (mission.Operating ? "OPERACION EN CURSO..." : "SISTEMA LISTO") + "\n\nTAB cerrar | F1 interactuar"
                    : "TERMINAL ACTIVA");
        }
    }

    private GameObject AdaptSecurityTablet(GameObject model)
    {
        // Vendor demo scripts look for a global security camera and read legacy input.
        // Disable only this spawned copy before Start; keep the imported prefab intact.
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour.GetType().Name == "TabletDisplayController" || behaviour.GetType().Name == "TabletInput")
                behaviour.enabled = false;
        var wrapper = new GameObject("Mission tablet");
        model.transform.SetParent(wrapper.transform, false);
        var renderer = model.GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return wrapper;
        Bounds bounds = renderer.bounds;
        float factor = .32f / Mathf.Max(.001f, bounds.size.x);
        model.transform.localScale *= factor;
        model.transform.localPosition = -bounds.center * factor;
        // The owner hosts the actual role UI here; remote copies show only status.
        var screen = new GameObject("Role status screen", typeof(RectTransform), typeof(Canvas));
        screen.transform.SetParent(wrapper.transform, false);
        var canvas = screen.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        screenCanvas = canvas;
        var rect = screen.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1440, 900);
        rect.localScale = Vector3.one * (.26f / 1440);
        rect.localPosition = new Vector3(0, 0, -bounds.extents.z * factor - .001f);
        tabletScreen = screen.AddComponent<Image>(); tabletScreen.raycastTarget = false;
        if (local)
        {
            screen.AddComponent<GraphicRaycaster>();
            canvas.worldCamera = Camera.main;
            LocalScreen = canvas;
            return wrapper;
        }
        var label = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(screen.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(28, 20); labelRect.offsetMax = new Vector2(-28, -20);
        tabletStatus = label.GetComponent<TextMeshProUGUI>();
        tabletStatus.fontSize = 34; tabletStatus.alignment = TextAlignmentOptions.MidlineLeft;
        tabletStatus.raycastTarget = false;
        return wrapper;
    }
    private void OnDestroy()
    {
        if (local && LocalScreen == screenCanvas)
        {
            RoleTerminalUI.ReleaseTabletScreen();
            LocalScreen = null;
        }
    }
}
