The application expects the following to be set in the application settings:
---

**StorageConnectionString**: Tablestorage connectionstring

**GreyOtron-Token**: Discord bot token

**DiscordBotsToken**: DiscordBots token
**DiscordBotId**: Discord bot id

In local development you can use:
dotnet user-secrets set {name} "value" --project {projectName}


This can be done the conventional way in an azure web app for example.

Currently the bot is hosted on azure and can be added to your server using:
https://discordapp.com/oauth2/authorize?client_id=518387999537496069&scope=bot&permissions=268512320
