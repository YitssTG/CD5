#if CMPSETUP_COMPLETE
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [SerializeField] private bool isPlayerWritingChat = false;
        private bool localControl;
        private bool gameplayBlocked = true;
        public bool IsBlocked => !localControl || gameplayBlocked || isPlayerWritingChat;

        public void SetLocalControl(bool value) { localControl = value; ClearInput(); }
        public void SetGameplayBlocked(bool value)
        {
            if (gameplayBlocked == value) return;
            gameplayBlocked = value;
            if (IsBlocked) ClearInput();
            Debug.Log($"[LocalControl] gameplayBlocked={value} chatBlocked={isPlayerWritingChat} local={localControl}");
        }
        private void ClearInput()
        {
            move = look = Vector2.zero;
            jump = sprint = interact = pushToTalk = false;
        }
        public InputActionReference PushToTalkAction, moveAction, lookAction, jumpAction;
        public InputActionReference interactAction;

        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Interaction")]
        public bool interact;
        public event Action OnInteractPressed;

        [Header("Movement Settings")]
        public bool analogMovement;
        [Header("Voice Input")]
        public bool pushToTalk;
        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value) { MoveInput(value.Get<Vector2>()); }
        public void OnLook(InputValue value) { if (cursorInputForLook) LookInput(value.Get<Vector2>()); }
        public void OnJump(InputValue value) { JumpInput(value.isPressed); }
        public void OnSprint(InputValue value) { SprintInput(value.isPressed); }
        public void OnCursorState(InputValue value)
        {
            if (IsBlocked) return;
            if (Cursor.lockState is CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        private void EnablePushToTalk(InputAction.CallbackContext context)
        {
            if (IsBlocked) return;
            pushToTalk = true;
        }
        private void DisablePushToTalk(InputAction.CallbackContext context)
        {
            if (!localControl) return;
            pushToTalk = false;
        }
        private void EnableInteract(InputAction.CallbackContext context)
        {
            if (IsBlocked) return;
            Debug.Log("🎮 EnableInteract llamado!");
            interact = true;
            OnInteractPressed?.Invoke(); // dispara inmediatamente
        }
#endif
        public void MoveInput(Vector2 newMoveDirection) { if (IsBlocked) return; move = newMoveDirection; }
        public void LookInput(Vector2 newLookDirection) { if (IsBlocked) return; look = newLookDirection; }
        public void JumpInput(bool newJumpState) { if (IsBlocked) return; jump = newJumpState; }
        public void SprintInput(bool newSprintState) { if (IsBlocked) return; sprint = newSprintState; }
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!localControl) return;
            if (!hasFocus) { ClearInput(); return; }
            Cursor.lockState = IsBlocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsBlocked;
        }
        public void DisablePlayerInput() { isPlayerWritingChat = true; ClearInput(); }
        public void EnablePlayerInput() { isPlayerWritingChat = false; }
        private void Awake()
        {
            PushToTalkAction.action.Enable();
            PushToTalkAction.action.performed += EnablePushToTalk;
            PushToTalkAction.action.canceled += DisablePushToTalk;
            interactAction.action.Enable();
            interactAction.action.performed += EnableInteract;
        }

        private void OnDestroy()
        {
            // The actions belong to a shared asset; only remove this player's listeners.
            PushToTalkAction.action.performed -= EnablePushToTalk;
            PushToTalkAction.action.canceled -= DisablePushToTalk;
            interactAction.action.performed -= EnableInteract;
        }
    }
}
#endif
