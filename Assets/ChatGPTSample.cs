using Google.Protobuf.WellKnownTypes;
using Oculus.Platform;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using static Google.Apis.Requests.BatchRequest;

public class ChatGPTSample : MonoBehaviour
{
    public AzureVoiceGenerator azureVoice;

    public ChatBotController ChatBotController;
    public string input;
    public OpenAIAPI api;
    public OpenAI_RequestConfiguration requestConfiguration;

    private string lastChatResponse;
    protected string chatResponse;
    //public CompletionResult res;
    // Start is called before the first frame update
    void Start()
    {
        lastChatResponse = "-1";
        // Read configuration data from file
        string jsonString = File.ReadAllText(Path.Combine(UnityEngine.Application.streamingAssetsPath, "services_config.json"));
        ConfigData configData = JsonUtility.FromJson<ConfigData>(jsonString);
        if (configData == null) Debug.LogError("Failed to find configuration file. Please add in Streaming Assets.");

        // Set up APIs using configuration data
        api = new OpenAIAPI(new APIAuthentication(configData.OpenAI_APIKey));
        azureVoice.SetServiceConfiguration(configData.AzureVoiceSubscriptionKey, configData.AzureVoiceRegion);
        //var ConversationReqTask = Task.Run(() => CreateConversationRequest().Wait());
        //var CompletionReqTask = Task.Run(() => CompletionReqGPT4().Wait());


        CreateContinuousConversation();
        var ConversationTask = Task.Run(() => DebugConvo().Wait());
    }

    
    // Update is called once per frame
    void Update()
    {
        if ((chatResponse != "" && chatResponse != lastChatResponse)) {
            //_ = CreateConversationRequest();
            //var ConversationTask = Task.Run(() => DebugConvo().Wait());
            azureVoice.inputField.text = chatResponse;
            azureVoice.InvokeAzureVoiceRequest();
            lastChatResponse = chatResponse;
        } else if (Input.GetKeyUp(KeyCode.N)) SubmitConvoReq();
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
        input = youSaid;
        SubmitConvoReq();
    }

    public void SubmitConvoReq() {
        Debug.Log("Sending msg to openai server");
        var CompletionReqTask = Task.Run(() => AppendConversation(input).Wait());
    }

    async Task CreateConversationRequest() {
        var results = api.Chat.CreateChatCompletionAsync("Hello! what is the best time of the year for these lands in your opinion?");
        Debug.Log($"res {results.Result.ToString()}");        
    }

    private Conversation chat;
    void CreateContinuousConversation() {
        chat = api.Chat.CreateConversation();
        /// give instruction as System
        //chat.AppendSystemMessage("You are a AI system that understands all of human knowledge.  If the asks you a question, please answer intelligently and fully.");        
        chat.AppendSystemMessage("You are a medieval sorcerer that knows all knowledge in a land known as Eckalu.  For any question the user rasks you, please answer intelligently and fully about your knowledge of Eckalu");
    }
    
    async Task DebugConvo() {
        // now let's ask it a question'
        chat.AppendUserInput("What is it about Eckalu that makes it to mystical?");
        // and get the response
        chatResponse = await chat.GetResponseFromChatbotAsync();
        Debug.Log(chatResponse); // "Yes"
        // Set text for Azure Voice API to speak        
    }


    // and continue the conversation by asking another
    async Task AppendConversation(string msg) {
        chat.AppendUserInput(msg);
        // and get another response
        chatResponse = await chat.GetResponseFromChatbotAsync();
        Debug.Log(chatResponse); // "No"
    }

    

    protected void GetChatHistory() {
        // the entire chat history is available in chat.Messages
        foreach (ChatMessage msg in chat.Messages) {
            Debug.Log($"{msg.Role}: {msg.Content}");
        }
    }

    async Task CompletionReq() {
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
