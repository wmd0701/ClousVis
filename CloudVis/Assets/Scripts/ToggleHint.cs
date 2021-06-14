using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleHint : MonoBehaviour
{
    private bool updateText = false;
    private Text ToggleHintText;
    private bool cli_on = false;
    private bool clw_on = false;
    private bool qr_on =  false;
    private bool vec_on = false;

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
            case "1": updateText = true; cli_on = !cli_on; ToggleHintText.text = "Specific Cloud Ice Content: " + getText(cli_on); break;
            case "2": updateText = true; clw_on = !clw_on; ToggleHintText.text = "Specific Cloud Water Content: " + getText(clw_on); break;
            case "3": updateText = true; qr_on  = !qr_on ; ToggleHintText.text = "Rain Mixing Ratio: " + getText(qr_on); break;
            case "4": updateText = true; vec_on = !vec_on; ToggleHintText.text = "Wind (vector field): " + getText(vec_on); break;
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

    string getText(bool b) {
        return b ? "On" : "Off";
    }
}
