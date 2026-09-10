# Records docs/images/concurrency.gif, the "concurrency" section of the console
# demonstration. Seven callouts at once, without and with a lock.
#
#   dotnet build                      # or the build output lands in the recording
#   vhs docs/tapes/concurrency.tape        # run from the repository root

Output docs/images/concurrency.gif

Set Shell bash
Set FontSize 15
Set Width 1280
Set Height 340
Set Padding 20
Set Theme "Catppuccin Mocha"
Set TypingSpeed 25ms

Hide
Type "export PS1='$ '" Enter
Type "clear" Enter
Show

Type "dotnet run --project src/EmergencyResponse.ConsoleDemo -- concurrency"
Sleep 700ms
Enter
Sleep 5s
