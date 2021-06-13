using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleHint : MonoBehaviour
{
    private bool updateText = false;
    private Text ToggleHintText;

    // Start is called before the first frame update
    void Start()
    {
        ToggleHintText = GetComponent<Text>();
        ToggleHintText.text = "Press H to view controll hint";
        StartCoroutine(UpdateTextAndFadeOut(ToggleHintText, 2.0f));
    }

    // Update is called once per frame
    void Update()
    {
        var input = Input.inputString;
        switch (input)
        {
            case "1": updateText = true; ToggleHintText.text = "Current view: Specific Cloud Ice Content"; break;
            case "2": updateText = true; ToggleHintText.text = "Current view: Specific Cloud Water Content"; break;
            case "3": updateText = true; ToggleHintText.text = "Current view: Rain Mixing Ratio"; break;
            case "4": updateText = true; ToggleHintText.text = "Current view: Isosurfaces (j/k: -/+ isovalue)"; break;
            case "5": updateText = true; ToggleHintText.text = "Current view: Wind (vector field)"; break;
            case "0": updateText = true; ToggleHintText.text = "Current view: Nothing"; break;
            default: updateText = false; break;
        }

        if (updateText)
            StartCoroutine(UpdateTextAndFadeOut(ToggleHintText, 4.0f));
    }

    public IEnumerator UpdateTextAndFadeOut(Text text, float f)
    {
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1);

        yield return new WaitForSeconds(f);

        while (text.color.a > 0.0f)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, text.color.a - Time.deltaTime);
            yield return null;
        }
    }
}
