using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MultipleMatchesAdditives
{

    public class CanvasLoader : MonoBehaviour
    {
        // A 'fake' but easy way to do loading screens
        // Instantiate before anything needs to load, destroy once loaded or after X amount of time

        public Button loadingButton;

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            // optional button to close canvas loading overlay
            loadingButton.onClick.AddListener(LoadingButtonChanged);
            // just needs to be enough time to cover a scene loading in
            StartCoroutine(RemoveLoadingCanvas(0.1f));
        }

        private void LoadingButtonChanged()
        {
            Destroy(this.gameObject);
        }

        IEnumerator RemoveLoadingCanvas(float _time)
        {
            yield return new WaitForSeconds(_time);
            Destroy(this.gameObject);
        }
    }
}