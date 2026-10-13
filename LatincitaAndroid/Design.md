# DESCRIPTION

This is a simple Android App that is intended as a distraction-free 
version of the website "www.latincita.com".  It allows users to listen
to music hosted on the website, whilst driving a car.  It tries to
present the user with as simple an interface as possible, with large
buttons that require a minimum of user interaction.  The idea is to
select something, and then listen to songs for at least one hour without
having to touch the app.

# ELEMENTS

The music it manages comes in three forms: "SONGS", "TRACKS" and "RADIOS".

- "SONGS" are items which are 5 minute-long MP3's, containing a single song.
- "TRACKS" are items that are a single song embedded in an hour-long MP3.  
- The hour-long MP3 itself is an item referred to as a "RADIO-SHOW".

All music is arranged into collections.  There are 4 types of collections...

- a "CD" is collections of "SONGS"
- a "RADIO" is a single "RADIO-SHOW" which contains a list of "TRACKS"
- a "FAVORITE-LIST" is a collection of "SONGS" and/or individual "TRACKS"
- there is a "RANDOM" collection that continuously selects a random
  "SONG" or "TRACK" and (automatically) adds it to the current collection.

# USER-INTERFACE

From the home-page, the user selects either "CD", "RADIO", "FAVORITES" or "RANDOM".

- "CD" presents the user with a list of "CD" collections
- "RADIO" presents the user with a list of "RADIO" collections
- "FAVORITE" presents the user with a list of "FAVORITE-LISTS"
- "RANDOM" selects one random "SONG" or "TRACK" and adds it to a new collection

Selecting one item from the collection, produces a screen with a large image
representing the collection, with the name and perhaps a date for the collection.
Below this is a table/list with the items in the collection.

For a "CD" collections, each item is a "SONG", and they are automatically played
from top to bottom.  The user can skip to the next or the previous "SONG", or
return to the list of "CD"'s, or return to the home-page.

For a "RADIO", one long MP3 is queued up and the offset of the "playhead" is used
to determine which "TRACK" is currently playing.  The relevant track is highlighted
in the list below.  The user can move up and down through the track-lists, or
return to the list of "RADIO"'s, or to the home-page.

For "FAVORITE"'s, the user receives a list of "SONGS" and/or "TRACKS".  
For "SONGS",a single MP3 is loaded and played in its entirety.  For "TRACKS",
the entire MP3 needs to be loaded and the "playhead" moved to the start of the
"TRACK".  If the "TRACK" is from the same "RADIO" as the previous "TRACK", then 
the "playhead" only needs to be moved.  The correct MP3 is already queued up.  
The user has the same options of moving to the previous or next "SONG" or "TRACK",
with the complication of either loading a MP3, or moving the "playhead".  The user can return to the list of "FAVORITE"'s, or to the home-page.

Handling "FAVORITE"'s has an extra complication: each "SONG" or "TRACK" has its
own image and meta-data.  These replace the image and text at the top of the page
when the "SONG" or "TRACK" starts playing.

Handling a "RANDOM" collection is particularly complicated.  On the "Sub-Page", 
the user can keep hitting the "RANDOM" button to add new items to the collection.
After a number of items have been added, the user can tap on any one of the items.
A page appears with all the current items in the collection in a list.  The item
the user tapped on is highlighted, and starts playing. The user can move to the
previous item or next item.  However, if the user is at the first or the last
item in the collection, then a new "RANDOM" "SONG" or "TRACK" is added to the
collection.  When playing music, this happens automatically when the bottom
of the list is reached.   From this page, the user can clear the list and create
a new blank collection, by returning to the previous screen. Or the user can
go to the home-page.

Handling "RANDOM" has the same problem as "FAVORITE". Each "SONG" or "TRACK" 
retrieved has its own image and meta-data.  These replace the image and text at 
the top of the page when the "SONG" or "TRACK" starts playing.

# IMPLEMENTATION

Under the motor, this app is using the "CommunityToolkit.Maui.MediaElement"
as a music-player.  It does not use the built in GUI controls of the
MediaElement, and instead implements its own controls including a slider
for the playhead-position.

There are only two XAML pages in this App: MainPage and DetailsPage.  
MainPage contains both the buttons for selecting a collection-type, and 
the list of collections of the selected type.  Selecting a collection from 
this list, loads the DetailsPage with an image at the top, and a list of items
below it.

# INTERNAL DATA

All the "SONGS", "TRACKS" and "RADIO-SHOWS" are stored in a single table of 
"TrackObject's".  This table is referred to as "theFsongs".  It contains 
the URL of artwork for each song, the URL of the MP3, and all relevant 
meta-data.

Each item in a collection is a "RadioProgram" object.  The list of "CD's" is
for example a list of "RadioProgram" objects.  When the user selects an item
in the list of collections, the selected "RadioProgram" object is used to 
retrieve the corresponding list of "TrackObjects" (from "theFsongs").

The items in a "RadioProgram" are therefore objects of the class "TrackObject"
These items are added to the "CurrentTrackList", which gets rendered as the
list/table on the DetailsPage.  

The TrackObject's in the CurrentTrackList are converted into one or several
PlayListItem's.  Each "PlayListItem" has a track_list property, which is a 
list of TrackListItem's.

For a "CD" collection, each PlayListItem contains a single "SONG" and the 
track_list property on the PlayListItem is empty.

For a "RADIO" collection, there is only a single PlayListItem, and its 
track_list contains a TrackListItem for each "TRACK" in the radio-program.

For a "FAVORITE" or a "RANDOM" collections, the items in the collection are
either "SONGS" or "TRACKS".  Each item will have a corresponding PlayListItem
object.   If the item is a "TRACK", then the track_list on the item's PlayListItem
will have a track_list property with only one TrackListItem.  This contains the
image-URL, and begin and end offsets of the "TRACK" in the hour-long MP3.

There is a background-service that is responsible for fetching MP3's and
driving the MediaElement.  The background-service has a queue of PlayListItem's
that it receives from the front-end.  When allowed, the background-service
will fetch a PlayListItem from the queue, and pass it on the the MediaElement.
When a item finishes playing, the background service is instructed to take a 
new item from the queue, and ask the MediaElement to play it. This should all
happen automatically.  However, the user can ask the background-service to go 
to the previous item fetched from the queue, or to skip to the next item in 
the queue.

One important point to mention is the relationship between TrackObject and
PlayListItem and the TrackListItem's in the PlayListItem's track_list.  
Each TrackObject has a "cached_playlist_item" property that will point to a
PlayListItem if the TrackObject was used to create a PlayListItem.  Conversely, 
the resulting PlayListItem has a "track" attribute that points back to the
TrackObject it was generated from.   
All TrackObject's have a primary key "id_str" that identifies them.  
PlayListItem's have a guid that uniquely identifies them.

If a TrackObject is a TRACK, then the corresponding PlayListItem will have one 
or more TrackListItem's in its track_list.  The TrackObject will correspond to 
only one of these TrackObject's.  Which one can currently only be determined by
comparing the TrackObject's offset with the offs property of each of the 
TrackListItem's in the track_list.

If a TrackObject is a "RADIO-SHOW", then the all the corresponding 
TrackListItem's in the PlayListItem's track_list will match the TrackObject.

One important note is how "RADIO-SHOW"'s are stored.  
The entire hour-long "RADIO-SHOW" will have a TrackObject where 
"purpose" = "BAND" or "RADIO".  This TrackObject will have a given "songid".  
All the TRACK's in this RADIO-SHOW will have a radioid where
TRACK.radioid == RADIO-SHOW.songid.  This is how the app finds the TRACKS in
a RADIO-SHOW.

# PROGRAM-FLOW

Execution starts when the user selects a collection-type from the MainPage.
This executes code in the RadioProgramsViewModel.  Typically, the
RadioProgramsService is called to fetch a list of RadioProgram objects from the website.  These objects are rendered on the MainPage.   For the "RANDOM" collection
type, the RandomService is called to return a single PlayListItem that represents 
either a "SONG" or a "TRACK".  Each PlayListItem is converted to a RadioProgram,
and added to the items on the MainPage.

When the user taps on an item on the MainPage, the selected RadioProgram is passed 
To GoToDetails in the RadioProgramsViewModel.   This calls SetProgram in 
ProgramListService to select the current RadioProgram to display on the 
DetailsPage.   

After displaying the RadioProgram's image and title, SetProgram fetches the items
for the table/list under the MediaPlayer-controls below the image & title.

SetProgram calls get_track from the AllLatincitaService to either fetch an entire
RADIO-SHOW (the TrackObject that points to the hour-long MP3) or to select the
TrackObject pointed to in the selected RANDOM item's SHOW or TRACK.

SetProgram will also use the AllLatincitaService to fill the CurrentTrackList
with TrackObject's related to the current RadioProgram.   These are rendered
to the table/list mentioned earlier.

AllLatincitaService maintains "theFsongs".  If this list is empty, it is fetched
from "www.latincita.com".  All TrackObject's it returns, are copied from 
"theFsongs".

After SetProgram has filled CurrentTrackList, each TrackObject in the list
is converted to a PlayListItem using addToPlaylist in ProgramListService.
As explained earlier, a PlayListItem has a track_list property that is either
empty, or contains a single "TRACK" or all the "TRACK"s on a "RADIO".

Each of the PlayListItem's generated is pushed to the background-service's
queue with QueueTrackAsync via the AudioPlaybackService.

After this, we have a list of TrackObject visible to the user, and one or more
PlayListItem's in the background-service's queue.

The app waits until the MediaElement on the page has been initialized, and then
it tells the background-service to start processing its queue.

The background-service takes a PlayListItem from the queue, and instructs the
MediaElement to load the required MP3.  If necessary, it will wait for the MediaElement to queue up the MP3, and then instructs it to SEEK to the first track
on the PlayList's track_list.  After the SEEK has completed (or if no SEEK is 
required) the MediaElement is instructed to start Playing.  

From here, event handlers for the MediaElement on DetailsPage.cs will take over.  
They try to determine what the MediaElement is doing and update the user's display
accordingly.   Using the OnPositionChanged event, the code will try to determine
which item in the CurrentTrackList is currently being played, and will highlight 
it. When OnPositionChanged determines that an item on CurrentTrackList has finished
playing, it may tell the MediaElement to stop playing, and ask the 
background-service to fetch the next item from the queue.  Or, in the case of
RANDOM's it could fetch a new RANDOM using RandomService, add this to 
CurrentTrackList, and push it as a PlayListItem onto the background-service's
queue.  [It should also be added as a RadioProgram to the items on the MainPage.]

Note, that highlighting the currently playing item in the CurrentTrackList may
also involve updating the image and title-information for that item at the top
of the DetailsPage.  This is not done for "RADIO" or "CD" collection items.  
Highlighting a line in the table, and optionally updating the image is done by 
SetTrack in ProgramListService.

As mentioned in the section about the MediaElement, the built in controls are
not used.  Buttons and sliders on the DetailsPage call functions on the 
MediaElement. The are used to pause/resume playing or scroll back and forth 
inside a track.

The Previous/Next track buttons are more complex.  These need to determine the
previous/next item on the CurrentTrackList, and figure out how to get this item
to play.  In the case of a RANDOM, this could trigger the code to fetch a new
RANDOM and add it to all lists, as explained above.






