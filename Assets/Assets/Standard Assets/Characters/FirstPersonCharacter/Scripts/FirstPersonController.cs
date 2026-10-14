
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Interaction UI")]
        [SerializeField] private Image squareUI;

        [Header("UI Canvas Root")]
        [SerializeField] private GameObject uiRoot;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        [Header("Zoom Settings")]
        [SerializeField] private bool enableZoom = true;
        [SerializeField, Range(10f, 90f)] private float zoomFOV = 35f;
        [SerializeField] private float zoomSpeed = 10f;

        private float originalFOV;

        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField, Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;
        [SerializeField] private AudioClip m_JumpSound;
        [SerializeField] private AudioClip m_LandSound;

        private Camera m_Camera;
        private bool m_Jump;
        private bool suppressJumpUntilReleased = false;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;

        private IInteractable currentInteractable;
        private PlayableDirector timelineDirector;
        private PickupController pickupController;

        // Movement lock for Timeline
        public bool disableMovement = false;

        private bool IsValidFloat(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private bool IsValidVector3(Vector3 value)
        {
            return IsValidFloat(value.x) &&
                   IsValidFloat(value.y) &&
                   IsValidFloat(value.z);
        }

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;

            if (m_Camera == null)
            {
                Debug.LogError(
                    "FirstPersonController: Main Camera not found!"
                );
                enabled = false;
                return;
            }

            originalFOV = m_Camera.fieldOfView;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;

            if (!IsValidVector3(m_OriginalCameraPosition))
            {
                m_OriginalCameraPosition = Vector3.zero;
                m_Camera.transform.localPosition =
                    m_OriginalCameraPosition;
            }

            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);

            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;

            m_AudioSource = GetComponent<AudioSource>();
            pickupController = GetComponent<PickupController>();
            if (pickupController == null)
                Debug.LogWarning("FirstPersonController: Add PickupController to the player to enable carrying items.");
            m_MouseLook.Init(transform, m_Camera.transform);

            // Hide interaction square initially
            if (squareUI != null)
                squareUI.enabled = false;

            // Hide and lock cursor during gameplay
            LockCursor();

            timelineDirector = FindObjectOfType<PlayableDirector>();

            if (timelineDirector != null)
            {
                timelineDirector.played += OnTimelineStart;
                timelineDirector.stopped += OnTimelineStop;
            }
        }

        private void OnDestroy()
        {
            if (timelineDirector != null)
            {
                timelineDirector.played -= OnTimelineStart;
                timelineDirector.stopped -= OnTimelineStop;
            }
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnTimelineStart(PlayableDirector obj)
        {
            disableMovement = true;
            m_CharacterController.enabled = false;

            currentInteractable = null;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (uiRoot != null)
                uiRoot.SetActive(false);

            if (squareUI != null)
                squareUI.enabled = false;

            if (m_Camera != null)
                m_Camera.fieldOfView = originalFOV;
        }

        private void OnTimelineStop(PlayableDirector obj)
        {
            disableMovement = false;
            m_CharacterController.enabled = true;

            LockCursor();

            if (uiRoot != null)
                uiRoot.SetActive(true);

            if (squareUI != null)
                squareUI.enabled = false;
        }

        private void Update()
        {
            if (disableMovement)
            {
                m_Jump = false;
                currentInteractable = null;

                if (squareUI != null)
                    squareUI.enabled = false;

                return;
            }

            RotateView();
            HandleZoom();
            HandleInteraction();

            // A minigame can consume Space without causing a buffered jump.
            if (suppressJumpUntilReleased)
            {
                m_Jump = false;
                if (!Input.GetKey(KeyCode.Space))
                    suppressJumpUntilReleased = false;
            }
            else if (!m_Jump)
            {
                m_Jump =
                    CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded &&
                m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();

                m_MoveDir.y = 0f;
                m_Jumping = false;
            }

            if (!m_CharacterController.isGrounded &&
                !m_Jumping &&
                m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded =
                m_CharacterController.isGrounded;
        }

        // LMB interacts with objects (including the basket while carrying candy).
        // When holding an item, LMB on empty space drops it.
        private void HandleInteraction()
        {
            currentInteractable = null;

            bool holdingItem = pickupController != null &&
                               pickupController.IsHoldingItem();

            Ray ray = new Ray(
                m_Camera.transform.position,
                m_Camera.transform.forward
            );

            RaycastHit hit;
            if (Physics.Raycast(
                ray,
                out hit,
                interactionDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore))
            {
                MonoBehaviour[] behaviours =
                    hit.collider.GetComponentsInParent<MonoBehaviour>();

                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour == null || !behaviour.isActiveAndEnabled)
                        continue;

                    IInteractable interactable = behaviour as IInteractable;
                    if (interactable == null)
                        continue;

                    PickupItem pickup = behaviour as PickupItem;
                    if (pickup != null)
                    {
                        // A carried item cannot be picked up again, and
                        // a second pickup is unavailable while carrying one.
                        if (holdingItem || !pickup.canPickup || pickup.IsHeld)
                            continue;
                    }

                    currentInteractable = interactable;
                    break;
                }
            }

            if (squareUI != null)
                squareUI.enabled = currentInteractable != null;

            if (!Input.GetMouseButtonDown(0))
                return;

            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
            else if (holdingItem)
            {
                pickupController.Drop();
            }
        }

        // Hold RMB to zoom
        private void HandleZoom()
        {
            if (!enableZoom || m_Camera == null)
                return;

            bool isZooming = Input.GetMouseButton(1);

            float targetFOV =
                isZooming ? zoomFOV : originalFOV;

            m_Camera.fieldOfView = Mathf.Lerp(
                m_Camera.fieldOfView,
                targetFOV,
                Mathf.Clamp01(zoomSpeed * Time.deltaTime)
            );
        }

        private void FixedUpdate()
        {
            if (disableMovement ||
                !m_CharacterController.enabled)
                return;

            float speed;
            GetInput(out speed);

            Vector3 desiredMove =
                transform.forward * m_Input.y +
                transform.right * m_Input.x;

            RaycastHit hitInfo;

            bool hasGroundHit = Physics.SphereCast(
                transform.position,
                m_CharacterController.radius,
                Vector3.down,
                out hitInfo,
                m_CharacterController.height / 2f
            );

            if (hasGroundHit)
            {
                desiredMove = Vector3.ProjectOnPlane(
                    desiredMove,
                    hitInfo.normal
                ).normalized;
            }
            else
            {
                desiredMove = desiredMove.normalized;
            }

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();

                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir +=
                    Physics.gravity *
                    m_GravityMultiplier *
                    Time.fixedDeltaTime;
            }

            m_CollisionFlags =
                m_CharacterController.Move(
                    m_MoveDir * Time.fixedDeltaTime
                );

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
        }

        private void PlayLandingSound()
        {
            if (m_LandSound != null)
            {
                m_AudioSource.clip = m_LandSound;
                m_AudioSource.Play();
            }

            m_NextStep = m_StepCycle + 0.5f;
        }

        private void PlayJumpSound()
        {
            if (m_JumpSound != null)
            {
                m_AudioSource.clip = m_JumpSound;
                m_AudioSource.Play();
            }
        }

        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 &&
                (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle +=
                    (m_CharacterController.velocity.magnitude +
                    (speed * (m_IsWalking
                        ? 1f
                        : m_RunstepLenghten))) *
                    Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
                return;

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }

        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
                return;

            if (m_FootstepSounds == null ||
                m_FootstepSounds.Length == 0)
                return;

            if (m_FootstepSounds.Length == 1)
            {
                if (m_FootstepSounds[0] != null)
                    m_AudioSource.PlayOneShot(m_FootstepSounds[0]);

                return;
            }

            int n = Random.Range(1, m_FootstepSounds.Length);

            AudioClip selectedClip = m_FootstepSounds[n];

            if (selectedClip == null)
                return;

            m_AudioSource.clip = selectedClip;
            m_AudioSource.PlayOneShot(selectedClip);

            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = selectedClip;
        }

        private void UpdateCameraPosition(float speed)
        {
            if (!m_UseHeadBob)
                return;

            Vector3 newCameraPosition =
                m_OriginalCameraPosition;

            if (m_CharacterController.velocity.magnitude > 0 &&
                m_CharacterController.isGrounded)
            {
                float bobSpeed =
                    m_CharacterController.velocity.magnitude +
                    (speed * (m_IsWalking
                        ? 1f
                        : m_RunstepLenghten));

                if (IsValidFloat(bobSpeed) && bobSpeed > 0f)
                {
                    Vector3 bobPosition =
                        m_HeadBob.DoHeadBob(bobSpeed);

                    if (IsValidVector3(bobPosition))
                    {
                        newCameraPosition = bobPosition;
                    }
                }
            }

            float jumpOffset = m_JumpBob.Offset();

            if (IsValidFloat(jumpOffset))
            {
                newCameraPosition.y -= jumpOffset;
            }

            if (!IsValidVector3(newCameraPosition))
            {
                newCameraPosition = m_OriginalCameraPosition;
            }

            m_Camera.transform.localPosition =
                newCameraPosition;
        }

        // Instant WASD movement
        private void GetInput(out float speed)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif

            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;

            m_Input = new Vector2(horizontal, vertical);

            if (m_Input.sqrMagnitude > 1f)
                m_Input.Normalize();

            if (m_IsWalking != waswalking &&
                m_UseFovKick &&
                m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();

                StartCoroutine(
                    !m_IsWalking
                        ? m_FovKick.FOVKickUp()
                        : m_FovKick.FOVKickDown()
                );
            }
        }

        private void RotateView()
        {
            m_MouseLook.LookRotation(
                transform,
                m_Camera.transform
            );
        }

        private void OnControllerColliderHit(
            ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            if (m_CollisionFlags == CollisionFlags.Below)
                return;

            if (body == null || body.isKinematic)
                return;

            body.AddForceAtPosition(
                m_CharacterController.velocity * 0.1f,
                hit.point,
                ForceMode.Impulse
            );
        }

        // Called when entering/leaving the candy timing minigame.
        // Clear buffered jump and wait until Space has been released.
        public void SuppressJumpUntilReleased()
        {
            m_Jump = false;
            suppressJumpUntilReleased = true;
        }

        // Sensitivity slider support
        public void SetSensitivity(float value)
        {
            m_MouseLook.XSensitivity = value;
            m_MouseLook.YSensitivity = value;
        }
    }
}
