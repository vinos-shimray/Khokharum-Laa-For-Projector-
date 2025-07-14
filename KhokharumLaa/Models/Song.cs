using System.Collections.Generic;

namespace KhokharumLaa.Models
{
    /// <summary>
    /// Represents the core metadata for a single song.
    /// This class maps to the 'Songs' table in the database.
    /// </summary>
    public class Song
    {
        // The unique ID  'kkl' column.
        public int SongID { get; set; }

        // The title of the song.
        public string Title { get; set; }

        // The musical key of the song (e.g., "C", "G#m").
        public string Key { get; set; }

        // The associated bible verse for the song.
        public string BibleVerse { get; set; }

        // A list of all authors for this song.
      
        public List<Author> Authors { get; set; }

        // A list of all the lyrical parts (verses, choruses, etc.) for this song.
        
        public List<SongPart> Parts { get; set; }

        /// <summary>
        /// Constructor to ensure the lists are never null.
        /// </summary>
        public Song()
        {
            Authors = new List<Author>();
            Parts = new List<SongPart>();
        }
    }

    


}
