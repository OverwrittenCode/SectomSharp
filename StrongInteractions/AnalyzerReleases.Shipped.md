## Release 1.0

### New Rules

 Rule ID | Category | Severity | Notes               
 --------|----------|----------|--------------------
 SI001   | StrongInteractions | Error    | Methods decorated with [StrongSelectMenuInteraction] must have a string[] as the last parameter to properly handle select menu interaction values. 
 SI002   | StrongInteractions | Error    | Methods decorated with [StrongModalInteraction] must have a class that implements the IModal interface as the last parameter to properly handle modal interactions.
