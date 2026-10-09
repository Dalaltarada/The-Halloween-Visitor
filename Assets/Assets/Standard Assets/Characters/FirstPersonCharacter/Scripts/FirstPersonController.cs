
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
        [Header("UI")]
        [SerializeField] private Image squareUI;

        [Header("UI Canvas Root")]
        [SerializeField] private GameObject uiRoot;

        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField][Range(0f, 1f)] private float m_RunstepLenghten;
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
        private float m_YRotation;
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

        // Movement lock for Timeline
        public bool disableMovement = false;

        // Check whether a float is valid
        private bool IsValidFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // Check whether a Vector3 contains valid values
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

            m_OriginalCameraPosition = m_Camera.transform.localPosition;

            // Ensure the saved camera position is valid
            if (!IsValidVector3(m_OriginalCameraPosition))
            {
                m_OriginalCameraPosition = Vector3.zero;
                m_Camera.transform.localPosition = m_OriginalCameraPosition;
            }

            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);

            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;

            m_AudioSource = GetComponent<AudioSource>();

            m_MouseLook.Init(transform, m_Camera.transform);

            if (squareUI != null)
                squareUI.enabled = true;

            PlayableDirector pd = FindObjectOfType<PlayableDirector>();

            if (pd != null)
            {
                pd.played += OnTimelineStart;
                pd.stopped += OnTimelineStop;
            }
        }

        private void OnTimelineStart(PlayableDirector obj)
        {
            disableMovement = true;
            m_CharacterController.enabled = false;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (uiRoot != null)
                uiRoot.SetActive(false);

            if (squareUI != null)
                squareUI.enabled = false;
        }

        private void OnTimelineStop(PlayableDirector obj)
        {
            disableMovement = false;
            m_CharacterController.enabled = true;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (uiRoot != null)
                uiRoot.SetActive(true);

            if (squareUI != null)
                squareUI.enabled = true;
        }

        private void Update()
        {
            if (disableMovement)
            {
                return;
            }

            RotateView();

            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
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

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            if (Input.GetKeyDown(KeyCode.Q) && squareUI != null)
            {
                squareUI.enabled = !squareUI.enabled;
            }
        }

        private void FixedUpdate()
        {
            if (disableMovement)
            {
                return;
            }

            float speed;
            GetInput(out speed);

            Vector3 desiredMove =
                transform.forward * m_Input.y +
                transform.right * m_Input.x;

            RaycastHit hitInfo;

            Physics.SphereCast(
                transform.position,
                m_CharacterController.radius,
                Vector3.down,
                out hitInfo,
                m_CharacterController.height / 2f
            );

            desiredMove =
                Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

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

            m_NextStep = m_StepCycle + .5f;
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
                    (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                    Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }

        // FIX: Safe footstep audio
        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }

            if (m_FootstepSounds == null ||
                m_FootstepSounds.Length == 0)
            {
                return;
            }

            if (m_FootstepSounds.Length == 1)
            {
                if (m_FootstepSounds[0] != null)
                {
                    m_AudioSource.PlayOneShot(m_FootstepSounds[0]);
                }

                return;
            }

            int n = Random.Range(1, m_FootstepSounds.Length);

            AudioClip selectedClip = m_FootstepSounds[n];

            if (selectedClip == null)
            {
                return;
            }

            m_AudioSource.clip = selectedClip;
            m_AudioSource.PlayOneShot(selectedClip);

            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = selectedClip;
        }

        // FIX: Prevent invalid camera positions
        private void UpdateCameraPosition(float speed)
        {
            if (!m_UseHeadBob)
            {
                return;
            }

            Vector3 newCameraPosition = m_OriginalCameraPosition;

            if (m_CharacterController.velocity.magnitude > 0 &&
                m_CharacterController.isGrounded)
            {
                float bobSpeed =
                    m_CharacterController.velocity.magnitude +
                    (speed * (m_IsWalking ? 1f : m_RunstepLenghten));

                // Only calculate head bob with a valid speed
                if (IsValidFloat(bobSpeed) && bobSpeed > 0f)
                {
                    Vector3 bobPosition = m_HeadBob.DoHeadBob(bobSpeed);

                    // Validate BEFORE assigning to the camera
                    if (IsValidVector3(bobPosition))
                    {
                        newCameraPosition = bobPosition;
                    }
                    else
                    {
                        // Invalid head bob result: use safe position
                        newCameraPosition = m_OriginalCameraPosition;
                    }
                }
            }

            // Apply jump bob offset only if valid
            float jumpOffset = m_JumpBob.Offset();

            if (IsValidFloat(jumpOffset))
            {
                newCameraPosition.y -= jumpOffset;
            }

            // Final safety check
            if (!IsValidVector3(newCameraPosition))
            {
                newCameraPosition = m_OriginalCameraPosition;
            }

            // Assign only a valid position
            m_Camera.transform.localPosition = newCameraPosition;
        }

        private void GetInput(out float speed)
        {
            float horizontal =
                CrossPlatformInputManager.GetAxis("Horizontal");

            float vertical =
                CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif

            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;

            m_Input = new Vector2(horizontal, vertical);

            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

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

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }

            body.AddForceAtPosition(
                m_CharacterController.velocity * 0.1f,
                hit.point,
                ForceMode.Impulse
            );
        }

        // Public method for sensitivity adjustment
        public void SetSensitivity(float value)
        {
            m_MouseLook.XSensitivity = value;
            m_MouseLook.YSensitivity = value;
        }
    }
}
