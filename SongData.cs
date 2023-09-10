internal class SongData
{
    public SongData(int trackNumber, DateTime timestamp, string artist, string title)
    {
        TrackNumber = trackNumber;
        Timestamp = timestamp;
        Artist = artist;
        Title = title;
    }

    public DateTime Timestamp { get; set; }
    public TimeSpan Length { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public int TrackNumber { get; set; }
}
