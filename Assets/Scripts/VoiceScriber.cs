using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class VoiceScriber : MonoBehaviour
{
    public TestAPIHit gpt3;
    public TextMeshProUGUI textMesh;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnFinalResult(string youSaid) {
        textMesh.text = "you said: " + youSaid;// + "\n\nPress submit button if that's right";
        gpt3.question = youSaid;
        gpt3.SendRequestVoice();
    }

}
