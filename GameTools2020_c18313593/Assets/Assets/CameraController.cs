using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Camera Settings")]
    public float xSensitivity = 2f;
    public float ySensitivity = 2f;
    public float cameraHeight = 0.2f;
    public float fieldOfView = 10f;
    public bool reverseXAxis;
    public bool reverseYAxis;

    [Header("Zoom")]
    public bool enableZoom = true;
    public float zoomSpeed = 2f;

    [Header("Limits")]
    public float verticalLimitMin = 75f;
    public float verticalLimitMax = -70f;
    public float zoomLimitMin = 4f;
    public float zoomLimitMax = 14f;

    [Header("Smoothing")]
    public float rotationSmoothing = 1f;
    public float smoothing = 20f;
    public float zoomSmoothing = 100f;

    public Vector2 Axes { get; private set; }
    public Vector2 CurrentRotations { get; private set; }

    void OnEnable() => SetCursorState(false);

    public static void SetCursorState(bool state)
    {
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;
    }

    void Update()
    {
        Axes = new Vector2(Input.GetAxis("Mouse X") * xSensitivity, Input.GetAxis("Mouse Y") * ySensitivity);
        CurrentRotations = new Vector2(Mathf.Lerp(CurrentRotations.x, CurrentRotations.x + (!reverseXAxis ? Axes.x : -Axes.x) * xSensitivity, rotationSmoothing * Time.deltaTime),
            Mathf.Clamp(Mathf.Lerp(CurrentRotations.y, CurrentRotations.y + (!reverseYAxis ? Axes.y : -Axes.y) * ySensitivity, rotationSmoothing * Time.deltaTime), verticalLimitMin, verticalLimitMax));

        if (!enableZoom) return;
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        if (mouseWheel < 0f)
            fieldOfView = Mathf.MoveTowards(fieldOfView, fieldOfView + zoomSpeed, zoomSmoothing * Time.deltaTime);
        else if(mouseWheel > 0f)
            fieldOfView = Mathf.MoveTowards(fieldOfView, fieldOfView - zoomSpeed, zoomSmoothing * Time.deltaTime);
        fieldOfView = Mathf.Clamp(fieldOfView, zoomLimitMin, zoomLimitMax);
    }

    void LateUpdate()
    {
        if (!target) return;
        Vector3 targetPosition = new Vector3(target.transform.position.x, target.transform.position.y + cameraHeight, target.transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition + Quaternion.Euler(CurrentRotations.y, CurrentRotations.x, 0f) * (Vector3.forward * -fieldOfView), smoothing * Time.deltaTime);
        transform.LookAt(targetPosition);
    }
}
