# ACNHSpawner 
For mobile and desktop, despite the name.

Multi-tool app for Animal Crossing: New Horizons built in Unity. Designed to be used while you are playing the game so you don't have to manually edit saves on a PC. Confirmed working on Windows, Mac, Linux, Android and iOS.

『あつまれ どうぶつの森』でリアルタイムにアイテムをインジェクトしたり島を編集したりゲーム内の値を変更したり等のカスタマイズを行うことができるAndroid/ iOS(Windows/ macOS/ Linuxも可)向けユーティリティ。

Requires a Switch running custom firmware with the sysmodule [sys-botbase](https://github.com/olliz0r/sys-botbase) or [USB-Botbase](https://github.com/fishguy6564/USB-Botbase) installed.

It currently supports the following in-play actions:
* Injecting and deleting inventory items. Supports all players 1-8.
* Changing amount of miles, bells in your bank and in your wallet (inventory).
* [Changing and replacing villagers](https://www.youtube.com/watch?v=5CUUZhGtsxk) using the perfect villager database.
* Changing the turnip buy/sell prices and fluctuations.
* [Place and bulk spawn items to your map](https://www.youtube.com/watch?v=LfedVAabGN4), a few presets exist within the app. You may also find and replace items.
* [Spawning internal items*](https://www.youtube.com/watch?v=q50R6ky0hIQ) such as the donut. [A list of all internal items is here.](https://github.com/berichan/ACNHMobileSpawner/wiki/List-of-internal-items)
* Hex editing raw RAM bytes. This can be used in any Switch game, not just Animal Crossing.
* Removing certain items from your map, such as weeds, trees, flowers, spoiled turnips, etc.
* Saving, sharing and loading certain New Horizons file types: _*.nhi (inventory), *.nhv (villager) and *.nhvh (villager house)._
* [Refresh items on the floor of your island](https://www.youtube.com/watch?v=w1PKrrQJyjE&t=16s), and logs people coming in during the time the refresher was running.
* [Freeze certain values (villagers, inventory, map etc)](https://www.youtube.com/watch?v=1_0FbbIZLqM)
* Create teleports so you can easily move between common areas.

Refer to the [Wiki](https://github.com/berichan/ACNHMobileSpawner/wiki) for help and troubleshooting.

Based heavily on [NHSE](https://github.com/kwsch/NHSE).

You run this at your own risk, I'm not responsible for anything. Please check the [license](https://github.com/berichan/ACNHMobileSpawner/blob/master/LICENSE) for full details before using the app or source. 

*Please do not use this app to ruin the experience of other players, **be responsible!** Do not trade or use items from the [internal list](https://github.com/berichan/ACNHMobileSpawner/wiki/List-of-internal-items) in local or online play- this should go without saying, but **they will ruin the experience of other players** as they are not easily removable on a non-cfw console.

### Discord help server

You may ask for help in the support server if you're running into trouble. Please [read the wiki](https://github.com/berichan/ACNHMobileSpawner/wiki) first if you haven't already done so.

[<img src="https://canary.discordapp.com/api/guilds/771477382409879602/widget.png?style=banner2">](https://discord.gg/5bT8XK8dYe)

### Builds

Each major release is built for Windows, Android, MacOSX and iOS. You may download the [compiled builds here](https://github.com/berichan/ACNHMobileSpawner/releases).

iOS builds are auto-built and untested, but I've been told they work. 

### Screenshots

<img src = "https://user-images.githubusercontent.com/66521620/84556327-bcb53000-ad19-11ea-96c6-12dc65441efd.png" width = "300">

### Video guide

<a href="https://youtu.be/c5HJgwqeb7w" target="_blank"><img src = "https://i.imgur.com/XJnWZk2.jpg" width = "300"></a>

Click the image above.

### Notes

Some code was deleted and had to be rebuilt using ILSpy. I've done my best to clean up the classes affected, but they will be uncommented, minus ILSpy warnings I've kept in just to know what was affected.
