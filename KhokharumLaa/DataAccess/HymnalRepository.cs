using KhokharumLaa.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace KhokharumLaa.DataAccess
{
    /// <summary>
    /// Handles all data access and communication with the SQLite database.
    /// This is the single source of truth for all hymnal data.
    /// </summary>
    public class HymnalRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes the repository and sets up the path to the database.
        /// </summary>
        public HymnalRepository(string dbFileName)
        {
            
            string dbPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, dbFileName);
            _connectionString = $"Data Source={dbPath}";

            if (!File.Exists(dbPath))
            {
               
                throw new FileNotFoundException("The database file was not found.", dbPath);
            }
        }

        /// <summary>
        /// Adds a new song and its parts to the database within a single transaction.
        /// </summary>
        /// <param name="song">The song object to add. The SongID will be updated upon successful insertion.</param>
        public void AddSong(Song song)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                       
                        var insertSongCmd = connection.CreateCommand();
                        insertSongCmd.Transaction = transaction;
                        insertSongCmd.CommandText = "INSERT INTO Songs (Title, Key, BibleVerse) VALUES ($title, $key, $bibleVerse)";
                        insertSongCmd.Parameters.AddWithValue("$title", song.Title);
                        insertSongCmd.Parameters.AddWithValue("$key", song.Key ?? (object)DBNull.Value);
                        insertSongCmd.Parameters.AddWithValue("$bibleVerse", song.BibleVerse ?? (object)DBNull.Value);
                        insertSongCmd.ExecuteNonQuery();

                      
                        var lastIdCmd = connection.CreateCommand();
                        lastIdCmd.Transaction = transaction;
                        lastIdCmd.CommandText = "SELECT last_insert_rowid()";
                        long newSongId = (long)lastIdCmd.ExecuteScalar();
                        song.SongID = (int)newSongId;

                       
                        for (int i = 0; i < song.Parts.Count; i++)
                        {
                            var part = song.Parts[i];
                            var insertPartCmd = connection.CreateCommand();
                            insertPartCmd.Transaction = transaction;
                            insertPartCmd.CommandText = "INSERT INTO SongParts (SongID, PartType, PartNumber, DisplayOrder, Lyrics) VALUES ($songId, $partType, $partNumber, $displayOrder, $lyrics)";
                            insertPartCmd.Parameters.AddWithValue("$songId", newSongId);
                            insertPartCmd.Parameters.AddWithValue("$partType", part.PartType);
                            insertPartCmd.Parameters.AddWithValue("$partNumber", part.PartNumber);
                            insertPartCmd.Parameters.AddWithValue("$displayOrder", i); // Use the loop index for DisplayOrder
                            insertPartCmd.Parameters.AddWithValue("$lyrics", part.Lyrics);
                            insertPartCmd.ExecuteNonQuery();
                        }

                        // If  successful, commit the transaction
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        // If anything  wrong, roll back the entire transaction
                        transaction.Rollback();
                        throw; 
                    }
                }
            }
        }

        /// <summary>
        /// Loads all songs,- parts, and their authors from the database.
        /// </summary>
        /// <returns>A list of fully populated Song objects.</returns>
        public List<Song> GetAllSongs()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Step 1: Load all songs into a dictionary for quick lookups by SongID.
                var songs = LoadAllSongs(connection);

                // Step 2: Load all authors into a dictionary.
                var authors = LoadAllAuthors(connection);

                // Step 3: Load all song parts.
                LoadAndAssignSongParts(connection, songs);

                // Step 4: Link songs to their authors.
                LinkAuthorsToSongs(connection, songs, authors);

                // Return the final, fully populated list of songs.
                return songs.Values.OrderBy(s => s.SongID).ToList();
            }
        }

        private Dictionary<int, Song> LoadAllSongs(SqliteConnection connection)
        {
            var songsDictionary = new Dictionary<int, Song>();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT SongID, Title, Key, BibleVerse FROM Songs";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var song = new Song
                    {
                        SongID = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Key = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        BibleVerse = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                    };
                    songsDictionary[song.SongID] = song;
                }
            }
            return songsDictionary;
        }

        private Dictionary<int, Author> LoadAllAuthors(SqliteConnection connection)
        {
            var authorsDictionary = new Dictionary<int, Author>();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT AuthorID, Name FROM Authors";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var author = new Author
                    {
                        AuthorID = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    };
                    authorsDictionary[author.AuthorID] = author;
                }
            }
            return authorsDictionary;
        }

        private void LoadAndAssignSongParts(SqliteConnection connection, Dictionary<int, Song> songs)
        {
            var command = connection.CreateCommand();
            //  order by DisplayOrder to ensure lyrics are in the correct sequence.
            command.CommandText = "SELECT PartID, SongID, PartType, PartNumber, DisplayOrder, Lyrics FROM SongParts ORDER BY DisplayOrder";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int songId = reader.GetInt32(1);
                    if (songs.TryGetValue(songId, out var song))
                    {
                        song.Parts.Add(new SongPart
                        {
                            PartID = reader.GetInt32(0),
                            PartType = reader.GetString(2),
                            PartNumber = reader.GetInt32(3),
                            DisplayOrder = reader.GetInt32(4),
                            Lyrics = reader.GetString(5)
                        });
                    }
                }
            }
        }
       
        private void LinkAuthorsToSongs(SqliteConnection connection, Dictionary<int, Song> songs, Dictionary<int, Author> authors)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT SongID, AuthorID FROM SongAuthors_Link";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int songId = reader.GetInt32(0);
                    int authorId = reader.GetInt32(1);

                    if (songs.TryGetValue(songId, out var song) && authors.TryGetValue(authorId, out var author))
                    {
                        song.Authors.Add(author);
                    }
                }
            }
        }
    }
}
