using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterAnimationViewer : MonoBehaviour
{
    [Serializable]
    private class SpriteFrame
    {
        public Sprite sprite;
        [Min(1)] public int frames = 7;
    }

    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text[] actionLabels;
    [SerializeField, Min(1)] private float framesPerSecond = 60f;
    [SerializeField] private SpriteFrame[] basic;
    [SerializeField] private SpriteFrame[] stanby;
    [SerializeField] private SpriteFrame[] hold;
    [SerializeField] private SpriteFrame[] fire;
    [SerializeField] private float rosalynScale = 0.35f;
    [SerializeField] private Vector3 rosalynPosition = new Vector3(0f, 0.1f, 0f);
    [Header("S1 Dando")]
    [SerializeField] private SpriteFrame[] dandoBasic;
    [SerializeField] private SpriteFrame[] dandoStanby;
    [SerializeField] private SpriteFrame[] dandoHold;
    [SerializeField] private SpriteFrame[] dandoAttack;
    [SerializeField] private float dandoScale = 0.35f;
    [SerializeField] private Vector3 dandoPosition = new Vector3(0f, 0.1f, 0f);

    private SpriteRenderer rosalynRenderer;
    private GameObject selectionPanel;
    private bool isRosalyn;
    private bool isDando;
    private string currentState;
    private string nextState;
    private SpriteFrame[] currentFrames;
    private int frameIndex;
    private float elapsed;

    private void Awake()
    {
        if (animator == null)
        {
            Debug.LogWarning("TestDummy Animator is not assigned.", this);
            return;
        }

        GameObject rosalyn = new GameObject("Sprite Character", typeof(SpriteRenderer));
        rosalyn.transform.SetParent(transform, false);
        rosalyn.transform.localScale = Vector3.one * rosalynScale;
        rosalynRenderer = rosalyn.GetComponent<SpriteRenderer>();
        rosalyn.SetActive(false);
        CreateSelectionPanel();
    }

    private void Start()
    {
        SelectTestDummy();
    }

    private void Update()
    {
        if ((!isRosalyn && !isDando) || currentFrames == null || currentFrames.Length == 0)
            return;

        elapsed += Time.deltaTime;
        while (elapsed >= FrameDuration(currentFrames[frameIndex]))
        {
            elapsed -= FrameDuration(currentFrames[frameIndex]);
            frameIndex++;
            if (frameIndex >= currentFrames.Length)
            {
                if (nextState != null)
                {
                    string following = nextState;
                    if (!StartRosalynState(following, StateAfterAction(following), true))
                    {
                        frameIndex = currentFrames.Length - 1;
                        nextState = null;
                        return;
                    }
                    continue;
                }
                frameIndex = 0;
            }
            ShowFrame(currentFrames[frameIndex]);
        }
    }

    private float FrameDuration(SpriteFrame frame)
    {
        return Mathf.Max(1, frame.frames) / Mathf.Max(1f, framesPerSecond);
    }

    public void ToggleCharacterSelection()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(!selectionPanel.activeSelf);
    }

    public void SelectTestDummy()
    {
        if (animator == null) return;
        isRosalyn = false;
        isDando = false;
        currentFrames = null;
        if (rosalynRenderer != null) rosalynRenderer.gameObject.SetActive(false);
        animator.gameObject.SetActive(true);
        SetLabels(false);
        if (characterNameText != null) characterNameText.text = "TestDummy";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        PlayAnimation("Default");
    }

    public void SelectRosalyn()
    {
        if (animator == null || rosalynRenderer == null || !ValidFrames(basic, "Basic")) return;
        isRosalyn = true;
        isDando = false;
        animator.gameObject.SetActive(false);
        rosalynRenderer.gameObject.SetActive(true);
        rosalynRenderer.transform.localScale = Vector3.one * rosalynScale;
        SetLabels(true);
        if (characterNameText != null) characterNameText.text = "Rosalyn";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        StartRosalynState("Basic", null);
    }

    public void SelectDando()
    {
        if (animator == null || rosalynRenderer == null || !ValidFrames(dandoBasic, "Basic")) return;
        isRosalyn = false;
        isDando = true;
        animator.gameObject.SetActive(false);
        rosalynRenderer.gameObject.SetActive(true);
        rosalynRenderer.transform.localScale = Vector3.one * dandoScale;
        SetLabels(true);
        if (characterNameText != null) characterNameText.text = "S1_Dando";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        StartRosalynState("Basic", null);
    }

    public void PlayAnimation(string stateName)
    {
        if (!isRosalyn && !isDando)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            for (int i = 0; i < animator.layerCount; i++)
            {
                string path = animator.GetLayerName(i) + "." + stateName;
                if (!animator.HasState(i, Animator.StringToHash(path))) continue;
                animator.Play(path, i, 0f);
                if (statusText != null) statusText.text = stateName;
                return;
            }
            Debug.LogWarning($"Animator state '{stateName}' was not found.", this);
            return;
        }

        // The scene's existing buttons use the original TestDummy state names.
        switch (stateName)
        {
            case "Default": stateName = "Basic"; break;
            case "Attack": stateName = "Hold"; break;
            case "Hit": stateName = isDando ? "Attack" : "Fire"; break;
        }

        if (stateName == "Basic") StartRosalynState("Basic", null);
        else if (stateName == "Stanby") StartRosalynState("Stanby", "Hold");
        else if (stateName == "Hold" || IsAction(stateName))
        {
            if (currentState == "Basic" || currentState == "Stanby")
            {
                if (!ValidFrames(GetFrames(stateName), stateName)) return;
                if (currentState == "Stanby") nextState = stateName;
                else StartRosalynState("Stanby", stateName);
            }
            else StartRosalynState(stateName, StateAfterAction(stateName));
        }
        else Debug.LogWarning($"Sprite animation '{stateName}' was not found.", this);
    }

    private static bool IsAction(string stateName)
    {
        return stateName == "Fire" || stateName == "Attack";
    }

    private string StateAfterAction(string stateName)
    {
        if (!IsAction(stateName)) return null;
        return isDando ? "Basic" : "Hold";
    }

    private SpriteFrame[] GetFrames(string stateName)
    {
        if (isDando)
        {
            switch (stateName)
            {
                case "Basic": return dandoBasic;
                case "Stanby": return dandoStanby;
                case "Hold": return dandoHold;
                case "Attack": return dandoAttack;
                default: return null;
            }
        }
        switch (stateName)
        {
            case "Basic": return basic;
            case "Stanby": return stanby;
            case "Hold": return hold;
            case "Fire": return fire;
            default: return null;
        }
    }

    private bool ValidFrames(SpriteFrame[] frames, string stateName)
    {
        if (frames != null && frames.Length > 0)
        {
            foreach (SpriteFrame frame in frames)
                if (frame == null || frame.sprite == null)
                {
                    Debug.LogWarning($"Sprite animation '{stateName}' has a missing sprite.", this);
                    return false;
                }
            return true;
        }
        Debug.LogWarning($"Sprite animation '{stateName}' has no frames.", this);
        return false;
    }

    private bool StartRosalynState(string stateName, string following, bool preserveElapsed = false)
    {
        SpriteFrame[] frames = GetFrames(stateName);
        if (!ValidFrames(frames, stateName)) return false;
        currentState = stateName;
        currentFrames = frames;
        nextState = following;
        frameIndex = 0;
        if (!preserveElapsed) elapsed = 0f;
        if (statusText != null) statusText.text = stateName;
        ShowFrame(frames[0]);
        return true;
    }

    private void ShowFrame(SpriteFrame frame)
    {
        rosalynRenderer.sprite = frame.sprite;
        Sprite sprite = frame.sprite;
        Vector2 canvasCenter = new Vector2(sprite.texture.width, sprite.texture.height) * 0.5f;
        Vector2 offset = (sprite.rect.position + sprite.pivot - canvasCenter) / sprite.pixelsPerUnit;
        float scale = isDando ? dandoScale : rosalynScale;
        Vector3 position = isDando ? dandoPosition : rosalynPosition;
        rosalynRenderer.transform.localPosition = position + (Vector3)(offset * scale);
    }

    private void SetLabels(bool rosalyn)
    {
        string[] names = rosalyn
            ? new[] { "Basic", "Stanby", "Hold", isDando ? "Attack" : "Fire" }
            : new[] { "Default", "Stanby", "Attack", "Hit" };
        if (actionLabels == null) return;
        for (int i = 0; i < actionLabels.Length && i < names.Length; i++)
            if (actionLabels[i] != null) actionLabels[i].text = names[i];
    }

    private void CreateSelectionPanel()
    {
        if (selectButton == null) return;
        Transform parent = selectButton.transform.parent;
        selectionPanel = new GameObject("Character Selection", typeof(RectTransform));
        selectionPanel.transform.SetParent(parent, false);
        RectTransform panel = (RectTransform)selectionPanel.transform;
        panel.anchorMin = new Vector2(0.05f, 0.51f);
        panel.anchorMax = new Vector2(0.25f, 0.84f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        CreateChoice("TestDummy", 2, SelectTestDummy);
        CreateChoice("Rosalyn", 1, SelectRosalyn);
        CreateChoice("S1_Dando", 0, SelectDando);
        selectionPanel.SetActive(false);
    }

    private void CreateChoice(string label, int row, UnityEngine.Events.UnityAction action)
    {
        Button choice = Instantiate(selectButton, selectionPanel.transform);
        choice.name = "Select " + label;
        choice.onClick = new Button.ButtonClickedEvent();
        choice.onClick.AddListener(action);
        RectTransform rect = (RectTransform)choice.transform;
        rect.anchorMin = new Vector2(0f, row / 3f);
        rect.anchorMax = new Vector2(1f, (row + 1) / 3f);
        rect.offsetMin = new Vector2(0f, 4f);
        rect.offsetMax = new Vector2(0f, -4f);
        TMP_Text text = choice.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = label;
    }
}
