PREREQUISITES

The game requires at least .NET Framework 4.7.2 or Mono 5.18.0.


SETTING UP A SERVER

1. Open settings.json and set the "host" property to the URL and port you want to host the webapp on. Customize your other game settings as desired.

2. Add any extra pack files (they will end in .json) to your "packs" folder.

3. Run CardsOverLan.exe. You'll likely get a prompt from your firewall; if so, allow the application.
If you'd rather add firewall exceptions manually, make sure to whitelist TCP port 80 (or your custom port) and TCP port 3000 (for WebSockets).

4. On a different device connected to the same network, open a browser and connect to your hostname/IP and port. Verify that you can connect to the game.

5. Invite your friends. Enjoy!
