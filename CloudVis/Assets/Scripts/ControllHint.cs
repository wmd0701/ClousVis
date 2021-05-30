using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControllHint : MonoBehaviour
{
    private Text ControllHintText;
    private string hintContent;

    // Start is called before the first frame update
    void Start()
    {
        ControllHintText = GetComponent<Text>();
        hintContent = "Camera Movement: \n" +
                        "   wasd/D-pad: basic movement\n" +
                        "   q: upward\n" + 
                        "   e: backward\n" +
                        "   ctrl/shift/mouse left: speed up\n\n" + 
                        "Toggle view: \n" +
                        "   1: Specific Cloud Water Content\n" +
                        "   2: Specific Cloud Ice Content\n" +
                        "   3: Rain Mixing Ratio\n" +
                        "   4: Air Pressure";
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("h"))
        {
            ControllHintText.text = hintContent;
            StartCoroutine(UpdateTextAndFadeOut(ControllHintText, 2.0f));
        }

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
