# 🤖 Sectom

Sectom is a multi-purpose Discord bot designed to keep your server protected, safe, and engaging.  
It offers a wide range of features including moderation tools, fun games, and server management utilities.

## 🌟 Features

- ⚙️ **Administration**: Server management and configuration settings.
- 🛡️ **Moderation**: Comprehensive moderation commands including mute, kick, ban, timeout, and more.
- 📝 **Logging**: Keep track of important changes with customizable logging options.
- 🛠️ **Miscellaneous**: Utility commands like avatar, ping, help, and more.
- 🎮 **Games**: Enjoy interactive games like Rock Paper Scissors (RPS) and Tic-Tac-Toe (TTT).
- 🏆 **Leveling System**: Engage your community with an XP-based leveling system, including leaderboards and rank commands.
- 💡 **Suggestion System**: Allow users to submit and vote on suggestions for your server.

## ⌚ Coming Soon

- 🎫 **Ticket System**: Efficiently manage user inquiries and support requests.


## 🔧 Commands

Sectom offers a variety of slash commands across multiple categories:


<details>
<summary>⚙️ <strong>Admin</strong></summary>

| Command                               | Description                                               |
|---------------------------------------|-----------------------------------------------------------|
| `config leveling modify-settings`     | Modify the settings                                       |
| `config leveling add-auto-role`       | Adds an auto role on reaching a certain level             |
| `config leveling remove-auto-role`    | Removes an auto role for a certain level                  |
| `config leveling view-auto-roles`     | View the configured auto roles                            |
| `config leveling disable`             | Disable this configuration                                |
| `config leveling enable`              | Enable this configuration                                 |
| `config log-channel set-bot-log`      | Add or modify a bot log channel configuration             |
| `config log-channel set-audit-log`    | Add or modify an audit log channel configuration          |
| `config log-channel remove-bot-log`   | Remove a bot log channel configuration                    |
| `config log-channel remove-audit-log` | Remove an audit log channel configuration                 |
| `config log-channel view-bot-log`     | View the bot log channel configuration                    |
| `config log-channel view-audit-log`   | View the audit log channel configuration                  |
| `config suggestion add-panel`         | Add a panel to group components into an embed             |
| `config suggestion remove-panel`      | Remove a panel by name                                    |
| `config suggestion modify-panel`      | Modify a panel                                            |
| `config suggestion add-component`     | Add a component to a given panel                          |
| `config suggestion remove-component`  | Remove a component from a panel                           |
| `config suggestion modify-component`  | Modify a component                                        |
| `config suggestion send-panel`        | Send a panel to the current or a specified text channel   |
| `config suggestion view-panels`       | View the configured panels                                |
| `config suggestion view-components`   | View the components from a specified panel                |
| `config warn add-timeout-punishment`  | Add a timeout punishment on reaching a number of warnings |
| `config warn add-ban-punishment`      | Add a ban punishment on reaching a number of warnings     |
| `config warn remove-punishment`       | Remove a current punishment configuration                 |
| `config warn view-thresholds`         | View the configured warning thresholds                    |
| `config warn disable`                 | Disable this configuration                                |
| `config warn enable`                  | Enable this configuration                                 |

</details>

<details>
<summary>🛡️ <strong>Moderation</strong></summary>

| Command     | Description                                                                        |
|-------------|------------------------------------------------------------------------------------|
| `ban`       | Ban a user from the server                                                         |
| `softban`   | Ban a user to prune their messages and then immediately unban them from the server |
| `unban`     | Unban a user from the server                                                       |
| `deafen`    | Deafen a user in their current voice channel                                       |
| `kick`      | Kicks a user from the server                                                       |
| `mod-note`  | Add a moderation note to a user in the server                                      |
| `mute`      | Mute a user in their current voice channel                                         |
| `nick`      | Set the nickname of a user in the server                                           |
| `purge`     | Bulk delete messages in the current channel                                        |
| `timeout`   | Timeout a user on the server                                                       |
| `untimeout` | Remove a timeout from a user on the server                                         |
| `warn`      | Hand out an infraction to a user on the server                                     |
| `case view` | View a specific case on the server                                                 |
| `case list` | List and filter all cases on the server                                            |

</details>

<details>
<summary>🛠️ <strong>Miscellaneous</strong></summary>

| Command       | Description                                |
|---------------|--------------------------------------------|
| `avatar`      | Display the avatar of a user               |
| `help`        | Displays an interactive help menu          |
| `ping`        | Get the latency of the bot in milliseconds |
| `server-info` | Get information about the server           |
| `user-info`   | Get information about a user in the server |

</details>

<details>
<summary>🎮 <strong>Games</strong></summary>

| Command | Description                           |
|---------|---------------------------------------|
| `rps`   | Play rock-paper-scissors-lizard-spock |
| `ttt`   | Play tic-tac-toe                      |

</details>

<details>
<summary>🏆 <strong>Leveling</strong></summary>

| Command       | Description                       |
|---------------|-----------------------------------|
| `leaderboard` | Displays the level xp leaderboard |
| `rank`        | Display the rank of a user        |

</details>