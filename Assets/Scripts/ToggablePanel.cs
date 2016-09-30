using UnityEngine;
using System.Collections;

public class ToggablePanel : MonoBehaviour {

    public void Awake() {
        gameObject.SetActive(false);
    }

    public void ToggleActive() {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
