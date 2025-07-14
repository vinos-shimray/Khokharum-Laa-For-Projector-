using KhokharumLaa.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KhokharumLaa.ViewModels
{
    public class AddSongViewModel : BaseViewModel
    {
        private string _title;
        private SongPart _selectedPart;

        // The Song object that will be built and returned
        public Song NewSong { get; private set; }
        public ObservableCollection<SongPart> Parts { get; set; }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public SongPart SelectedPart
        {
            get => _selectedPart;
            set { _selectedPart = value; OnPropertyChanged(); }
        }

        public ICommand AddPartCommand { get; }
        public ICommand RemovePartCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }

        public AddSongViewModel()
        {
            NewSong = new Song();
            Parts = new ObservableCollection<SongPart>();
            Title = "New Song Title";

            AddPartCommand = new RelayCommand(AddPart);
            RemovePartCommand = new RelayCommand(RemovePart, CanRemovePart);
            SaveCommand = new RelayCommand(Save, CanSave);
            CloseCommand = new RelayCommand(p => { /* Logic handled by the window */ });
        }

        private bool CanSave(object parameter)
        {
            // Can only save if there's a title and at least one song part
            return !string.IsNullOrWhiteSpace(Title) && Parts.Any();
        }

        private void Save(object parameter)
        {
            NewSong.Title = this.Title;
            NewSong.Parts = this.Parts.ToList();
            // The window will check this property after closing
        }

        private bool CanRemovePart(object parameter)
        {
            return SelectedPart != null;
        }

        private void RemovePart(object parameter)
        {
            if (SelectedPart != null)
            {
                Parts.Remove(SelectedPart);
                RenumberParts();
            }
        }

        private void AddPart(object parameter)
        {
            // UPDATED: Read the part type from the command parameter
            string partType = parameter as string;
            if (string.IsNullOrEmpty(partType))
            {
                partType = "Verse"; // Default to "Verse" if no parameter is passed
            }

            var newPart = new SongPart
            {
                PartType = partType,
                Lyrics = $"New {partType.ToLower()} lyrics..."
            };
            Parts.Add(newPart);
            RenumberParts(); // Renumber after adding
            SelectedPart = newPart;
        }

        private void RenumberParts()
        {
            int verseCount = 1;
            int chorusCount = 1;
            // This can be expanded to handle other part types like Bridge, etc.
            foreach (var part in Parts)
            {
                if (part.PartType == "Verse")
                {
                    part.PartNumber = verseCount++;
                }
                else if (part.PartType == "Chorus")
                {
                    part.PartNumber = chorusCount++;
                }
            }
        }
    }
}
