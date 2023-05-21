using GoogleCloudStreamingSpeechToText;
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
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;
using static ChatBotController;

public class ChatBotController : MonoBehaviour {
    public bool conversationStarter;

    public delegate void NPCSpeakAction(string response, ChatBotController chatbotController);
    public event NPCSpeakAction OnNPCSpeakAction;

    public delegate void PlayerSpeakAction(bool state);
    public static event PlayerSpeakAction OnPlayerSpeakAction;

    /// <summary>
    /// References to other components and APIs
    /// </summary>
    public AzureVoiceGenerator azureVoice;
    public StreamingRecognizer googleStreamingRecognizer;
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

        googleStreamingRecognizer = FindObjectOfType<StreamingRecognizer>();
        if (googleStreamingRecognizer == null) Debug.LogError("Add Streaming Recognizer to scene");
        googleStreamingRecognizer.onFinalResult.AddListener(OnFinalResult);

        // Set up dictionary of personality profiles
        chatbotPersonalities = new Dictionary<string, ChatbotPersonalityProfile>();
        foreach (ChatbotPersonalityProfile chatbotPersonality in personalityProfiles) {
            chatbotPersonalities.Add(chatbotPersonality.PersonalityName, chatbotPersonality);
        }
        if (setPersonalityProfileName == "") setPersonalityProfileName = personalityProfiles[0].PersonalityName; // Default personality profile
        if (chatMode == ChatMode.ChatGPT3_5) CreateContinuousConversation();
        Initialize();

    }

    public void Initialize() {
        if (conversationStarter) 
            GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) {
        //Debug.Log($"Collided with player {other.name}");
        if (other.gameObject.CompareTag("Player") || 
            other.gameObject.CompareTag("NPC")) {
            if (conversationStarter) {
                SubmitConversationRequest("Introduce yourself please");
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            Debug.Log("Invoking player exit");
            OnPlayerSpeakAction?.Invoke(false);
        }
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


    /// <summary>
    /// Asynchronous method to create OpenAI API request
    /// </summary>
    /// <returns>The completion result</returns>
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
        //chat.AppendSystemMessage("You are a medieval sorcerer that knows all knowledge in a land known as Eckalu.  For any question the user rasks you, please answer intelligently and fully about your knowledge of Eckalu");
        chat.AppendSystemMessage(chatbotPersonalities[setPersonalityProfileName].PersonalityDescription);
    }

    public void SubmitConvoReqChatGPT(string input) {
        Debug.Log("Sending msg to openai server");
        var CompletionReqTask = Task.Run(() => AppendConversation(input).Wait());
    }

    public void SubmitCompletionReqGPT4(string input) {
        Debug.Log("Sending msg to openai server");
        var CompletionReqTask = Task.Run(() => CompletionReqGPT4(input).Wait());
    }

    /// <summary>
    /// Append user input to the conversation and get a response
    /// </summary>
    /// <param name="msg">The user's message</param>
    /// <returns>The chat response</returns>
    async Task AppendConversation(string msg) {
        chat.AppendUserInput(msg);
        // and get another response
        chatResponse = await chat.GetResponseFromChatbotAsync();
        Debug.Log(chatResponse); 
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
                    OnNPCSpeakAction?.Invoke(chatResponse, this);
                    azureVoice.InvokeAzureVoiceRequest();
                    playAudio = false;
                    lastChatResponse = chatResponse;
                }
                break;
            case ChatMode.CompletionRequestDaVinci:
                if (res != null &&
            res.Completions.Count > 0 && playAudio) {   // essentially: if audio received for playback
                    Debug.Log(res.Completions[0].Text); // log response from OpenAI
                    OnNPCSpeakAction?.Invoke(res.Completions[0].Text, this);
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
    /// </summary>
    /// <param name="youSaid">The user's speech input</param>
    public void OnFinalResult(string youSaid) {
        if (!this.isActiveAndEnabled) return;
        Debug.Log($"{youSaid}");
        textDebugger.text = "you said: " + youSaid;
        question = youSaid;
        OnPlayerSpeakAction?.Invoke(true);
        SubmitConversationRequest(question);
    }

    public void SubmitConversationRequest(string question) {
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

    public void NPC_ChatbotResponse(string NPCInput, ChatBotController chatBot) {
        Debug.Log("NPC_ChatbotResponse");
        AudioSource chatBotAudio = chatBot.azureVoice.audioSource;
        // Start a coroutine to wait for audio completion
        chatBot.StartCoroutine(WaitForAudioCompletion(NPCInput, chatBotAudio));
    }

    private IEnumerator WaitForAudioCompletion(string NPCInput, AudioSource chatBotAudio) {
        Debug.Log("Coroutine");
        // Wait until the audio source has stopped playing
        while (chatBotAudio.isPlaying) {
            yield return null;
        }

        // Audio has completed, proceed with invoking the request
        Debug.Log("Invoking request after audio");
        SubmitConversationRequest("You are talking to another person. Please respond to what they just said, which is: " + NPCInput);
    }

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