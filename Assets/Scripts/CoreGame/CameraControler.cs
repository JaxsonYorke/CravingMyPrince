using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(Camera))]
public class CameraControler : MonoBehaviour
{
    public GameObject Monster;
    public GameObject Princess;
    private Transform princessTransform;
    private Transform monsterTransform;
    private Transform cameraTransform;
    private Camera mainCamera;

    public float MOUSEOFFPUTSTRENGTH = 0.3f;
    
    public InputActionAsset _InputSystem;
    private InputAction m_lookValue;

    // private void OnEnable()
    // {
    //     _InputSystem.FindActionMap("Player").Enable();
    // }

    // private void OnDisable()
    // {
    //     _InputSystem.FindActionMap("Player").Disable();
    // }
    public void Awake()
    {
        m_lookValue = _InputSystem.FindAction("Look");
        
        mainCamera = GetComponent<Camera>();
        cameraTransform = GetComponent<Transform>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        princessTransform = Princess.GetComponent<Transform>();
        monsterTransform = Monster.GetComponent<Transform>();
    }

    // Update is called once per frame after all Update functions have been called
    // This is to make sure the camera moves after the characters have moved
    void LateUpdate()
{
    if (!Princess || !Monster || !mainCamera) return;

    Vector3 midpoint = new Vector3(
        (princessTransform.position.x + monsterTransform.position.x) / 2f,
        (princessTransform.position.y + monsterTransform.position.y) / 2f,
        mainCamera.transform.position.z
    );

    Vector2 screenPos = m_lookValue.ReadValue<Vector2>();

    Vector3 viewportMouse = mainCamera.ScreenToViewportPoint(screenPos);
    viewportMouse.x = Mathf.Clamp01(viewportMouse.x);
    viewportMouse.y = Mathf.Clamp01(viewportMouse.y);

    Vector2 mouseFromCenter = new Vector2(
        viewportMouse.x - 0.5f,
        viewportMouse.y - 0.5f
    );

    float halfHeight = mainCamera.orthographicSize;
    float halfWidth = halfHeight * mainCamera.aspect;

    Vector3 mouseOffset =
        new Vector3(
            mouseFromCenter.x * 2f * halfWidth,
            mouseFromCenter.y * 2f * halfHeight,
            0f
        ) * MOUSEOFFPUTSTRENGTH;

    cameraTransform.position =
        midpoint + Vector3.ClampMagnitude(mouseOffset, halfWidth);
}
}
