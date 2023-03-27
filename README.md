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
3. Ensure FFMPEG is installed system-wide, or an ffmpeg executable is placed in the bin file of the project.
4. Run the program, and add your ElevenLabs key to your server's bot instance with the /addkey command (requires admin priveledges)
5. Use the generated /ai command to access your account's voices
