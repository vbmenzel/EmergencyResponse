# Records docs/images/policy.gif, the "policy" section of the console
# demonstration. The same centre and roster, two assignment policies.
#
#   dotnet build                      # or the build output lands in the recording
#   vhs docs/tapes/policy.tape        # run from the repository root

Output docs/images/policy.gif

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

Type "dotnet run --project src/EmergencyResponse.ConsoleDemo -- policy"
Sleep 700ms
Enter
Sleep 5s
