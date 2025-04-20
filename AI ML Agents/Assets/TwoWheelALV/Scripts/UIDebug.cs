using TMPro;
using UnityEngine;

public class UIDebug : MonoBehaviour
{
    public CarAgent car;

    public TMP_Text rawInputText;
    public TMP_Text velocityText;

    private Vector2 rawInput = Vector2.zero;
    private Vector3 velocity = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rawInputText.text = "Raw Input: " + rawInput.ToString();
        velocityText.text = "Velocity: " + velocity.ToString();
    }

    public void SetDebugValues(Vector2 rawInput, Vector3 velocity)
    {
        this.rawInput = rawInput;
        this.velocity = velocity;
    }
}
