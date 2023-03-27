# ElevenLabsTTSDiscordBot

This is the logic for a discord bot to hook up to a user's (or users') ElevenLabs.io account and take Slash Commands to generate, send, and play sound files in a Discord server. 

## SETUP

Before Running the project, ensure the following steps are completed:

1. Add your Discord bot's Token to the appsetting.json file under 'token'.
2. Generate your bot's invite link by including the following scopes and permissions, and invite to the desired server:
    - Scopes:
      - bot
      - applications.commands
    - Permissions
      - Text Permissions
        - Send Messages
        - Manage Messages
      - Voice Permissions
        - Connect
        - Speak
3. Download the zip file of .dll's for your system from this link: https://github.com/discord-net/Discord.Net/tree/dev/voice-natives
4. Extract libopus.dll and libsodium.dll to your program's root folder (Probably bin/debug/net7.0/ (May need to be built once in debug mode))
5. Change libopus.dll name to opus.dll
6. Run the program, and add your ElevenLabs key to your server's bot instance with the /addkey command (requires admin priveledges)
7. Use the generated /ai command to access your account's voices

3/21/2023 NOTE: As of 3/15/2023, Discord has pushed a change to the voice connection process which breaks Discord .Net's voice functionality. Please pull Discord .Net dev branch locally and swap from the nuget version to the local version for the time being, until the change gets pushed to the nuget branch.
