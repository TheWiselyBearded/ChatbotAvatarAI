using OpenAI_API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TestAPIHit : MonoBehaviour {
    public PersonalityProfile personalityProfile;
    public string startingPrompt = "I am a highly intelligent question answering bot. If you ask me a question that is rooted in truth, I will give you the answer. If you ask me a question that is nonsense, trickery, or has no clear answer, I will respond with \"Unknown\". Q: What is human life expectancy in the United States?";
    public InputField textInput;
    public string question;
    public HelloWorld hello;
    OpenAIAPI api = new OpenAIAPI(new APIAuthentication("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")); // create object manually
    // Start is called before the first frame update
    CompletionResult res;
    void Start() {
        //var result = await api.Completions.CreateCompletionAsync(new CompletionRequest("One Two Three One Two", temperature: 0.1));
        //StreamCompletionAsync(CompletionRequest request, Action < CompletionResult > resultHandler);
        // Get the Unity synchronization context
        //var result = Task.Run(Req);        

    }

    /// <summary>
    /// Invoked via button press
    /// </summary>
    public void SendRequest() {
        question = textInput.text;
        if (question == "") {
            question = personalityProfile.GetPersonality();
        }
        if (personalityProfile.personality == PersonalityProfile.ProfileSelection.SuperSmart) {
            question = personalityProfile.GetPersonality() + "Q: " + textInput.text;
        }
        var result = Task.Run(Req);
    }

    async Task<CompletionResult> Req() {
        if (personalityProfile.personality == PersonalityProfile.ProfileSelection.SuperSmart) {
            res = await api.Completions.CreateCompletionAsync(new CompletionRequest(question,  max_tokens: 100, top_p: 1));  // temperature: 0.1));        
        } else {
            res = await api.Completions.CreateCompletionAsync(new CompletionRequest(question, 200, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1));  // temperature: 0.1));        
        }
        playAudio = true;
        return res;
    }

    bool playAudio = false;

    private void Update() {
        if (res != null && 
            res.Completions.Count > 0 && playAudio) {
            Debug.Log(res.Completions[0].Text);
            if (personalityProfile.personality == PersonalityProfile.ProfileSelection.SuperSmart) {
                hello.inputField.text = res.Completions[0].Text;
            } else {
                hello.inputField.text = res.Completions[0].Text;
            }
            hello.ButtonClick();
            playAudio = false;
        }
    }

    private async Task StreamCompletionAsync(CompletionRequest completionRequest, object request, bool v) {
        //await api.Completions.StreamCompletionAsync(
        //    new CompletionRequest("My name is Roger and I am a principal software engineer at Salesforce.  This is my resume:", 200, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1),
        //    res => ResumeTextbox.Text += res.ToString());
    }



}
[System.Serializable]
public class PersonalityProfile {
    [System.Serializable]
    public enum ProfileSelection {
        SuperSmart,
        Alirza,
        None
    }
    public ProfileSelection personality;
    public Dictionary<ProfileSelection, string> personalities;

    string SuperSmart = "I am a highly intelligent question answering bot. If you ask me a question that is rooted in truth, I will give you the answer. If you ask me a question that is nonsense, trickery, or has no clear answer, I will respond with \"Unknown\". " +
        "Q: What is human life expectancy in the United States?" +
        "A: Human life expectancy in the United States is 78 years." +
        "Q: Who was president of the United States in 1955?" +
        "A: Dwight D.Eisenhower was president of the United States in 1955." +
        "Q: Which party did he belong to?" +
        "A: He belonged to the Republican Party." +
        "Q: What is the square root of banana?" +
        "A: Unknown" +
        "Q: How does a telescope work?" +
        "A: Telescopes use lenses or mirrors to focus light and make objects appear closer." +
        "Q: Where were the 1992 Olympics held?" +
        "A: The 1992 Olympics were held in Barcelona, Spain." +
        "Q: How many squigs are in a bonk?" +
        "A: Unknown";
    string Alireza = "My name is Garrett Duffy and I am a principal mechanical engineer studying biomedical sciences at ASU. This is a paragraph summary of my research experiences:";
    public PersonalityProfile() {
        personalities = new Dictionary<ProfileSelection, string>();
        personalities.Add(ProfileSelection.SuperSmart, SuperSmart);
        personalities.Add(ProfileSelection.Alirza, Alireza);
        personality = ProfileSelection.SuperSmart;
    }

    public string GetPersonality() {
        if (personalities.TryGetValue(personality, out string value))
            return value;
        else
            return "";
    }
}