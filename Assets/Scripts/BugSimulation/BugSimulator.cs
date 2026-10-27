using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Self-contained visual demo for BUG-001 through BUG-006.
/// Attach to an active GameObject. Press 1-6 to show each reproduction.
/// </summary>
public class BugSimulator : MonoBehaviour
{
    [Header("Demo settings")]
    [Min(1)] public int bug002TargetFps = 22;
    [Min(1)] public int bug003TargetFps = 22;
    [Min(0.1f)] public float bug003DurationSeconds = 3f;
    [Min(0.1f)] public float bug004MegabytesPerSecond = 25f;

    private Canvas canvas;
    private TMP_Text statusText;
    private TMP_Text scoreText;
    private TMP_Text fpsText;
    private TMP_Text memoryText;
    private TMP_Text feedbackText;
    private GameObject pauseOverlay;
    private int originalTargetFrameRate;
    private float originalFixedDeltaTime;
    private Coroutine activeDemo;
    private int demoNumber;
    private float demoElapsed;
    private float simulatedMemoryMb = 420f;
    private bool showingPause;
    private bool answered;

    private void Awake()
    {
        originalTargetFrameRate = Application.targetFrameRate;
        originalFixedDeltaTime = Time.fixedDeltaTime;
        BuildDemoUI();
        ShowWelcome();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) ShowBug(1);
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) ShowBug(2);
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) ShowBug(3);
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) ShowBug(4);
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) ShowBug(5);
        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) ShowBug(6);
        if (demoNumber == 1 && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (showingPause) ResumeWithBug001();
            else PauseBug001();
        }
        if ((demoNumber == 5 || demoNumber == 6) && !answered && keyboard.enterKey.wasPressedThisFrame) SubmitAnswer();

        if (demoNumber == 2 || demoNumber == 3)
            fpsText.text = $"{1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0} FPS";

        if (demoNumber == 4)
        {
            demoElapsed += Time.unscaledDeltaTime;
            simulatedMemoryMb += bug004MegabytesPerSecond * Time.unscaledDeltaTime;
            memoryText.text = $"{simulatedMemoryMb:F0} MB\n{demoElapsed / 60f:00}:{demoElapsed % 60f:00}";
        }
    }

    private void BuildDemoUI()
    {
        GameObject canvasObject = new GameObject("Bug Demo Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        CreatePanel("Background", canvasObject.transform, new Vector2(0, 0), new Vector2(1, 1), new Color(0.035f, 0.055f, 0.09f), 0);
        CreatePanel("Header", canvasObject.transform, new Vector2(0, 0.82f), new Vector2(1, 1), new Color(0.055f, 0.09f, 0.14f), 0);
        CreateText("Brand", canvasObject.transform, "CYBER DEFENSE", 30, new Vector2(0.04f, 0.86f), new Vector2(0.5f, 0.97f), TextAlignmentOptions.MidlineLeft, new Color(0.3f, 0.9f, 0.88f));
        CreateText("Profile", canvasObject.transform, "JUGADOR 01", 18, new Vector2(0.77f, 0.86f), new Vector2(0.96f, 0.97f), TextAlignmentOptions.MidlineRight, Color.white);

        CreatePanel("Main Card", canvasObject.transform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.68f), new Color(0.075f, 0.11f, 0.17f), 16);
        statusText = CreateText("Status", canvasObject.transform, "RETO ACTIVO  •  SQL INJECTION", 20, new Vector2(0.09f, 0.59f), new Vector2(0.7f, 0.66f), TextAlignmentOptions.MidlineLeft, new Color(0.5f, 0.8f, 1f));
        scoreText = CreateText("Score", canvasObject.transform, "PUNTOS   150", 34, new Vector2(0.09f, 0.48f), new Vector2(0.55f, 0.58f), TextAlignmentOptions.MidlineLeft, new Color(0.98f, 0.78f, 0.25f));
        fpsText = CreateText("FPS", canvasObject.transform, "55 FPS", 54, new Vector2(0.09f, 0.35f), new Vector2(0.42f, 0.48f), TextAlignmentOptions.MidlineLeft, new Color(0.3f, 0.95f, 0.65f));
        memoryText = CreateText("Memory", canvasObject.transform, "MEMORIA  420 MB", 28, new Vector2(0.09f, 0.34f), new Vector2(0.65f, 0.48f), TextAlignmentOptions.MidlineLeft, new Color(0.3f, 0.95f, 0.65f));
        feedbackText = CreateText("Feedback", canvasObject.transform, "", 25, new Vector2(0.09f, 0.22f), new Vector2(0.9f, 0.42f), TextAlignmentOptions.MidlineLeft, Color.white);
        CreateText("Footer", canvasObject.transform, "CYBER DEFENSE  •  SESIÓN ACTIVA", 17, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.12f), TextAlignmentOptions.Center, new Color(0.65f, 0.72f, 0.82f));

        pauseOverlay = CreatePanel("Pause Overlay", canvasObject.transform, new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.62f), new Color(0.02f, 0.03f, 0.06f, 0.96f), 30);
        CreateText("Paused Label", pauseOverlay.transform, "PAUSA", 30, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f), TextAlignmentOptions.Center, Color.white);
        pauseOverlay.SetActive(false);
    }

    private void ShowWelcome()
    {
        statusText.text = "RETO ACTIVO  •  SQL INJECTION";
        scoreText.text = "PUNTOS   150";
        scoreText.gameObject.SetActive(true);
        fpsText.gameObject.SetActive(false);
        memoryText.gameObject.SetActive(false);
        feedbackText.text = "Selecciona una respuesta";
        pauseOverlay.SetActive(false);
    }

    private void ShowBug(int number)
    {
        if (activeDemo != null) StopCoroutine(activeDemo);
        Application.targetFrameRate = originalTargetFrameRate;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        demoNumber = number;
        demoElapsed = 0f;
        answered = false;
        showingPause = false;
        pauseOverlay.SetActive(false);
        feedbackText.text = "";
        scoreText.gameObject.SetActive(number == 1);
        fpsText.gameObject.SetActive(number == 2 || number == 3);
        memoryText.gameObject.SetActive(number == 4);
        fpsText.color = new Color(0.3f, 0.95f, 0.65f);

        switch (number)
        {
            case 1:
                statusText.text = "RETO ACTIVO  •  SQL INJECTION";
                scoreText.text = "PUNTOS   150";
                break;
            case 2:
                statusText.text = "RETO ACTIVO  •  SQL INJECTION";
                Application.targetFrameRate = bug002TargetFps;
                fpsText.color = Color.red;
                break;
            case 3:
                statusText.text = "CARGANDO SIGUIENTE RETO...";
                fpsText.text = "55 FPS";
                activeDemo = StartCoroutine(TransitionDemo());
                break;
            case 4:
                statusText.text = "RETO ACTIVO  •  SQL INJECTION";
                memoryText.text = $"{simulatedMemoryMb:F0} MB\n00:00";
                memoryText.color = new Color(1f, 0.55f, 0.3f);
                break;
            case 5:
                statusText.text = "RETO ACTIVO  •  SQL INJECTION";
                feedbackText.text = "¿Qué método se usa para proteger consultas SQL?";
                break;
            case 6:
                statusText.text = "RETO ACTIVO  •  SQL INJECTION";
                feedbackText.text = "SELECT * FROM users WHERE id = '";
                break;
        }
    }

    private IEnumerator TransitionDemo()
    {
        Application.targetFrameRate = bug003TargetFps;
        Time.fixedDeltaTime = 1f / bug003TargetFps;
        float end = Time.realtimeSinceStartup + bug003DurationSeconds;
        while (Time.realtimeSinceStartup < end)
        {
            fpsText.text = $"{Mathf.Min(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), bug003TargetFps):F0} FPS";
            fpsText.color = Color.red;
            yield return null;
        }
        Application.targetFrameRate = originalTargetFrameRate;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        fpsText.text = "55 FPS";
        fpsText.color = new Color(0.3f, 0.95f, 0.65f);
        statusText.text = "RETO ACTIVO  •  SQL INJECTION";
    }

    private void ResumeWithBug001()
    {
        showingPause = false;
        pauseOverlay.SetActive(false);
        scoreText.text = "PUNTOS   0";
        scoreText.color = Color.red;
        statusText.text = "RETO REANUDADO  •  PROGRESO CONSERVADO";
    }

    private void SubmitAnswer()
    {
        answered = true;
        if (demoNumber == 5)
        {
            feedbackText.text = "✕  RESPUESTA INCORRECTA";
            feedbackText.color = new Color(1f, 0.3f, 0.3f);
        }
        else
        {
            feedbackText.text = "✓  RESPUESTA CORRECTA";
            feedbackText.color = new Color(0.3f, 0.95f, 0.55f);
        }
    }

    private void PauseBug001()
    {
        showingPause = true;
        pauseOverlay.SetActive(true);
        statusText.text = "RETO PAUSADO";
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color, float inset)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        panel.GetComponent<UnityEngine.UI.Image>().color = color;
        return panel;
    }

    private TMP_Text CreateText(string name, Transform parent, string content, float size, Vector2 min, Vector2 max, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        return text;
    }

    private void OnDestroy()
    {
        Application.targetFrameRate = originalTargetFrameRate;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }
}
