#if CMPSETUP_COMPLETE
using System;
using System.Collections;
using Cinemachine;
using UnityEngine;
using Fusion;
using StarterAssets;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace AvocadoShark
{
    public class GetPlayerCameraAndControls : NetworkBehaviour
    {
        [SerializeField] Transform playerCameraRoot;
        [SerializeField] StarterAssetsInputs AssetInputs;
        [SerializeField] PlayerInput PlayerInput;
        [SerializeField] Transform PlayerModel;
        [SerializeField] Transform InterpolationPoint;
        private Rigidbody _rigidbody;
        public bool UseMobileControls;

        private void Awake()
        {
            _rigidbody=GetComponent<Rigidbody>();
        }

        public override void Spawned()
        {
            var thirdPersonController = GetComponent<ThirdPersonController>();
            if (HasStateAuthority)
            {
                // _rigidbody.MovePosition(new Vector3(Random.Range(-7.6f, 14.2f), 0,
                //     Random.Range(-31.48f, -41.22f)));
                var virtualCamera = GameObject.Find("Player Follow Camera").GetComponent<CinemachineVirtualCamera>();
                virtualCamera.Follow = playerCameraRoot;
                virtualCamera.LookAt = playerCameraRoot;
                ConfigureFirstPersonCamera(virtualCamera);

                if (UseMobileControls)
                {
                    var mobileControls = GameObject.Find("Mobile Controls");
                    mobileControls.GetComponent<UICanvasControllerInput>().starterAssetsInputs = AssetInputs;
                    mobileControls.GetComponent<MobileDisableAutoSwitchControls>().playerInput = PlayerInput;
                }
                //thirdPersonController.enabled = true;
                StartCoroutine(EnableTpc());
                IEnumerator EnableTpc()
                {
                    transform.position = FusionConnection.Instance.UseCustomLocation
                        ? FusionConnection.Instance.CustomLocation
                        : new Vector3(Random.Range(-7.6f, 14.2f), 0, Random.Range(-31.48f, -41.22f));
                    yield return null;
                    thirdPersonController.enabled = true;
                }
            }
            else
            {
                PlayerModel.SetParent(InterpolationPoint);
            }
        }

        private void ConfigureFirstPersonCamera(CinemachineVirtualCamera virtualCamera)
        {
            var thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (thirdPersonFollow == null)
                return;

            thirdPersonFollow.Damping = Vector3.zero;
            thirdPersonFollow.ShoulderOffset = Vector3.zero;
            thirdPersonFollow.VerticalArmLength = 0f;
            thirdPersonFollow.CameraSide = 0.5f;
            thirdPersonFollow.CameraDistance = 0f;
            thirdPersonFollow.CameraCollisionFilter = 0;
        }
    }
}
#endif
