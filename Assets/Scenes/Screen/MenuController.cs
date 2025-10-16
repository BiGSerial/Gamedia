using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject menuOptions, rawImage, BackImage;
    private Animator animatorRawImage;
    private bool hasStarted;

    public CanvasGroup fade;

    void Start()
    {
        rawImage.SetActive(false);
        animatorRawImage = rawImage.GetComponent<Animator>();
        menuOptions.SetActive(false);
        if (BackImage != null)
        {
            BackImage.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (hasStarted || !Input.anyKeyDown) return;

        StartCoroutine(PlayFadeIn());
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadLevelCo("Level1"));
    }

     IEnumerator LoadLevelCo(string sceneName)
    {
        if (fade != null)
        {
            // Fade-in rápido
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime)
            {
                fade.alpha = t;
                yield return null;
            }
            fade.alpha = 1f;
        }

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private IEnumerator PlayFadeIn()
    {
        hasStarted = true;
        rawImage.SetActive(true);
        animatorRawImage.SetTrigger("FadeIn");
        menuOptions.SetActive(true);

        // wait one frame so animator switches to the fade state
        yield return null;

        var stateInfo = animatorRawImage.GetCurrentAnimatorStateInfo(0);
        var stateHash = stateInfo.shortNameHash;
        if (stateInfo.loop)
        {
            // force the overlay off after a single pass if the clip is marked as looping
            yield return new WaitForSeconds(stateInfo.length / Mathf.Max(animatorRawImage.speed, 0.0001f));
        }
        else
        {
            yield return new WaitForSeconds(stateInfo.length);
        }

        // lock animator on the final frame so the fade stays fully opaque
        animatorRawImage.Play(stateHash, 0, 1f);
        animatorRawImage.speed = 0f;
        if (BackImage != null)
        {
            BackImage.SetActive(true);
        }
    }
}
