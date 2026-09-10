# Records docs/images/roster.gif, the "roster" section of the console
# demonstration. Who is on shift and what has been called in.
#
#   dotnet build                      # or the build output lands in the recording
#   vhs docs/tapes/roster.tape        # run from the repository root

Output docs/images/roster.gif

Set Shell bash
Set FontSize 15
Set Width 1280
Set Height 520
Set Padding 20
Set Theme "Catppuccin Mocha"
Set TypingSpeed 25ms

Hide
Type "export PS1='$ '" Enter
Type "clear" Enter
Show

Type "dotnet run --project src/EmergencyResponse.ConsoleDemo -- roster"
Sleep 700ms
Enter
Sleep 5s
