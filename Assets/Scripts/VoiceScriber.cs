using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class VoiceScriber : MonoBehaviour
{
    public TestAPIHit gpt3;
    public TextMeshProUGUI textMesh;
    

    /// <summary>
    /// Using Google Streaming Recognizer component, the callback for "OnFinalResult"
    /// will invoke this method which feeds input to GPT3 API.
    /// </summary>
    /// <param name="youSaid"></param>
    public void OnFinalResult(string youSaid) {
        return; // NOTE: For debugging purposes
        textMesh.text = "you said: " + youSaid;// + "\n\nPress submit button if that's right";
        gpt3.question = youSaid;
        gpt3.SendRequestVoice();
    }

}
