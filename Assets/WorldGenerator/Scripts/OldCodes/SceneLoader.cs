using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Constants;
using static Utils;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] Image blackScreen;

    // Start is called before the first frame update
    void Start()
    {
        blackScreen.gameObject.SetActive(true);
        blackScreen.DOColor(Color.clear, 3*TWEEN_DURATION).SetEase(Ease.InExpo);
        StartCoroutine(DelayedInvoke(() => blackScreen.gameObject.SetActive(false), 3*TWEEN_DURATION));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
}
