using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using static Utils;

[RequireComponent(typeof(CameraController))]
public class TerrainController : MonoBehaviour
{
    [SerializeField]
    GameObject arrow;

    [SerializeField]
    RectTransform uiBar;

    [SerializeField]
    int arrowOffset = 1200;

    bool isUIBarOn = false;

    public bool IsUIBarOn { get => isUIBarOn; private set
        {
            isUIBarOn = value;

            if (isUIBarOn == false)
                uiBar.transform.DOScaleY(0, TWEEN_DURATION).SetEase(TWEEN_EASE);
            else
                uiBar.transform.DOScaleY(1, TWEEN_DURATION).SetEase(TWEEN_EASE);
        }
    }

    CameraController cameraController = null;  
    GameObject currentTerrainObj = null;
    Coroutine arrowMovement_Corou = null;



    void OnEnable()
    {
        if (cameraController == null)
            cameraController = GetComponent<CameraController>();

        cameraController.OnCameraFocus += OnCameraFocus;

        if (arrowMovement_Corou == null)
            arrowMovement_Corou = StartCoroutine(AnimateFloatingObj(arrow, 10));

        isUIBarOn = uiBar.gameObject.activeSelf;
    }

    private void OnDisable()
    {
        if (cameraController != null)
            cameraController.OnCameraFocus -= OnCameraFocus;

        StopCoroutine(arrowMovement_Corou);
        arrowMovement_Corou = null;
    }

    private void OnCameraFocus()
    {
        currentTerrainObj = cameraController.FocusObject;

        TerrainData terrainData = currentTerrainObj.GetComponent<Terrain>().terrainData;

        Vector3 offset = new Vector3(terrainData.size.x / 2, arrowOffset + terrainData.size.y, terrainData.size.z / 2);

        //Move the arrow to the point above the terrain
        arrow.transform.position = currentTerrainObj.transform.position + offset;
    }



    // Update is called once per frame
    void Update()
    {
        //Toggle the UI Bar
        if (Input.GetKeyDown(KeyCode.Tab))
            IsUIBarOn = !IsUIBarOn;

        //Number key handling
        if (Input.GetKeyDown(KeyCode.Alpha1))
            Perlin();
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            PeakAndPits();
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            Smooth();
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            Erode();
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            Blend();
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            ResetTerrain();
    }

    public void Perlin()
    {
        currentTerrainObj.GetComponent<TerrainBase>().PerlinNoise();
    }

    public void PeakAndPits()
    {
        currentTerrainObj.GetComponent<TerrainBase>().Voronoi();
    }

    public void Smooth()
    {
        currentTerrainObj.GetComponent<TerrainBase>().Smooth();
    }

    public void Erode()
    {
        currentTerrainObj.GetComponent<TerrainErosion>().Erode();
    }

    public void Blend()
    {
        GetComponent<TerrainBlender>().Blend();
    }
    
    public void ResetTerrain()
    {
        currentTerrainObj.GetComponent<TerrainBase>().ResetHeight();
    }
}
