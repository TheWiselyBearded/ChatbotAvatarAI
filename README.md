# AI NPC Unity Template Scene

This repository contains a template scene for interfacing with an AI-based NPC using OpenAI API, Azure Voice API, Google Cloud Speech to Text, and Oculus Lip Sync. The project uses Unity 2021.3.x.

The framework allows for easy integration with YOLO-NAS, enabling the NPC to stream virtual camera frames and receive responses from a YOLO-NAS server instance, whether local or remote. The response includes all identified objects along with their confidence scores. Additionally, it leverages the Ready Player Me avatar, providing a reference for mapping Oculus lip sync to avatar models.

Roadmap:
- Integrate RageAgainstThePixel's Open AI library (https://github.com/RageAgainstThePixel/com.openai.unity).
- Add support for Eleven Labs' text-to-voice library (https://github.com/RageAgainstThePixel/com.rest.elevenlabs).
- Make use of YOLO-NAS results in OpenAI conversation requests.
- Add example scene with Avaturn Avatar Models (https://avaturn.me/).
- Group NPC conversations.
- Implement a dynamic way of invoking pre-downloaded animations.

These additions to the project will enhance its capabilities and expand the available libraries and features over time.

Feel free to contribute to the project!

## Setup

Before running the scene, you'll need to set up the following services and create a configuration file for the application to read at runtime:

1. [Google Cloud Speech to Text](https://cloud.google.com/speech-to-text)
2. [Azure Voice API](https://azure.microsoft.com/en-us/services/cognitive-services/speech-services/)
3. [OpenAI API](https://beta.openai.com/docs/)

Review the setup instructions for the following repositories that are used in this project:

- [OpenAI C# API](https://github.com/betalgo/openai)
- [Google Speech to Text](https://github.com/oshoham/UnityGoogleStreamingSpeechToText)
- [Azure Voice](https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/quickstart/csharp/unity/text-to-speech/README.md)
- [Oculus Lip Sync](https://developer.oculus.com/documentation/unity/audio-ovrlipsync-using-unity/)

### Configuration File

In the `StreamingAssets` folder, create a `services_config.json` file with the following template, and replace the placeholder values with your own API keys and region information:




```json
{
"OpenAI_APIKey": "your_openai_api_key",
"AzureVoiceSubscriptionKey": "your_azure_voice_subscription_key",
"AzureVoiceRegion": "your_azure_voice_region"
}
```


Create a `gcp_credentials.json` file for Google Cloud runtime to read configuration properties from using the following template:

```json
{
"type": "service_account",
"project_id": "YOUR PROJECT ID",
"private_key_id": "YOUR PRIVATE KEY ID",
"private_key": "YOUR PRIVATE KEY",
"client_email": "YOUR CLIENT EMAIL",
"client_id": "YOUR CLIENT ID",
"auth_uri": "https://accounts.google.com/o/oauth2/auth",
"token_uri": "https://oauth2.googleapis.com/token",
"auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
"client_x509_cert_url": "YOUR CLIENT CERT URL"
}
```


### Known Issues

Sometimes on the first time opening the project, you might get the following error message: "Multiple precompiled assemblies with the same name Newtonsoft.Json.dll included on the current platform." This error occurs because Unity enforces a Newtonsoft import due to its services.core dependency. To resolve this error:

1. Close the project
2. Open the file explorer and navigate to `\Library\PackageCache\com.oshoham.unity-google-cloud-streaming-speech-to-text@0.1.8\Plugins`
3. Delete the `Newtonsoft.dll` file
4. Reopen the project and hit "Ignore"
5. Delete `Newtonsoft.dll` again from the same location
6. The import should now complete.

## Usage

Once you've set up the configuration files, you can run the scene and begin interacting with the NPC by asking it questions. The NPC will respond within seconds.

## Notes
Models Used:
This project currently uses the TextDavinciV3 model provided by OpenAI's GPT-3 API. Additionally, there is a separate branch attempting to update to the latest publicly available model, ChatGpt3_5Turbo.

In addition to the APIs and packages mentioned above, this project also uses the Meta Movement SDK. More information on this SDK can be found in its GitHub repository at https://github.com/oculus-samples/Unity-Movement.



## References

* [Quickstart article on the SDK documentation site](https://docs.microsoft.com/azure/cognitive-services/speech-service/quickstart-text-to-speech-csharp-unity)
* [Speech SDK API reference for C#](https://aka.ms/csspeech/csharpref)
