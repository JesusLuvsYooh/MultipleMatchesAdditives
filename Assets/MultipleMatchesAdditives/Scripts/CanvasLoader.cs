using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultipleMatchesAdditives
{

    public class CanvasLoader : MonoBehaviour
    {
        public Button loadingButton;

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
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