using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class ChatBotController : MonoBehaviour {
    // Set up references to other components and APIs
    public AzureVoiceGenerator azureVoice;
    public OpenAIAPI api;
    public OpenAI_RequestConfiguration requestConfiguration;
    public CompletionResult res;

    [SerializeField]
    public ChatMode chatMode;
    [System.Serializable]
    public enum ChatMode {
        ChatGPT3_5,
        CompletionRequestDaVinci,
        CompletionResponseGPT4
    }

    public ChatbotPersonalityProfile[] personalityProfiles; // for editor assignment
    protected Dictionary<string, ChatbotPersonalityProfile> chatbotPersonalities;     // for accessing relevant properties within code
    public string setPersonalityProfileName;

    // Set up debugging and user input fields
    public TextMeshProUGUI textDebugger;
    public string question;

    protected int numQuestions;
    protected bool playAudio = false;


    private Conversation chat;
    protected string chatResponse, lastChatResponse;

    void Awake() {
        // Read configuration data from file
        string jsonString = System.IO.File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "services_config.json"));
        ConfigData configData = JsonUtility.FromJson<ConfigData>(jsonString);
        if (configData == null) Debug.LogError("Failed to find configuration file. Please add in Streaming Assets.");

        // Set up APIs using configuration data
        api = new OpenAIAPI(new APIAuthentication(configData.OpenAI_APIKey));
        azureVoice.SetServiceConfiguration(configData.AzureVoiceSubscriptionKey, configData.AzureVoiceRegion);

        // Set up dictionary of personality profiles
        chatbotPersonalities = new Dictionary<string, ChatbotPersonalityProfile>();
        foreach (ChatbotPersonalityProfile chatbotPersonality in personalityProfiles) {
            chatbotPersonalities.Add(chatbotPersonality.PersonalityName, chatbotPersonality);
        }
        if (setPersonalityProfileName == "") setPersonalityProfileName = personalityProfiles[0].PersonalityName; // Default personality profile
        if (chatMode == ChatMode.ChatGPT3_5) CreateContinuousConversation();
        //StreamCompletionAsync(CompletionRequest request, Action < CompletionResult > resultHandler);        
        //var result = Task.Run(Req);   // Get the Unity synchronization context

    }

    public void SendRequestDaVinci() {
        if (question == "") {
            Debug.Log("Failed to receive user prompt");
            return;
        }
        // prepend personality assignment, then proceed with question        
        question = chatbotPersonalities[setPersonalityProfileName].PersonalityDescription + "Q: " + textDebugger.text;    
        var result = Task.Run(CreateCompletionDaVinciAPIRequestOpenAI);
        numQuestions++;
    }


    // Asynchronous method to create OpenAI API request
    async Task<CompletionResult> CreateCompletionDaVinciAPIRequestOpenAI() {
        Debug.Log($"question {question}");
        res = await api.Completions.CreateCompletionAsync(new CompletionRequest(question, Model.DavinciText, max_tokens: requestConfiguration.MaxTokens, temperature: requestConfiguration.Temperature,
                                                            presencePenalty: requestConfiguration.PresencePenalty, frequencyPenalty: requestConfiguration.FrequencyPenalty));
        Debug.Log("Transmitted completion request");
        playAudio = true;
        return res;
    }

    void CreateContinuousConversation() {
        chat = api.Chat.CreateConversation();
        /// give instruction as System
        //chat.AppendSystemMessage("You are a AI system that understands all of human knowledge.  If the asks you a question, please answer intelligently and fully.");        
        chat.AppendSystemMessage("You are a medieval sorcerer that knows all knowledge in a land known as Eckalu.  For any question the user rasks you, please answer intelligently and fully about your knowledge of Eckalu");
    }

    public void SubmitConvoReqChatGPT(string input) {
        Debug.Log("Sending msg to openai server");
        var CompletionReqTask = Task.Run(() => AppendConversation(input).Wait());
    }

    public void SubmitCompletionReqGPT4(string input) {
        Debug.Log("Sending msg to openai server");
        var CompletionReqTask = Task.Run(() => CompletionReqGPT4(input).Wait());
    }

    // and continue the conversation by asking another
    async Task AppendConversation(string msg) {
        chat.AppendUserInput(msg);
        // and get another response
        chatResponse = await chat.GetResponseFromChatbotAsync();
        Debug.Log(chatResponse); // "No"
        //azureVoice.inputField.text = chatResponse;
        //azureVoice.InvokeAzureVoiceRequest();
    }

    protected void GetChatHistory() {
        // the entire chat history is available in chat.Messages
        foreach (ChatMessage msg in chat.Messages) {
            Debug.Log($"{msg.Role}: {msg.Content}");
        }
    }


    private void Update() {
        switch (chatMode) {
            case ChatMode.ChatGPT3_5:
                if ((chatResponse != "" && chatResponse != lastChatResponse)) {
                    //_ = CreateConversationRequest();
                    //var ConversationTask = Task.Run(() => DebugConvo().Wait());
                    azureVoice.inputField.text = chatResponse;
                    azureVoice.InvokeAzureVoiceRequest();
                    lastChatResponse = chatResponse;
                }
                break;
            case ChatMode.CompletionRequestDaVinci:
                if (res != null &&
            res.Completions.Count > 0 && playAudio) {   // essentially: if audio received for playback
                    Debug.Log(res.Completions[0].Text); // log response from OpenAI

                    // Set text for Azure Voice API to speak
                    azureVoice.inputField.text = res.Completions[0].Text;

                    azureVoice.InvokeAzureVoiceRequest();
                    playAudio = false;
                }
                break;
        }        
    }

    /// <summary>
    /// Method to handle user input from Google Cloud Speech to Text API
    /// Using Google Streaming Recognizer component, the callback for "OnFinalResult"
    /// will invoke this method which feeds input to GPT3 API.
    /// </summary>
    /// <param name="youSaid"></param>
    public void OnFinalResult(string youSaid) {
        if (!this.isActiveAndEnabled) return;
        Debug.Log($"{youSaid}");
        textDebugger.text = "you said: " + youSaid;
        question = youSaid;
        switch (chatMode) {
            case ChatMode.ChatGPT3_5:
                SubmitConvoReqChatGPT(question);
                break;
            case ChatMode.CompletionRequestDaVinci:
                SendRequestDaVinci();
                break;
            case ChatMode.CompletionResponseGPT4:
                SubmitCompletionReqGPT4(question);
                break;
        }        
    }

    /*private async Task StreamCompletionAsync(CompletionRequest completionRequest, object request, bool v) {
        await api.Completions.StreamCompletionAsync(
            new CompletionRequest("My name is Roger and I am a principal software engineer at Salesforce.  This is my resume:", 200, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1),
            res => textDebugger.text += res.ToString());
    }*/

    async Task CompletionReqGPT4(string input) {
        // TODO: Feed input with merged personality
        string preprompt = "My name is Roger and I am a principal software engineer at Salesforce. Based on my experience, I will answer your question. Q:What is the most optimal business strategy for a VR company?";
        //var res = await api.Completions.CreateCompletionAsync(new CompletionRequest(preprompt + "What is the most optimal business strategy for a VR company?", Model.GPT4, max_tokens: requestConfiguration.MaxTokens, temperature: requestConfiguration.Temperature,
        //                                                    presencePenalty: requestConfiguration.PresencePenalty, frequencyPenalty: requestConfiguration.FrequencyPenalty));

        // for example
        var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest() {
            Model = Model.GPT4,
            Temperature = 0.1,
            MaxTokens = 100,
            Messages = new ChatMessage[] {
            new ChatMessage(ChatMessageRole.User, preprompt)
        }
        });
        Debug.Log($"resul {result.ToString()}");
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