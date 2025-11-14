using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    public List<Gate> connectedGates;

    public GameObject normalSprite; // child with normal image
    public GameObject activeSprite; // child with active image

    // this tracks whether the switch is currently ON or OFF
    private bool isOn = false;

    private void Start()
    {
        // make sure it starts in "OFF" state
        SetVisualState(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        ToggleSwitch();
    }

    void ToggleSwitch()
    {
        // flip ON/OFF
        isOn = !isOn;

        // toggle all connected gates
        foreach (var gate in connectedGates)
        {
            if (gate != null)
                gate.Toggle();
        }

        // update sprite
        SetVisualState(isOn);
    }

    void SetVisualState(bool activated)
    {
        if (normalSprite != null)
            normalSprite.SetActive(!activated);

        if (activeSprite != null)
            activeSprite.SetActive(activated);
    }
}
