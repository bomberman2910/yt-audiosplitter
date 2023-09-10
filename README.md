# yt-audiosplitter
A CLI tool that takes a mp3 file and a textfile with timestamp and metadata and cuts it into individual song files. Requires ffmpeg

## Usage

```yt-audiosplitter <path-to-file-with-timestamps> <line-order-in-timestamp-file>```

The mp3 file that is supposed to be split up must have the same name as the timestamp file, just with the ending .mp3.

Since this tool was mainly built for splitting audio mixes from YouTube and timestamps in the videos description don't have a uniform format, this tool can handle a few common variations, one of which has to be given as an argument:

- ```sta```: The lines in the timestamp file follow the order ```<timestamp> <song title> - <song artist>```
- ```sat```: The lines in the timestamp file follow the order ```<timestamp> <song artist> - <song title>```
- ```tas```: The lines in the timestamp file follow the order ```<song title> - <song artist> <timestamp>```
- ```ats```: The lines in the timestamp file follow the order ```<song artist> - <song title> <timestamp>```

The timestamps have to be in the format ```h:mm:ss``` or ```mm:ss```. There can be no other information in the lines (such as track numbers for example).

For splitting this tool uses ffmpeg, so make sure it is installed. ffmpeg is also used to set some metadata on the output files:

- title and artist are taken from the line in the timestamp file
- the track number is taken from the position of the line in the timestamp file
- the album is set to the title (not the file name, but the metadata) of the input file
- the album artist is set to "Various Artists" to keep the songs together in your media player of choice
