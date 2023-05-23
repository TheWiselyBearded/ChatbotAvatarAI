using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCGroupCommunicator : MonoBehaviour
{
    public ChatBotController[] chatbots;
    public bool PlayerInputMode;
    // Start is called before the first frame update
    void Awake()
    {
        PlayerInputMode = false;
        // subscribe all npc to one another
        for (int i = 0; i < chatbots.Length; i++) {
            for(int j = 0; j < chatbots.Length; j++) {
                if (chatbots[i] != chatbots[j]) {
                    chatbots[i].OnNPCSpeakAction += chatbots[j].NPC_ChatbotResponse;
                }
            }
        }
        ChatBotController.OnPlayerSpeakAction += SetPlayerInputMode; 
    }


    public void SetPlayerInputMode(bool state) {
        PlayerInputMode = state;
        Debug.Log($"Player input mode set to {state}");
        if (PlayerInputMode) {
            for (int i = 0; i < chatbots.Length; i++) {
                for (int j = 0; j < chatbots.Length; j++) {
                    if (chatbots[i] != chatbots[j]) {
                        chatbots[i].OnNPCSpeakAction -= chatbots[j].NPC_ChatbotResponse;
                    }
                }
            }
        } else {
            for (int i = 0; i < chatbots.Length; i++) {
                for (int j = 0; j < chatbots.Length; j++) {
                    if (chatbots[i] != chatbots[j]) {
                        chatbots[i].OnNPCSpeakAction += chatbots[j].NPC_ChatbotResponse;
                    }
                }
            }
        }
    }

    private void OnDestroy() {
        for (int i = 0; i < chatbots.Length; i++) {
            for (int j = 0; j < chatbots.Length; j++) {
                if (chatbots[i] != chatbots[j]) {
                    chatbots[i].OnNPCSpeakAction -= chatbots[j].NPC_ChatbotResponse;
                }
            }
        }
    }
}
