using OpenAI_API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBotController : MonoBehaviour {
    public AzureVoiceGenerator azureVoice;
    public OpenAIAPI api;
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
        string jsonString = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "services_config.json"));
        ConfigData configData = JsonUtility.FromJson<ConfigData>(jsonString);
        if (configData == null) Debug.LogError("Failed to find configuration file. Please add in Streaming Assets.");
        
        api = new OpenAIAPI(new APIAuthentication(configData.OpenAI_APIKey)); // create object manually
        azureVoice.SetServiceConfiguration(configData.AzureVoiceSubscriptionKey, configData.AzureVoiceRegion);

        chatbotPersonalities = new Dictionary<string, ChatbotPersonalityProfile>();
        foreach (ChatbotPersonalityProfile chatbotPersonality in personalityProfiles) {
            chatbotPersonalities.Add(chatbotPersonality.PersonalityName, chatbotPersonality);
        }
        if (setPersonalityProfileName == "") setPersonalityProfileName = personalityProfiles[0].PersonalityName;        
        //StreamCompletionAsync(CompletionRequest request, Action < CompletionResult > resultHandler);        
        //var result = Task.Run(Req);   // Get the Unity synchronization context

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


    async Task<CompletionResult> CreateAPIRequestOpenAI() {
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
        textDebugger.text = "you said: " + youSaid;
        question = youSaid;
        SendRequest();
    }

    /*private async Task StreamCompletionAsync(CompletionRequest completionRequest, object request, bool v) {
        await api.Completions.StreamCompletionAsync(
            new CompletionRequest("My name is Roger and I am a principal software engineer at Salesforce.  This is my resume:", 200, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1),
            res => textDebugger.text += res.ToString());
    }*/



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
    public ChatbotPersonalityProfile(string name, string personality) {
        PersonalityName = name;
        PersonalityDescription = personality;
    }
}

[System.Serializable]
public class ConfigData {
    public string OpenAI_APIKey;
    public string AzureVoiceSubscriptionKey;
    public string AzureVoiceRegion;
}