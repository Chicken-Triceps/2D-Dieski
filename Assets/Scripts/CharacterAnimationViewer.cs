using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CharacterAnimationViewer : MonoBehaviour
{
    [Serializable]
    private class SpriteFrame
    {
        public Sprite sprite;
        public Sprite extraSprite;
        [Min(1)] public int frames = 7;
    }

    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text[] actionLabels;
    [SerializeField] private Sprite testDummyPortrait;
    [SerializeField] private Sprite rosalynPortrait;
    [SerializeField] private Sprite dandoPortrait;
    [SerializeField] private Sprite revolverPortrait;
    [SerializeField] private Sprite pokPortrait;
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
    [Header("S1 Revolver")]
    [SerializeField] private SpriteFrame[] revolverBasic;
    [SerializeField] private SpriteFrame[] revolverStanby;
    [SerializeField] private SpriteFrame[] revolverHold;
    [SerializeField] private SpriteFrame[] revolverAttack;
    [SerializeField] private float revolverScale = 0.35f;
    [SerializeField] private Vector3 revolverPosition = new Vector3(0f, 0.1f, 0f);
    [Header("S1 Pok")]
    [SerializeField] private SpriteFrame[] pokBasic;
    [SerializeField] private SpriteFrame[] pokStanby;
    [SerializeField] private SpriteFrame[] pokHold;
    [SerializeField] private SpriteFrame[] pokCast;
    [SerializeField] private float pokScale = 0.35f;
    [SerializeField] private Vector3 pokPosition = new Vector3(0f, 0.1f, 0f);

    private SpriteRenderer rosalynRenderer;
    private SpriteRenderer extraSpriteRenderer;
    private GameObject selectionPanel;
    private bool isRosalyn;
    private bool isDando;
    private bool isRevolver;
    private bool isPok;
    private string currentState;
    private string nextState;
    private SpriteFrame[] currentFrames;
    private int frameIndex;
    private float elapsed;
    private GameObject animationControlPanel;
    private readonly List<FrameControl> frameControls = new List<FrameControl>();
    private readonly List<AnimationFrameControl> animationFrameControls = new List<AnimationFrameControl>();
    private const int ResetFrameCount = 10;
    private const string FramePreferencePrefix = "CharacterAnimationViewer.Frames.";

    private class FrameControl
    {
        public SpriteFrame frame;
        public string preferenceKey;
        public int value;
        public TMP_Text valueText;
        public AnimationFrameControl animation;
    }

    private class AnimationFrameControl
    {
        public readonly List<FrameControl> frames = new List<FrameControl>();
        public TMP_Text valueText;
    }

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
        GameObject extra = new GameObject("Extra Sprite", typeof(SpriteRenderer));
        extra.transform.SetParent(rosalyn.transform, false);
        extraSpriteRenderer = extra.GetComponent<SpriteRenderer>();
        extraSpriteRenderer.sortingOrder = rosalynRenderer.sortingOrder + 1;
        rosalyn.SetActive(false);
        LoadSavedFrames("Rosalyn", new[] { "Basic", "Stanby", "Hold", "Fire" },
            new[] { basic, stanby, hold, fire });
        LoadSavedFrames("S1_Dando", new[] { "Basic", "Stanby", "Hold", "Attack" },
            new[] { dandoBasic, dandoStanby, dandoHold, dandoAttack });
        LoadSavedFrames("S1_Revolver", new[] { "Basic", "Stanby", "Hold", "Attack" },
            new[] { revolverBasic, revolverStanby, revolverHold, revolverAttack });
        LoadSavedFrames("S1_Pok", new[] { "Basic", "Stanby", "Hold", "Cast" },
            new[] { pokBasic, pokStanby, pokHold, pokCast });
        CreateSelectionPanel();
    }

    private void Start()
    {
        SelectTestDummy();
    }

    private void Update()
    {
        if (animationControlPanel != null && EscapePressed())
            CloseAnimationControl();

        if ((!isRosalyn && !isDando && !isRevolver && !isPok) || currentFrames == null || currentFrames.Length == 0)
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

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
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
        bool reopenControl = animationControlPanel != null;
        CloseAnimationControl();
        isRosalyn = false;
        isDando = false;
        isRevolver = false;
        isPok = false;
        currentFrames = null;
        if (rosalynRenderer != null) rosalynRenderer.gameObject.SetActive(false);
        animator.gameObject.SetActive(true);
        SetLabels(false);
        if (characterNameText != null) characterNameText.text = "TestDummy";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        PlayAnimation("Default");
        if (reopenControl) CreateAnimationControl();
    }

    public void SelectRosalyn()
    {
        if (animator == null || rosalynRenderer == null || !ValidFrames(basic, "Basic")) return;
        bool reopenControl = animationControlPanel != null;
        CloseAnimationControl();
        isRosalyn = true;
        isDando = false;
        isRevolver = false;
        isPok = false;
        animator.gameObject.SetActive(false);
        rosalynRenderer.gameObject.SetActive(true);
        rosalynRenderer.transform.localScale = Vector3.one * rosalynScale;
        SetLabels(true);
        if (characterNameText != null) characterNameText.text = "Rosalyn";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        StartRosalynState("Basic", null);
        if (reopenControl) CreateAnimationControl();
    }

    public void SelectDando()
    {
        if (animator == null || rosalynRenderer == null || !ValidFrames(dandoBasic, "Basic")) return;
        bool reopenControl = animationControlPanel != null;
        CloseAnimationControl();
        isRosalyn = false;
        isDando = true;
        isRevolver = false;
        isPok = false;
        animator.gameObject.SetActive(false);
        rosalynRenderer.gameObject.SetActive(true);
        rosalynRenderer.transform.localScale = Vector3.one * dandoScale;
        SetLabels(true);
        if (characterNameText != null) characterNameText.text = "S1_Dando";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        StartRosalynState("Basic", null);
        if (reopenControl) CreateAnimationControl();
    }

    public void SelectRevolver()
    {
        if (animator == null || rosalynRenderer == null || !ValidFrames(revolverBasic, "Basic")) return;
        bool reopenControl = animationControlPanel != null;
        CloseAnimationControl();
        isRosalyn = false;
        isDando = false;
        isRevolver = true;
        isPok = false;
        animator.gameObject.SetActive(false);
        rosalynRenderer.gameObject.SetActive(true);
        rosalynRenderer.transform.localScale = Vector3.one * revolverScale;
        SetLabels(true);
        if (characterNameText != null) characterNameText.text = "S1_Revolver";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        StartRosalynState("Basic", null);
        if (reopenControl) CreateAnimationControl();
    }

    public void SelectPok()
    {
        if (animator == null || rosalynRenderer == null || !ValidFrames(pokBasic, "Basic")) return;
        bool reopenControl = animationControlPanel != null;
        CloseAnimationControl();
        isRosalyn = false;
        isDando = false;
        isRevolver = false;
        isPok = true;
        animator.gameObject.SetActive(false);
        rosalynRenderer.gameObject.SetActive(true);
        rosalynRenderer.transform.localScale = Vector3.one * pokScale;
        SetLabels(true);
        if (characterNameText != null) characterNameText.text = "S1_Pok";
        if (selectionPanel != null) selectionPanel.SetActive(false);
        StartRosalynState("Basic", null);
        if (reopenControl) CreateAnimationControl();
    }

    public void PlayAnimation(string stateName)
    {
        if (!isRosalyn && !isDando && !isRevolver && !isPok)
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
            case "Hit": stateName = isPok ? "Cast" : isDando || isRevolver ? "Attack" : "Fire"; break;
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
        return stateName == "Fire" || stateName == "Attack" || stateName == "Cast";
    }

    private string StateAfterAction(string stateName)
    {
        if (!IsAction(stateName)) return null;
        return isDando ? "Basic" : "Hold";
    }

    private SpriteFrame[] GetFrames(string stateName)
    {
        if (isPok)
        {
            switch (stateName)
            {
                case "Basic": return pokBasic;
                case "Stanby": return pokStanby;
                case "Hold": return pokHold;
                case "Cast": return pokCast;
                default: return null;
            }
        }
        if (isRevolver)
        {
            switch (stateName)
            {
                case "Basic": return revolverBasic;
                case "Stanby": return revolverStanby;
                case "Hold": return revolverHold;
                case "Attack": return revolverAttack;
                default: return null;
            }
        }
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
        extraSpriteRenderer.sprite = frame.extraSprite;
        if (frame.extraSprite != null)
        {
            Vector2 primaryPivot = (sprite.rect.position + sprite.pivot) / sprite.pixelsPerUnit;
            Vector2 extraPivot = (frame.extraSprite.rect.position + frame.extraSprite.pivot) / frame.extraSprite.pixelsPerUnit;
            extraSpriteRenderer.transform.localPosition = extraPivot - primaryPivot;
        }
        Vector2 canvasCenter = new Vector2(sprite.texture.width, sprite.texture.height) * 0.5f;
        Vector2 offset = (sprite.rect.position + sprite.pivot - canvasCenter) / sprite.pixelsPerUnit;
        float scale = isPok ? pokScale : isRevolver ? revolverScale : isDando ? dandoScale : rosalynScale;
        Vector3 position = isPok ? pokPosition : isRevolver ? revolverPosition : isDando ? dandoPosition : rosalynPosition;
        rosalynRenderer.transform.localPosition = position + (Vector3)(offset * scale);
    }

    private void SetLabels(bool rosalyn)
    {
        string[] names = rosalyn
            ? new[] { "Basic", "Stanby", "Hold", isPok ? "Cast" : isDando || isRevolver ? "Attack" : "Fire" }
            : new[] { "Default", "Stanby", "Attack", "Hit" };
        if (actionLabels == null) return;
        for (int i = 0; i < actionLabels.Length && i < names.Length; i++)
            if (actionLabels[i] != null) actionLabels[i].text = names[i];
    }

    private void LoadSavedFrames(string character, string[] states, SpriteFrame[][] sequences)
    {
        for (int i = 0; i < states.Length; i++)
        {
            SpriteFrame[] sequence = sequences[i];
            if (sequence == null) continue;
            for (int j = 0; j < sequence.Length; j++)
            {
                SpriteFrame frame = sequence[j];
                if (frame == null) continue;
                string key = FrameKey(character, states[i], j, frame);
                if (PlayerPrefs.HasKey(key))
                    frame.frames = Mathf.Clamp(PlayerPrefs.GetInt(key), 1, 9999);
            }
        }
    }

    private static string FrameKey(string character, string state, int index, SpriteFrame frame)
    {
        string spriteName = frame.sprite != null ? frame.sprite.name : "missing";
        return FramePreferencePrefix + character + "." + state + "." + index + "." + spriteName;
    }

    public void ToggleAnimationControl()
    {
        if (animationControlPanel != null)
        {
            CloseAnimationControl();
            return;
        }
        if (selectButton == null) return;
        if (selectionPanel != null) selectionPanel.SetActive(false);
        CreateAnimationControl();
    }

    private void CloseAnimationControl()
    {
        if (animationControlPanel == null) return;
        Destroy(animationControlPanel);
        animationControlPanel = null;
        frameControls.Clear();
        animationFrameControls.Clear();
    }

    private void CreateAnimationControl()
    {
        string character = isRosalyn ? "Rosalyn" : isDando ? "S1_Dando" : isRevolver ? "S1_Revolver" : isPok ? "S1_Pok" : "TestDummy";
        string[] states = isPok
            ? new[] { "Basic", "Stanby", "Hold", "Cast" }
            : isRosalyn
            ? new[] { "Basic", "Stanby", "Hold", "Fire" }
            : new[] { "Basic", "Stanby", "Hold", "Attack" };

        animationControlPanel = new GameObject("Panel | Animation Control", typeof(RectTransform), typeof(Image));
        animationControlPanel.transform.SetParent(selectButton.transform.parent, false);
        RectTransform panel = (RectTransform)animationControlPanel.transform;
        panel.anchorMin = new Vector2(0.685f, 0.21f);
        panel.anchorMax = new Vector2(0.995f, 0.84f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        animationControlPanel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.98f);
        panel.SetAsLastSibling();

        TMP_Text title = CreateControlText(panel, "Animation Control | " + character, 24);
        SetControlRect((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -48f), new Vector2(-68f, -8f));
        Button close = CreateControlButton(panel, "X", CloseAnimationControl);
        SetControlRect((RectTransform)close.transform, Vector2.one, Vector2.one,
            new Vector2(-52f, -48f), new Vector2(-8f, -8f));

        Button apply = CreateControlButton(panel, "Apply", ApplyFrameControls);
        SetControlRect((RectTransform)apply.transform, Vector2.zero, new Vector2(0.5f, 0f),
            new Vector2(10f, 8f), new Vector2(-4f, 64f));
        Button reset = CreateControlButton(panel, "Reset", ResetFrameControls);
        SetControlRect((RectTransform)reset.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0f),
            new Vector2(4f, 8f), new Vector2(-10f, 64f));
        SetControlButtonTextSize(apply, 24f);
        SetControlButtonTextSize(reset, 24f);

        if (character == "TestDummy")
        {
            TMP_Text message = CreateControlText(panel, "TestDummy uses Animator clips, not sprite frames.", 20);
            SetControlRect((RectTransform)message.transform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f),
                new Vector2(18f, 0f), new Vector2(-18f, 0f));
            apply.interactable = false;
            reset.interactable = false;
            return;
        }

        GameObject scrollObject = new GameObject("Frames", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel, false);
        RectTransform scrollRect = (RectTransform)scrollObject.transform;
        SetControlRect(scrollRect, Vector2.zero, Vector2.one, new Vector2(8f, 72f), new Vector2(-8f, -56f));
        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollRect, false);
        RectTransform viewport = (RectTransform)viewportObject.transform;
        SetControlRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = true;
        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewport, false);
        RectTransform content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 40f;

        float top = 0f;
        Color[] borderColors =
        {
            new Color(0.38f, 0.75f, 0.95f, 1f),
            new Color(0.96f, 0.71f, 0.34f, 1f),
            new Color(0.43f, 0.83f, 0.58f, 1f),
            new Color(0.91f, 0.46f, 0.55f, 1f)
        };
        int stateIndex = 0;
        foreach (string state in states)
        {
            SpriteFrame[] frames = GetFrames(state);
            Color borderColor = borderColors[stateIndex++];
            if (frames == null) continue;
            int frameCount = 0;
            foreach (SpriteFrame frame in frames)
                if (frame != null) frameCount++;
            CreateAnimationBorder(content, top, 50f + 38f * frameCount, borderColor);
            TMP_Text heading = CreateControlText(content, state, 21);
            SetControlRect((RectTransform)heading.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -top - 50f), new Vector2(-184f, -top));
            AnimationFrameControl animation = new AnimationFrameControl();
            animationFrameControls.Add(animation);
            Button animationMinus = CreateControlButton(content, "-", () => AdjustAnimationFrames(animation, -1));
            SetControlRect((RectTransform)animationMinus.transform, Vector2.one, Vector2.one,
                new Vector2(-174f, -top - 47f), new Vector2(-128f, -top - 1f));
            animation.valueText = CreateControlText(content, "", 18);
            animation.valueText.alignment = TextAlignmentOptions.Center;
            SetControlRect((RectTransform)animation.valueText.transform, Vector2.one, Vector2.one,
                new Vector2(-124f, -top - 47f), new Vector2(-64f, -top - 1f));
            Button animationPlus = CreateControlButton(content, "+", () => AdjustAnimationFrames(animation, 1));
            SetControlRect((RectTransform)animationPlus.transform, Vector2.one, Vector2.one,
                new Vector2(-60f, -top - 47f), new Vector2(-14f, -top - 1f));
            top += 50f;
            for (int i = 0; i < frames.Length; i++)
            {
                SpriteFrame frame = frames[i];
                if (frame == null) continue;
                FrameControl control = new FrameControl
                {
                    frame = frame,
                    value = Mathf.Clamp(frame.frames, 1, 9999),
                    preferenceKey = FrameKey(character, state, i, frame),
                    animation = animation
                };
                frameControls.Add(control);
                animation.frames.Add(control);
                string label = frame.sprite != null ? frame.sprite.name : state + " " + (i + 1);
                TMP_Text name = CreateControlText(content, label, 16);
                SetControlRect((RectTransform)name.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(18f, -top - 38f), new Vector2(-164f, -top));
                Button minus = CreateControlButton(content, "-", () => AdjustFrameControl(control, -1));
                SetControlRect((RectTransform)minus.transform, Vector2.one, Vector2.one,
                    new Vector2(-150f, -top - 36f), new Vector2(-114f, -top - 2f));
                control.valueText = CreateControlText(content, control.value.ToString(), 18);
                control.valueText.alignment = TextAlignmentOptions.Center;
                SetControlRect((RectTransform)control.valueText.transform, Vector2.one, Vector2.one,
                    new Vector2(-110f, -top - 36f), new Vector2(-54f, -top - 2f));
                Button plus = CreateControlButton(content, "+", () => AdjustFrameControl(control, 1));
                SetControlRect((RectTransform)plus.transform, Vector2.one, Vector2.one,
                    new Vector2(-50f, -top - 36f), new Vector2(-14f, -top - 2f));
                top += 38f;
            }
            animationMinus.interactable = animation.frames.Count > 0;
            animationPlus.interactable = animation.frames.Count > 0;
            UpdateAnimationFrameValue(animation);
            top += 18f;
        }
        content.sizeDelta = new Vector2(0f, top + 8f);
    }

    private void ApplyFrameControls()
    {
        foreach (FrameControl control in frameControls)
        {
            control.frame.frames = control.value;
            PlayerPrefs.SetInt(control.preferenceKey, control.value);
        }
        PlayerPrefs.Save();
    }

    private void ResetFrameControls()
    {
        foreach (FrameControl control in frameControls)
        {
            control.value = ResetFrameCount;
            control.valueText.text = control.value.ToString();
        }
        foreach (AnimationFrameControl animation in animationFrameControls)
            UpdateAnimationFrameValue(animation);
        ApplyFrameControls();
    }

    private static void AdjustFrameControl(FrameControl control, int delta)
    {
        control.value = Mathf.Clamp(control.value + delta, 1, 9999);
        control.valueText.text = control.value.ToString();
        UpdateAnimationFrameValue(control.animation);
    }

    private static void AdjustAnimationFrames(AnimationFrameControl animation, int delta)
    {
        if (animation.frames.Count == 0) return;
        int value = Mathf.Clamp(animation.frames[0].value + delta, 1, 9999);
        foreach (FrameControl control in animation.frames)
        {
            control.value = value;
            control.valueText.text = value.ToString();
        }
        UpdateAnimationFrameValue(animation);
    }

    private static void UpdateAnimationFrameValue(AnimationFrameControl animation)
    {
        if (animation.frames.Count == 0)
        {
            animation.valueText.text = "-";
            return;
        }
        int value = animation.frames[0].value;
        foreach (FrameControl control in animation.frames)
        {
            if (control.value != value)
            {
                animation.valueText.text = "Mixed";
                return;
            }
        }
        animation.valueText.text = value.ToString();
    }

    private TMP_Text CreateControlText(Transform parent, string value, float size)
    {
        TMP_Text template = selectButton.GetComponentInChildren<TMP_Text>();
        TMP_Text text = Instantiate(template, parent);
        text.name = "Text | " + value;
        text.text = value;
        text.color = Color.white;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateControlButton(Transform parent, string value, UnityEngine.Events.UnityAction action)
    {
        Button button = Instantiate(selectButton, parent);
        button.name = "Button | " + value;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.text = value;
        label.fontSize = 20f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 20f;
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void SetControlButtonTextSize(Button button, float size)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.fontSize = size;
        label.fontSizeMax = size;
    }

    private static void CreateAnimationBorder(RectTransform parent, float top, float height, Color color)
    {
        CreateBorderEdge(parent, color, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(2f, -top - 3f), new Vector2(-2f, -top));
        CreateBorderEdge(parent, color, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(2f, -top - height), new Vector2(-2f, -top - height + 3f));
        CreateBorderEdge(parent, color, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(2f, -top - height), new Vector2(5f, -top));
        CreateBorderEdge(parent, color, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-5f, -top - height), new Vector2(-2f, -top));
    }

    private static void CreateBorderEdge(Transform parent, Color color, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject edge = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edge.transform.SetParent(parent, false);
        Image image = edge.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        SetControlRect((RectTransform)edge.transform, min, max, offsetMin, offsetMax);
    }

    private static void SetControlRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void CreateSelectionPanel()
    {
        if (selectButton == null) return;
        Transform parent = selectButton.transform.parent;
        selectionPanel = new GameObject("Character Selection", typeof(RectTransform));
        selectionPanel.transform.SetParent(parent, false);
        RectTransform panel = (RectTransform)selectionPanel.transform;
        panel.anchorMin = new Vector2(0.05f, 0.38f);
        panel.anchorMax = new Vector2(0.23f, 0.84f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        CreateChoice("TestDummy", 4, SelectTestDummy, testDummyPortrait);
        CreateChoice("Rosalyn", 3, SelectRosalyn, rosalynPortrait);
        CreateChoice("S1_Dando", 2, SelectDando, dandoPortrait);
        CreateChoice("S1_Revolver", 1, SelectRevolver, revolverPortrait);
        CreateChoice("S1_Pok", 0, SelectPok, pokPortrait);
        selectionPanel.SetActive(false);
    }

    private void CreateChoice(string label, int row, UnityEngine.Events.UnityAction action, Sprite portrait)
    {
        Button choice = Instantiate(selectButton, selectionPanel.transform);
        choice.name = "Select " + label;
        choice.onClick = new Button.ButtonClickedEvent();
        choice.onClick.AddListener(action);
        RectTransform rect = (RectTransform)choice.transform;
        rect.anchorMin = new Vector2(0f, row / 5f);
        rect.anchorMax = new Vector2(1f, (row + 1) / 5f);
        rect.offsetMin = new Vector2(0f, 4f);
        rect.offsetMax = new Vector2(0f, -4f);
        TMP_Text text = choice.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = label;
            text.enableAutoSizing = true;
            text.fontSizeMin = 9f;
            text.fontSizeMax = 24f;
            SetControlRect((RectTransform)text.transform, new Vector2(0.29f, 0f), Vector2.one,
                new Vector2(0f, 2f), new Vector2(-3f, -2f));
        }
        if (portrait != null)
        {
            GameObject portraitObject = new GameObject("Portrait | " + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            portraitObject.transform.SetParent(choice.transform, false);
            Image image = portraitObject.GetComponent<Image>();
            image.sprite = portrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetControlRect((RectTransform)portraitObject.transform, Vector2.zero, new Vector2(0.27f, 1f),
                new Vector2(3f, 3f), new Vector2(-3f, -3f));
        }
    }
}
