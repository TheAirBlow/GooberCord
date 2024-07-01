# GooberCord Server
Discord bot and REST API server

## Setup
1) Install MongoDB and start it's daemon
2) Compile project with .NET 8 SDK
3) Start server and wait for it to crash
4) Open `config.json` and edit it:
```json
{
  "_AuthSecret": "Symmetric key for JWT signing",
  "AuthSecret": "[REDACTED]",
  "_Customization": "Various customization options",
  "Customization": {
    "_GlobalMessage": "Format for a global chat message",
    "GlobalMessage": "*{0}*: {1}",
    "_LocalMessage": "Format for a local chat message",
    "LocalMessage": "<:zpmvibrator:1171802059343921173> *{0}*: {1}",
    "_AssignedProxy": "Notification sent when a new proxy player is assigned",
    "AssignedProxy": "<:obama:1138432261411324025> *{0}* is now proxying chat",
    "_NoProxy": "Notification sent on failure to assign a new proxy player",
    "NoProxy": "<:TrollShrug:1256731287310569563> No players left to proxy chat"
  },
  "_Bot": "Discord bot configuration",
  "Bot": {
    "_Token": "Bot authentication token",
    "Token": "change_me",
    "_Guild1": "Guild to register slash commands in",
    "_Guild2": "Set to null to register them globally",
    "Guild": null
  }
}
```
5) Start server again and enjoy!
6) Create a TTL index for links through mongosh:
```js
use goobercord;
db.links.createIndex({ "ExpireAt": 1 }, { expireAfterSeconds: 0 })
```

## Official instance
By default the [official mod](https://git.sussy.dev/GooberSoft/GooberCord/-/tree/client) will use the official server: `gc.sussy.dev` \
You can use it by [downloading the mod](https://example.com/) and [inviting the bot](https://discord.com/oauth2/authorize?client_id=1256954682862075946&permissions=277025737728&integration_type=0&scope=bot) to your guild.

## Commands
1) `/link` - Gives a code for linking your Minecraft account to Discord
2) `/unlink` - Unlinks a Minecraft account from your account
3) `/links` - Lists all used linking codes from all guilds
4) `/regex <channel> <regex>` - Changes chat message regex for relay channel
5) `/server <channel> <server>` - Changes server IP for relay channel
6) `/create <channel> <server>` - Creates a new relay channel
7) `/delete <channel>` - Deletes a relay channel

## Development
Swagger docs are accessible at `/swagger/index.html`