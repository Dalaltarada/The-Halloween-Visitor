using System.Collections;
using UnityEngine;
using Fungus;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>Controls the walking dialogue and arrival at the grave.
/// Fungus walking block calls WalkingDialogueFinished() as its LAST command.
/// The reveal block begins only after the dialogue AND arrival are complete.</summary>
public class GraveyardWalkSequence : MonoBehaviour
{
    [Header("Player")]
    public FirstPersonController playerController;
    public Transform[] playerWaypoints;
    public float playerSpeed = 1.8f;

    [Header("Christopher")]
    public Transform christopher;
    public Transform[] christopherWaypoints;
    public float christopherSpeed = 1.8f;
    public float christopherTurnSpeed = 300f;
    public Animation christopherAnimation;
    public string walkClip = "PumpkinKid_walk_cycle02_CH_Pumpkin_Kid";
    public string idleClip = "PumpkinKid_idle01_CH_Pumpkin_Kid";

    [Header("Grave Reveal")]
    [Tooltip("The grave Christopher turns toward. He stays in place.")]
    public Transform graveLookTarget;
    [Tooltip("Degrees per second for smooth turns toward the grave or player.")]
    public float graveLookTurnSpeed = 75f;
    [Tooltip("Adjust if Christopher's model faces backwards.")]
    public float facingOffset = 0f;

    [Header("Ghostly Disappearance")]
    [Tooltip("How many seconds Christopher takes to fade away.")]
    [Min(0.1f)] public float ghostFadeDuration = 2.5f;
    [Tooltip("Optional: disable Christopher after the fade finishes.")]
    public bool disableChristopherAfterFade = true;

    [Header("Fungus")]
    public Flowchart flowchart;
    public string walkingBlock = "Graveyard_Walking";
    public string revealBlock = "Graveyard_Reveal";
    [Tooltip("Fungus block started after Graveyard_Reveal finishes.")]
    public string graveArrivalBlock = "Graveyard_Goodbye";

    [Header("Fade UI")]
    [Tooltip("Full-screen black CanvasGroup, with alpha initially zero. Canvas should be Screen Space Overlay.")]
    public CanvasGroup blackFade;
    public float fadeDuration = 0.8f;

    [Header("Arrival")]
    public float waypointTolerance = 0.15f;
    public bool startOnAwake = true;

    private int playerIndex;
    private int christopherIndex;
    private bool walkingDialogueDone;
    private bool running;
    private bool transitionStarted;
    private bool freezeForTeleport;
    private bool revealStarted;
    private bool goodbyeStarted;
    private CharacterController characterController;
    private Coroutine graveTurnRoutine;
    private Coroutine ghostFadeRoutine;

    private void Awake()
    {
        if (playerController != null) characterController = playerController.GetComponent<CharacterController>();
        if (blackFade != null) { blackFade.alpha = 0f; blackFade.blocksRaycasts = false; }
    }

    private void Start()
    {
        if (startOnAwake) BeginSequence();
    }

    public void BeginSequence()
    {
        if (running || transitionStarted) return;
        if (playerController == null || christopher == null || !HasValidRoute(playerWaypoints) || !HasValidRoute(christopherWaypoints))
        {
            Debug.LogError("[Graveyard] Assign player, Christopher, and both waypoint arrays (final waypoint required).");
            return;
        }
        running = true;
        playerController.scriptedMovement = true;
        PlayAnimation(walkClip);
        // Only one source should start this block. Do not also add a Fungus Game Started event.
        if (flowchart != null && flowchart.FindBlock(walkingBlock) != null)
        {
            Block block = flowchart.FindBlock(walkingBlock);
            if (!block.IsExecuting()) flowchart.ExecuteBlock(walkingBlock);
        }
        else Debug.LogWarning("[Graveyard] Walking dialogue block missing.");
    }

    private static bool HasValidRoute(Transform[] route)
    {
        return route != null && route.Length > 0 && route[route.Length - 1] != null;
    }

    private void Update()
    {
        // Keep following waypoints during the fade-in-to-black.
        // Freeze only when fully black so no in-place walk is visible.
        if (!running || freezeForTeleport) return;
        MovePlayer();
        MoveChristopher();
        // Walking dialogue completion drives the transition; never start it twice.
    }

    private bool PlayerArrived { get { return playerIndex >= playerWaypoints.Length; } }
    private bool ChristopherArrived { get { return christopherIndex >= christopherWaypoints.Length; } }

    private void MovePlayer()
    {
        if (PlayerArrived) return;
        Transform target = playerWaypoints[playerIndex];
        if (target == null) { playerIndex++; return; }
        Vector3 delta = target.position - playerController.transform.position;
        delta.y = 0f;
        float step = playerSpeed * Time.deltaTime;
        if (delta.magnitude <= Mathf.Max(waypointTolerance, step))
        {
            // Use CharacterController movement to preserve collision checks.
            MovePlayerDelta(delta);
            playerIndex++;
        }
        else MovePlayerDelta(delta.normalized * step);
    }

    private void MovePlayerDelta(Vector3 movement)
    {
        if (characterController != null && characterController.enabled)
        {
            movement += Vector3.down * 2f * Time.deltaTime;
            characterController.Move(movement);
        }
        else playerController.transform.position += movement;
    }

    private void MoveChristopher()
    {
        if (ChristopherArrived) { PlayAnimation(idleClip); return; }
        Transform target = christopherWaypoints[christopherIndex];
        if (target == null) { christopherIndex++; return; }
        Vector3 delta = target.position - christopher.position;
        delta.y = 0f;
        float step = christopherSpeed * Time.deltaTime;
        if (delta.magnitude <= Mathf.Max(waypointTolerance, step))
        {
            christopher.position += delta;
            christopherIndex++;
            if (ChristopherArrived) PlayAnimation(idleClip);
        }
        else
        {
            christopher.position += delta.normalized * step;
            RotateChristopher(delta, christopherTurnSpeed);
            PlayAnimation(walkClip);
        }
    }

    private string currentAnimation;
    private void PlayAnimation(string clip)
    {
        if (christopherAnimation == null || string.IsNullOrEmpty(clip) || currentAnimation == clip) return;
        if (christopherAnimation.GetClip(clip) == null) return;
        christopherAnimation.Play(clip);
        currentAnimation = clip;
    }

    // Add this as the last Invoke Method in the walking Fungus block.
    public void WalkingDialogueFinished()
    {
        if (walkingDialogueDone) return;
        walkingDialogueDone = true;
        if (running && !transitionStarted)
            StartCoroutine(CompleteWalk());
    }

    private IEnumerator CompleteWalk()
    {
        if (transitionStarted) yield break;
        transitionStarted = true;
        // Fungus Invoke Method runs while the walking block is still executing.
        // Wait until it has actually ended before launching the next block.
        yield return WaitForBlockToEnd(walkingBlock);

        // Always fade after the walking dialogue, even if both characters arrived early.
        if (blackFade != null)
        {
            yield return FadeTo(1f);
            freezeForTeleport = true;
            if (characterController != null) characterController.enabled = false;
            playerController.transform.position = playerWaypoints[playerWaypoints.Length - 1].position;
            if (characterController != null) characterController.enabled = true;
            christopher.position = christopherWaypoints[christopherWaypoints.Length - 1].position;
            playerIndex = playerWaypoints.Length;
            christopherIndex = christopherWaypoints.Length;
            PlayAnimation(idleClip);
            yield return FadeTo(0f);
        }
        else
        {
            Debug.LogWarning("[Graveyard] Assign Black Fade for the transition. Waiting for both arrivals.");
            while (!PlayerArrived || !ChristopherArrived)
            {
                MovePlayer();
                MoveChristopher();
                yield return null;
            }
        }

        running = false;
        PlayAnimation(idleClip);
        if (!revealStarted && flowchart != null && flowchart.FindBlock(revealBlock) != null)
        {
            revealStarted = true;
            Block block = flowchart.FindBlock(revealBlock);
            if (!block.IsExecuting()) flowchart.ExecuteBlock(revealBlock);
        }
        else if (!revealStarted) Debug.LogWarning("[Graveyard] Reveal dialogue block missing.");
    }

    private IEnumerator WaitForBlockToEnd(string blockName)
    {
        if (flowchart == null) yield break;
        Block block = flowchart.FindBlock(blockName);
        if (block == null) yield break;
        while (block.IsExecuting()) yield return null;
        // Let Fungus finish its end-of-frame cleanup.
        yield return null;
    }

    private IEnumerator FadeTo(float target)
    {
        float initial = blackFade.alpha;
        float elapsed = 0f;
        blackFade.blocksRaycasts = true;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            blackFade.alpha = Mathf.Lerp(initial, target, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeDuration)));
            yield return null;
        }
        blackFade.alpha = target;
        blackFade.blocksRaycasts = target > 0.99f;
    }

    // Fungus: after the walking dialogue has ended and the reveal begins.
    // Make Christopher turn toward the grave before "Um... why are we here?"
    public void LookAtGrave()
    {
        if (graveLookTarget == null) { Debug.LogWarning("[Graveyard] Assign Grave Look Target."); return; }
        StartTurn(graveLookTarget.position, graveLookTurnSpeed);
    }

    // Place as the LAST Invoke Method in Graveyard_Reveal.
    // Starts Graveyard_Goodbye only after Fungus has ended the reveal block.
    public void RevealDialogueFinished()
    {
        if (goodbyeStarted) return;
        goodbyeStarted = true;
        StartCoroutine(StartGoodbyeAfterReveal());
    }

    private IEnumerator StartGoodbyeAfterReveal()
    {
        yield return WaitForBlockToEnd(revealBlock);
        if (flowchart == null || string.IsNullOrEmpty(graveArrivalBlock)) yield break;
        Block block = flowchart.FindBlock(graveArrivalBlock);
        if (block == null)
        {
            Debug.LogWarning("[Graveyard] Goodbye block missing: " + graveArrivalBlock);
            yield break;
        }
        if (!block.IsExecuting()) flowchart.ExecuteBlock(graveArrivalBlock);
    }

    // Fungus: after "Christopher, that's not scary..."
    public void LookAtPlayer()
    {
        if (playerController == null || christopher == null) return;
        StartTurn(playerController.transform.position, graveLookTurnSpeed);
    }

    private void StartTurn(Vector3 target, float speed)
    {
        if (christopher == null) return;
        if (graveTurnRoutine != null) StopCoroutine(graveTurnRoutine);
        graveTurnRoutine = StartCoroutine(TurnRoutine(target, speed));
    }

    private IEnumerator TurnRoutine(Vector3 target, float speed)
    {
        // Rotate only around world Y. This preserves any X/Z model tilt and
        // avoids snapping the model to a new upright orientation.
        Vector3 delta = target - christopher.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.001f)
        {
            graveTurnRoutine = null;
            yield break;
        }
        float targetYaw = Quaternion.LookRotation(delta).eulerAngles.y + facingOffset;
        while (christopher != null)
        {
            float currentYaw = christopher.eulerAngles.y;
            float remaining = Mathf.DeltaAngle(currentYaw, targetYaw);
            if (Mathf.Abs(remaining) <= 0.2f) break;
            float step = Mathf.MoveTowards(0f, remaining, Mathf.Max(1f, speed) * Time.deltaTime);
            christopher.rotation = Quaternion.AngleAxis(step, Vector3.up) * christopher.rotation;
            yield return null;
        }
        graveTurnRoutine = null;
    }

    private void RotateChristopher(Vector3 delta, float speed)
    {
        if (delta.sqrMagnitude < 0.001f) return;
        Quaternion rotation = Quaternion.LookRotation(delta) * Quaternion.Euler(0f, facingOffset, 0f);
        christopher.rotation = Quaternion.RotateTowards(christopher.rotation, rotation, speed * Time.deltaTime);
    }

    // Fungus: call immediately after Christopher says "Happy Halloween!"
    public void HideChristopher()
    {
        if (christopher == null || !christopher.gameObject.activeInHierarchy) return;
        if (ghostFadeRoutine != null) return;
        if (graveTurnRoutine != null) { StopCoroutine(graveTurnRoutine); graveTurnRoutine = null; }
        PlayAnimation(idleClip);
        ghostFadeRoutine = StartCoroutine(FadeChristopher());
    }

    private IEnumerator FadeChristopher()
    {
        // Renderer.materials creates unique material copies, so no other objects
        // sharing Christopher's original materials are affected.
        Renderer[] renderers = christopher.GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Material> fadeMaterials =
            new System.Collections.Generic.List<Material>();
        System.Collections.Generic.List<Color> originalColors =
            new System.Collections.Generic.List<Color>();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                if (material == null) continue;
                // Built-in Standard shader needs Fade rendering mode before alpha works.
                if (material.shader != null && material.shader.name == "Standard")
                {
                    material.SetFloat("_Mode", 2f);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                else
                {
                    // Built-in opaque/legacy shaders usually ignore alpha.
                    // Switch the instance to a transparent shader while preserving
                    // its main texture and tint.
                    Shader transparent = Shader.Find("Legacy Shaders/Transparent/Diffuse");
                    if (transparent != null)
                    {
                        Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                        Color tint = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                        material.shader = transparent;
                        if (mainTexture != null) material.SetTexture("_MainTex", mainTexture);
                        material.color = tint;
                    }
                    else Debug.LogWarning("[Graveyard] Transparent shader unavailable: " + material.name);
                }
                if (!material.HasProperty("_Color")) continue;
                fadeMaterials.Add(material);
                originalColors.Add(material.color);
            }
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, ghostFadeDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < fadeMaterials.Count; i++)
            {
                if (fadeMaterials[i] == null) continue;
                Color color = originalColors[i];
                color.a *= alpha;
                fadeMaterials[i].color = color;
            }
            yield return null;
        }

        if (disableChristopherAfterFade && christopher != null)
            christopher.gameObject.SetActive(false);
        ghostFadeRoutine = null;
    }

    public void ReleasePlayer()
    {
        if (playerController != null) playerController.scriptedMovement = false;
    }
}
