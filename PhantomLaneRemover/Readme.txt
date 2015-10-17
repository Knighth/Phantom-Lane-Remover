
Phantom Lane Remover
v1.2.1_Build003

Purpose
------------
Allows you to detect and remove phantom lanes if your save file is suffering from that problem. 
Chances are very high you don't have this problem and don't need this mod. 

What's a phantom lane? 
Lanes that exist in your map that are marked as created, but which link to nothing and are not really in use but are counting against your maximum number of lanes. 

How do I know if I have this problem? 
First off, you probably don't have it. 
If you've hit the "object limit" error message problem though and CSL Show More Limits shows you're over the Net Lane limit, then you might indeed have the problem. 

A good indicator of the problem is when your segment count * 10 is less than your number of lanes. That calculation is not always going to be an indicator, you could still have the problem if it's less but think of it as a sure sign you have the problem. Nothing in the base game (no mods) creates more then 10 lanes per segment, nor yet do most if any mods, so if your segment count * 10 is less than number of lanes in use something is very likely wrong. For most maps the average will be less than that 10 figure (probably more like 5), but think of 10 as the high-water mark. 

How to Use
-----------------
After subscribing to the mod, enable it and load your map, it should pop up a window after the map loads. 
Step 1: (skipable) Look at your segment #'s, multiply that figure by 10'ish 
Step 2: (skipable) Look at your lane #'s, are they higher then figure you came up with in step 1? If so proceed. 
Step 3: Press "Detect Phantom Lanes" - If the message says in green it detected 0 phantoms. You don't have the problem. If says in yellow'ish it detected ###### phantom lanes, and shows a "Fix Lanes" button then you have the problem. 
Step 4: The "Fix Lanes" button should exist, (Now would be a good time to backup your map!), now press it. 
Step 5: After a few seconds it should tell you in green how many lanes it cleaned up, the lane # shown in the counter list should reflect the change. You're done, save the now cleaned version of the map and go on about your game.

Questions
------------
Will this touch my save data. 
Yes. Though technically not till you save the game after using it, once the tool has been run there is no need to have the mod enabled after that, it's work has been done, at least until you need\want to check another map. 
PLEASE BACKUP YOU SAVE FILE BEFORE USING THIS TOOL. 

Will deleting these "phantom" lanes cause some other problems 
Probably not, to the extent that I have been able to fix serveral game files so far I've not seen a problem afterward and most of those maps were using various popular mods, including ones that screw around with lanes. 
However, i make NO guarantees. 

The gui looks like a stripped down Show More Limits, what's up with that? 
That's cause it is. :) Because it would be helpful to have some of the same data shown I just took my code from that mod, removed most of what wasn't needed and added in some for fixing this problem. Yes I know, I should just add this stuff to that one, I probably will if this issue sticks around, for now I wanted them separate. 

What causes phantom lanes to get created in the first place? 
I don't know. Spent way too much time trying to tack down the cause with no results. This I can tell you, with the base game (no mods), I think I've tried every possible thing to reproduce this problem and can not, so don't scream at C\O over this one yet. I also can not reproduce it yet with mods either, however every sample map I've seen with this problem had a certain type of mod used on it. I'm not going to say that type yet because it's not conclusive yet which one is the cause, if one of those is even the cause at all. 

How can I help you find the cause of these phantoms? 
Great question. Please share your save file (before you apply the fix) if you have the problem along with anything you can tell me about said save file, but especially what mods were used, the more you detail the better, though you can skip generic assets, but special transport ones would be helpful to know (like multi line train stations,etc). 

Is this compatible with other mods? 
It should be compatible with everything, it does not detour any CSL functions to operate. 
The CTRL + (P & L at same time) key binding in theory though could conflict \ activate some other mod, however you really shouldn't need to re-display the dialog during a session anyway. 

*Thank you to all those on Reddit, Steam, and the CSL PDX forums who've helped me with supplying sample files and in investigating this. As always if this mod helped you don't forget to give a thumbs up. 
