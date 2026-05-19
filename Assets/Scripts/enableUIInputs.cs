using UnityEngine;
using UnityEngine.InputSystem;

public class enableUIInputs : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActionAsset;
    void OnEnable()
    {
        _inputActionAsset.FindActionMap("UI").Enable();
    }

    void OnDisable()
    {
        _inputActionAsset.FindActionMap("UI").Disable();
    }
}
