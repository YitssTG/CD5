using UnityEngine;
using TMPro;

public class MissionWorldPresentation : MonoBehaviour
{
    private TextMeshPro serverLabel, adminLabel;
    private Light serverLight;
    private float searchAt;
    private void Update()
    {
        var m = CooperativeIncident.Instance;
        if (m == null || !m.IsOnline) return;
        if (Time.unscaledTime >= searchAt)
        {
            searchAt = Time.unscaledTime + 1;
            if (serverLabel == null) serverLabel = Label(m.FindTerminal(RoleType.Pentester), m.Definition.serverId + " / DIAGNOSTICO");
            if (adminLabel == null) adminLabel = Label(m.FindTerminal(RoleType.Admin), "PC ADMIN / TICKETS");
            if (serverLight == null && serverLabel != null)
            {
                var go = new GameObject("Incident indicator light"); go.transform.SetParent(serverLabel.transform.parent,false);go.transform.localPosition=Vector3.up;
                serverLight=go.AddComponent<Light>();serverLight.type=LightType.Point;serverLight.range=3;serverLight.intensity=1.2f;
            }
        }
        if (serverLight != null) serverLight.color = m.Phase == MissionPhase.Complete ? new Color(.2f,1,.5f) :
            m.Phase != MissionPhase.Active ? Color.cyan : m.Stage >= IncidentStage.Contained ? new Color(1,.65f,.1f) : new Color(1,.2f,.2f);
        var cam=Camera.main;if(cam==null)return;
        if(serverLabel!=null)serverLabel.transform.rotation=cam.transform.rotation;
        if(adminLabel!=null)adminLabel.transform.rotation=cam.transform.rotation;
    }
    private TextMeshPro Label(Transform terminal,string title)
    {
        if(terminal==null)return null;
        var go=new GameObject("Station label");go.transform.SetParent(terminal,false);go.transform.localPosition=Vector3.up*1.8f;
        var label=go.AddComponent<TextMeshPro>();label.text=title+"\n[E] Interactuar";label.fontSize=2;label.alignment=TextAlignmentOptions.Center;
        label.color=new Color(.35f,.95f,1);label.rectTransform.sizeDelta=new Vector2(3,.65f);return label;
    }
    private void OnDestroy()
    {
        if(serverLabel!=null)Destroy(serverLabel.gameObject);if(adminLabel!=null)Destroy(adminLabel.gameObject);if(serverLight!=null)Destroy(serverLight.gameObject);
    }
}
