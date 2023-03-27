using OpenAI_API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBotController : MonoBehaviour {
    public AzureVoiceGenerator azureVoice;
    public OpenAIAPI api; //= new OpenAIAPI(new APIAuthentication("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")); // create object manually
    public OpenAI_RequestConfiguration requestConfiguration;
    public CompletionResult res;
    
    
    public ChatbotPersonalityProfile[] personalityProfiles; // for editor assignment
    protected Dictionary<string, ChatbotPersonalityProfile> chatbotPersonalities;     // for accessing relevant properties within code
    public string setPersonalityProfileName;

    public TextMeshProUGUI textDebugger;
    public string question;

    protected int numQuestions;
    protected bool playAudio = false;

    void Awake() {
        api = new OpenAIAPI(new APIAuthentication("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")); // create object manually
        chatbotPersonalities = new Dictionary<string, ChatbotPersonalityProfile>();
        foreach (ChatbotPersonalityProfile chatbotPersonality in personalityProfiles) {
            chatbotPersonalities.Add(chatbotPersonality.PersonalityName, chatbotPersonality);
        }
        if (setPersonalityProfileName == "") setPersonalityProfileName = personalityProfiles[0].PersonalityName;
        //var result = await api.Completions.CreateCompletionAsync(new CompletionRequest("One Two Three One Two", temperature: 0.1));
        //StreamCompletionAsync(CompletionRequest request, Action < CompletionResult > resultHandler);
        // Get the Unity synchronization context
        //var result = Task.Run(Req);        

    }

    /// <summary>
    /// Invoked via button press
    /// </summary>
    public void SendRequest() {
        if (question == "") {
            Debug.Log("Failed to receive user prompt");
            return;
        }
        // prepend personality assignment, then proceed with question        
        question = chatbotPersonalities[setPersonalityProfileName].PersonalityDescription + "Q: " + textDebugger.text;    
        var result = Task.Run(CreateAPIRequestOpenAI);
        numQuestions++;
    }

    
    /*public void SendRequestVoice() {
        //question = textInput.text;
        if (question == "") {
            Debug.Log("Failed to receive any voice message/recording.");
            return;
        }
        if (setPersonalityProfileName.personality == ChatbotPersonalityProfile.ProfileSelection.Alirza &&
            numQuestions > 0) {
            question = "My name is Alireza and I am a graduate student studying multi-sensory mixed reality and AI at ASU. This is a paragraph summary on what I think about the following question:" + question;
        }
    }*/

    async Task<CompletionResult> CreateAPIRequestOpenAI() {
        /*if (setPersonalityProfileName.personality == ChatbotPersonalityProfile.ProfileSelection.SuperSmart) {
            res = await api.Completions.CreateCompletionAsync(new CompletionRequest(question, max_tokens: 100, top_p: 1));  // temperature: 0.1));        
        } else {
            res = await api.Completions.CreateCompletionAsync(new CompletionRequest(question, 200, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1));  // temperature: 0.1));        
        }*/
        res = await api.Completions.CreateCompletionAsync(new CompletionRequest(question, requestConfiguration.MaxTokens, requestConfiguration.Temperature,
                                                            presencePenalty : requestConfiguration.PresencePenalty, frequencyPenalty : requestConfiguration.FrequencyPenalty));
        playAudio = true;
        return res;
    }    

    private void Update() {
        if (res != null &&
            res.Completions.Count > 0 && playAudio) {   // essentially: if audio received for playback
            Debug.Log(res.Completions[0].Text); // log response from OpenAI

            azureVoice.inputField.text = res.Completions[0].Text;
            
            azureVoice.InvokeAzureVoiceRequest();
            playAudio = false;
        }
    }

    /// <summary>
    /// Using Google Streaming Recognizer component, the callback for "OnFinalResult"
    /// will invoke this method which feeds input to GPT3 API.
    /// </summary>
    /// <param name="youSaid"></param>
    public void OnFinalResult(string youSaid) {
        textDebugger.text = "you said: " + youSaid;// + "\n\nPress submit button if that's right";
        question = youSaid;
        SendRequest();
    }

    private async Task StreamCompletionAsync(CompletionRequest completionRequest, object request, bool v) {
        //await api.Completions.StreamCompletionAsync(
        //    new CompletionRequest("My name is Roger and I am a principal software engineer at Salesforce.  This is my resume:", 200, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1),
        //    res => ResumeTextbox.Text += res.ToString());
    }



}

[System.Serializable]
public class OpenAI_RequestConfiguration {
    public OpenAI_RequestConfiguration() { }
    public OpenAI_RequestConfiguration(double frequencyPenalty = 0.1, double presencePenality=0.1,  double topP = 1, double temperature = 0.5, int maxTokens = 200) {        
        FrequencyPenalty = frequencyPenalty;
        TopP = topP;
        Temperature = temperature;
        MaxTokens = maxTokens;
    }
    //public int? Logprobs { get; set; }
    //public bool Stream { get; }
    //public int? NumChoicesPerPrompt { get; set; }
    public double FrequencyPenalty;
    public double TopP;
    //public string[] MultipleStopSequences { get; set; }
    public double Temperature;
    public int MaxTokens;
    //public string Prompt;
    //public string[] MultiplePrompts { get; set; }
    //public object CompiledPrompt { get; }
    public double PresencePenalty;
    //public string StopSequence { get; set; }
}

[System.Serializable]
public class ChatbotPersonalityProfile {
    public string PersonalityName;
    public string PersonalityDescription;


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
    string Alireza = "My name is Alireza and I am a principal software engineer studying multi-sensory mixed reality and AI at ASU. This is a paragraph summary of my research experience:";
    public ChatbotPersonalityProfile(string name, string personality) {
        PersonalityName = name;
        PersonalityDescription = personality;
    }

}