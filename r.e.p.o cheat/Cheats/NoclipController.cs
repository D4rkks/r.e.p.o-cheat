using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using System.Collections;

public static class NoclipController
{
    public static bool noclipActive = false;

    private static float previousGravityValue;
    private static object playerControllerInstance;
    private static System.Type playerControllerType = System.Type.GetType("PlayerController, Assembly-CSharp");

    private static InputAction jumpAction;
    private static InputAction crouchAction;
    private static InputAction sprintAction;

    private static FieldInfo rbField;
    private static FieldInfo customGravityField;
    private static MethodInfo antiGravityMethod;

    private static void InitializePlayerController()
    {
        if (playerControllerType == null)
        {
            Debug.LogError("PlayerController type not found.");
            return;
        }

        if (playerControllerInstance == null)
        {
            playerControllerInstance = GameObject.FindObjectOfType(playerControllerType);
            if (playerControllerInstance == null)
            {
                Debug.LogError("PlayerController instance not found in current scene.");
                return;
            }
            // store the field links
            rbField = playerControllerType.GetField("rb", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            customGravityField = playerControllerType.GetField("CustomGravity", BindingFlags.Public | BindingFlags.Instance);
            antiGravityMethod = playerControllerType.GetMethod("AntiGravity", BindingFlags.Public | BindingFlags.Instance);
        }
    }

    private static void InitializeInputActions()
    {
        // Get input actions to see what keybinds user has
        if (jumpAction != null && crouchAction != null && sprintAction != null)
            return;

        Type inputManagerType = typeof(InputManager);
        if (inputManagerType == null)
        {
            Debug.LogError("InputManager type not found.");
            return;
        }

        FieldInfo instanceField = inputManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceField == null)
        {
            Debug.LogError("InputManager.instance field not found.");
            return;
        }

        object inputManagerInstance = instanceField.GetValue(null);
        if (inputManagerInstance == null)
        {
            Debug.LogError("InputManager instance is null.");
            return;
        }

        FieldInfo inputActionsField = inputManagerType.GetField("inputActions", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (inputActionsField == null)
        {
            Debug.LogError("Field 'inputActions' not found in InputManager.");
            return;
        }

        object inputActionsValue = inputActionsField.GetValue(inputManagerInstance);
        if (inputActionsValue == null)
        {
            Debug.LogError("InputManager.inputActions is null.");
            return;
        }

        IDictionary actionsDict = inputActionsValue as IDictionary;
        if (actionsDict == null)
        {
            Debug.LogError("InputManager.inputActions is not a dictionary.");
            return;
        }

        foreach (DictionaryEntry entry in actionsDict)
        {
            if (entry.Value is InputAction action)
            {
                if (action.name == "Jump")
                {
                    Debug.Log("Found Jump action: " + action);
                    jumpAction = action;
                }
                else if (action.name == "Crouch")
                {
                    Debug.Log("Found Crouch action: " + action);
                    crouchAction = action;
                }
                else if (action.name == "Sprint")
                {
                    Debug.Log("Found Sprint action: " + action);
                    sprintAction = action;
                }
            }
        }
    }

    public static void ToggleNoclip()
    {
        InitializePlayerController();
        if (playerControllerInstance == null) return;

        noclipActive = !noclipActive;
        if (rbField != null)
        {
            Rigidbody rb = rbField.GetValue(playerControllerInstance) as Rigidbody;
            if (rb != null)
            {
                rb.useGravity = !noclipActive;
                rb.isKinematic = noclipActive;

                if (noclipActive)
                {
                    if (antiGravityMethod != null)
                    {
                        antiGravityMethod.Invoke(playerControllerInstance, new object[] { 100000000f });
                    }
                    else
                    {
                        Debug.Log("Method AntiGravity not found in PlayerController.");
                    }
                    rb.velocity = Vector3.zero;
                }
                else
                {
                    if (antiGravityMethod != null)
                    {
                        antiGravityMethod.Invoke(playerControllerInstance, new object[] { 0 });
                    }
                    else
                    {
                        Debug.Log("Method AntiGravity not found in PlayerController.");
                    }
                }
                foreach (var collider in ((MonoBehaviour)playerControllerInstance).GetComponentsInChildren<Collider>())
                {
                    collider.isTrigger = noclipActive;
                }
                Debug.Log($"Player noclip {(!noclipActive ? "disabled" : "enabled")}.");
            }
            else
            {
                Debug.Log("Rigidbody not found in PlayerController.");
            }
        }
        else
        {
            Debug.Log("rb field not found in PlayerController.");
        }

        if (customGravityField != null)
        {
            if (noclipActive)
            {
                previousGravityValue = (float)customGravityField.GetValue(playerControllerInstance);
                customGravityField.SetValue(playerControllerInstance, 0f);
            }
            else
            {
                float restoreGravity = previousGravityValue > 0 ? previousGravityValue : 30f;
                customGravityField.SetValue(playerControllerInstance, restoreGravity);
            }
        }
        else
        {
            Debug.Log("CustomGravity field not found in PlayerController.");
        }
    }

    public static void UpdateMovement()
    {
        try
        {
            if (!noclipActive || playerControllerInstance == null)
                return;

            if (jumpAction == null || crouchAction == null || sprintAction == null)
                InitializeInputActions();

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("Camera.main is null!");
                noclipActive = false;
                return;
            }

            Vector3 movement = Vector3.zero;
            Transform cameraTransform = cam.transform;
            Transform playerTransform = ((MonoBehaviour)playerControllerInstance).transform;
            if (playerTransform == null)
            {
                Debug.LogError("Player transform is null!");
                noclipActive = false;
                return;
            }

            // everyone use wasd, i think
            if (Input.GetKey(KeyCode.W)) movement += cameraTransform.forward;
            if (Input.GetKey(KeyCode.S)) movement -= cameraTransform.forward;
            if (Input.GetKey(KeyCode.A)) movement -= cameraTransform.right;
            if (Input.GetKey(KeyCode.D)) movement += cameraTransform.right;

            if (jumpAction != null && jumpAction.IsPressed()) movement += Vector3.up;
            if (crouchAction != null && crouchAction.IsPressed()) movement -= Vector3.up;

            float speed = (sprintAction != null && sprintAction.IsPressed()) ? 20.0f : 10.0f;
            movement = movement.normalized * speed * Time.deltaTime;
            playerTransform.position += movement;

            // remove inertia if not sprinting
            if (sprintAction != null && !sprintAction.IsPressed())
            {
                if (rbField != null)
                {
                    Rigidbody rb = rbField.GetValue(playerControllerInstance) as Rigidbody;
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                    }
                    else
                    {
                        Debug.LogError("Rigidbody (rb) is null!");
                        noclipActive = false;
                        return;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in NoclipController.UpdateMovement: " + ex.Message);
            noclipActive = false;
            return;
        }   
    }
}
