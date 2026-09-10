# Records docs/images/callbacks.gif, the "callbacks" section of the console
# demonstration. One callout end to end, with both callback forms.
#
#   dotnet build                      # or the build output lands in the recording
#   vhs docs/tapes/callbacks.tape        # run from the repository root

Output docs/images/callbacks.gif

Set Shell bash
Set FontSize 15
Set Width 1280
Set Height 300
Set Padding 20
Set Theme "Catppuccin Mocha"
Set TypingSpeed 25ms

Hide
Type "export PS1='$ '" Enter
Type "clear" Enter
Show

Type "dotnet run --project src/EmergencyResponse.ConsoleDemo -- callbacks"
Sleep 700ms
Enter
Sleep 5s
