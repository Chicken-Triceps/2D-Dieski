using TMPro;
using UnityEngine;

public class CharacterAnimationViewer : MonoBehaviour
{
    private const string DefaultStateName = "Default";

    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            GameObject testDummy = GameObject.Find("TestDummy");
            if (testDummy != null)
            {
                animator = testDummy.GetComponent<Animator>();
            }
        }
    }

    private void Start()
    {
        PlayAnimation(DefaultStateName);
    }

    public void PlayAnimation(string stateName)
    {
        if (animator == null)
        {
            Debug.LogWarning("TestDummy Animator is not assigned.");
            return;
        }

        if (!TryFindState(stateName, out int layerIndex, out string statePath))
        {
            Debug.LogWarning($"Animator state '{stateName}' was not found.");
            return;
        }

        animator.Play(statePath, layerIndex, 0f);

        if (statusText != null)
        {
            statusText.text = stateName;
        }
    }

    private bool TryFindState(string stateName, out int layerIndex, out string statePath)
    {
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, Animator.StringToHash(stateName)))
            {
                layerIndex = i;
                statePath = stateName;
                return true;
            }

            string layerStatePath = $"{animator.GetLayerName(i)}.{stateName}";
            if (animator.HasState(i, Animator.StringToHash(layerStatePath)))
            {
                layerIndex = i;
                statePath = layerStatePath;
                return true;
            }
        }

        layerIndex = -1;
        statePath = string.Empty;
        return false;
    }
}
