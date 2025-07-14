using System.Collections.Generic;
namespace KhokharumLaa.Models
{
    /// <summary>
    /// Represents a single structural and lyrical piece of a song (e.g., Verse 1, Chorus).
    /// This class maps to the 'SongParts' table.
    ///// </summary>
   

    public class SongPart
    {
       
        public int PartID { get; set; }

        // The type of the part, e.g., "Verse", "Chorus", "Bridge", "Tag".
        public string PartType { get; set; }

        // The number of the part, e.g., for "Verse 1", this would be 1. 
        public int PartNumber { get; set; }

        // The order in which parts are displayed.
      
        public int DisplayOrder { get; set; }

        // The actual lyric text for this part.
        public string Lyrics { get; set; }
    }
}
