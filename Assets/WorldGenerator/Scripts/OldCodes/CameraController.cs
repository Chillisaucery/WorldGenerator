using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static Constants;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    GameObject initialFocusObject = null;

    [SerializeField]
    Transform cameraTransform;

    [SerializeField]
    int rotateSpeed = 2000, moveSpeed = 1000;

    [SerializeField]
    float lookAtDistance = 1000;



    GameObject focusObject;
    public GameObject FocusObject { get => focusObject; private set
        {
            focusObject = value;
            OnFocus();
        }
    }

    public Transform CameraTransform { get => cameraTransform; private set => cameraTransform = value; }

    (Vector3 posistion, Vector3 eulerAngle) initialCameraAngle = (Vector3.zero, Vector3.zero);

    Vector3 previousMousePos = Vector3.zero;


    //Events
    public delegate void CameraControllerEvent();
    public CameraControllerEvent OnCameraFocus;

    private void Start()
    {
        initialCameraAngle = (CameraTransform.position, CameraTransform.eulerAngles);
        FocusObject = initialFocusObject;
    }



    // Update is called once per frame
    void Update()
    {
        //Click on object to be in focus
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                FocusObject = hit.collider.gameObject;
            }
        }

        //Move the camera with WASD or arrow keys
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            Vector3 forwardAxis = new Vector3(CameraTransform.forward.x, 0, CameraTransform.forward.z).normalized;
            Vector3 horizontalAxis = Vector3.Cross(CameraTransform.forward, Vector3.up).normalized;

            CameraTransform.position += (forwardAxis * Input.GetAxis("Vertical") * Time.deltaTime
                                        + -horizontalAxis * Input.GetAxis("Horizontal") * Time.deltaTime) * moveSpeed;
        }

        //Rotate the camera by holding right mouse
        if (Input.GetMouseButtonDown(1))
        {
            //Store the value to begin the rotating process
            previousMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 mouseDifference = Input.mousePosition - previousMousePos;
            Vector3 axisForVerticalRotate = Vector3.Cross(CameraTransform.forward, Vector3.up).normalized;

            //Rotate around the up axis (rotate horizontally)
            CameraTransform.RotateAround(CameraTransform.position, Vector3.up, - mouseDifference.x * Time.deltaTime * rotateSpeed);
            
            //Rotate around a horizontal axis (rotate vertically)
            CameraTransform.RotateAround(CameraTransform.position, axisForVerticalRotate, -mouseDifference.y * Time.deltaTime * rotateSpeed);

            //Lock the z axis so that it won't rotate out of control
            cameraTransform.eulerAngles = new Vector3(cameraTransform.eulerAngles.x, cameraTransform.eulerAngles.y,0);

            previousMousePos = Input.mousePosition;
        }

        //Ctrl or Space to raise or lower the camera position
        if (Input.GetKey(KeyCode.LeftControl))
        {
            CameraTransform.position -= Vector3.up * Time.deltaTime * moveSpeed;
        }
        else if (Input.GetKey(KeyCode.Space))
        {
            CameraTransform.position += Vector3.up * Time.deltaTime * moveSpeed;
        }

        //Reset camera
        if (Input.GetKeyDown(KeyCode.Backspace))
            ResetCamera();
    }

    private void OnFocus()
    {
        CameraTransform.DOLookAt(FocusObject.transform.position, TWEEN_DURATION).SetEase(TWEEN_EASE);

        float distanceToFocusObj = (FocusObject.transform.position - CameraTransform.position).magnitude;

        Vector3 newCameraPos = Vector3.LerpUnclamped(CameraTransform.position, FocusObject.transform.position,
                                (distanceToFocusObj - lookAtDistance) / distanceToFocusObj);

        CameraTransform.DOMove(newCameraPos, TWEEN_DURATION).SetEase(TWEEN_EASE);

        if (OnCameraFocus != null)
            OnCameraFocus.Invoke();
    }

    public void ResetCamera()
    {
        CameraTransform.DOMove(initialCameraAngle.posistion, TWEEN_DURATION).SetEase(TWEEN_EASE);
        CameraTransform.DORotate(initialCameraAngle.eulerAngle, TWEEN_DURATION).SetEase(TWEEN_EASE);
    }
}
