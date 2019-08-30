#dkp-bot

**Step One** Before you compile program, first create bot(application)
https://discordapp.com/developers/applications/

Go to Bot settings (add bot)

Copy Client ID in General Inforamation 
replace client id:
https://discordapp.com/oauth2/authorize?client_id=CLIENT_ID&scope=bot&permissions=1
select server

Go to Bot menu and copy Token (for config file)
--------------------------------------
**Step Two** COMPILE PROGRAM, ADD THIS LINE TO CONFIG FILE:
add to config.json

{
  "token": "TOKEN",
  "cmdPrefix": "!",
  "raidRole": "Raid Leader",
  "inRaid": "In Raid",
  "minBid": 5, // minimum bid that must be devisible with specified number
  "auctionTime": 25 // auction time in seconds
}

-------------------------------------

**Step Three** Create Raid Leader and In Raid role
use "In Raid" role while you are in raid

in Discord type "!help" for help

---
![discord-preview](https://raw.githubusercontent.com/softban/nodkp/master/setup/discord-preview.png)
---


