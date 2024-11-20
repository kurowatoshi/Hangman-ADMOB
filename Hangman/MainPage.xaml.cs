﻿using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Plugin.Maui.Audio;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Hangman
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {

        // Declare the audio player instance and timer
        private IAudioPlayer _audioPlayer;
        private Timer _loopTimer;

        #region UI Properties
        public string Spotlight
        {
            get => spotlight; set
            {
                spotlight = value;
                OnPropertyChanged();
            }
        }

        public List<char> Letters
        {
            get => letters;
            set
            {
                letters = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => message; set
            {
                message = value;
                OnPropertyChanged();
            }
        }

        public string GameStatus
        {
            get => gameStatus; set
            {
                gameStatus = value;
                Debug.WriteLine($"GameStatus updated: {gameStatus}");  // Debug output
                OnPropertyChanged();
            }
        }

        public string CurrentImage
        {
            get => currentImage;
            set
            {
                currentImage = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Fields
        Dictionary<string, List<string>> categoryWords = new Dictionary<string, List<string>>()
        {
            { "Animals", new List<string> { "elephant", "tiger", "giraffe", "koala", "zebra", "kangaroo" } },
            { "Fruits", new List<string> { "apple", "banana", "orange", "kiwi", "mango", "grape" } },
            { "Countries", new List<string> { "india", "china", "france", "brazil", "canada", "germany" } },
            { "Cities", new List<string> { "paris", "london", "tokyo", "sydney", "newyork", "berlin" } },
            { "Movies", new List<string> { "inception", "titanic", "avatar", "gladiator", "joker", "batman" } }
        };

        List<string> words = new List<string>();
        string answer = "";
        private string spotlight;
        List<char> guessed = new List<char>();
        private List<char> letters = new List<char>();
        private string message;
        int mistakes = 0;
        int maxWrong = 7;
        private string gameStatus;
        private string currentImage = "hang1.png";
        #endregion

        public MainPage()
        {
            InitializeComponent();
            Letters.AddRange("abcdefghijklmnopqrstuvwxyz");
            BindingContext = this;
            CategoryPicker.SelectedIndex = 0; // Set default category to Animals
            PickWord(); // Pick a word from the selected category
            CalculateWord(answer, guessed);

            // Set a default value for GameStatus so it shows on the UI
            GameStatus = "Errors: 0 of 7";
        }

        private void OnCategoryChanged(object sender, EventArgs e)
        {
            // Get the selected category
            var selectedCategory = CategoryPicker.SelectedItem.ToString();
            words = categoryWords[selectedCategory];  // Update word list based on category
            PickWord();  // Pick a new word from the selected category
            CalculateWord(answer, guessed);  // Update Spotlight with the new word
            mistakes = 0;  // Reset the mistakes counter
            CurrentImage = "hang1.png";  // Reset the image
            Message = "";  // Clear any previous message
            UpdateStatus();  // Update the status
            EnableLetters();  // Re-enable letter buttons
        }

        #region Game Engine
        private void PickWord()
        {
            answer = words[new Random().Next(0, words.Count)];
            Debug.WriteLine($"Picked word: {answer}");
        }

        private void CalculateWord(string answer, List<char> guessed)
        {
            var temp = answer.Select(x => (guessed.IndexOf(x) >= 0 ? x : '_')).ToArray();
            Spotlight = string.Join(' ', temp);
        }
        #endregion

        private void Button_Clicked(object sender, EventArgs e)
        {
            // Reset the game when the button is clicked
            mistakes = 0;
            guessed = new List<char>();
            CurrentImage = "hang1.png";
            PickWord();
            CalculateWord(answer, guessed);
            Message = "";
            UpdateStatus();
            EnableLetters();
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                var letter = btn.Text;
                btn.IsEnabled = false;
                HandleGuess(letter[0]);
            }
        }

        private void HandleGuess(char letter)
        {
            if (guessed.IndexOf(letter) == -1)
            {
                guessed.Add(letter);
            }

            if (answer.IndexOf(letter) >= 0)
            {
                CalculateWord(answer, guessed);
                CheckIfGameWon();
            }
            else
            {
                mistakes++;
                UpdateStatus();
                CheckIfGameLost();
                CurrentImage = $"hang{mistakes}.png";
            }
        }

        private void CheckIfGameWon()
        {
            if (Spotlight.Replace(" ", "") == answer)
            {
                Message = "You Win!";
                ShowGameResultPopup("You Win!", answer);
                DisableLetters();
            }
        }

        private void UpdateStatus()
        {
            GameStatus = $"Errors: {mistakes} of {maxWrong}";
        }

        private void CheckIfGameLost()
        {
            if (mistakes == maxWrong)
            {
                Message = "You Lost!!";
                ShowGameResultPopup("You Lost!!", answer);
                DisableLetters();
            }
        }

        private void DisableLetters()
        {
            foreach (var children in LettersContainer.Children)
            {
                var btn = children as Button;
                if (btn != null)
                {
                    btn.IsEnabled = false;
                }
            }
        }

        private void EnableLetters()
        {
            foreach (var children in LettersContainer.Children)
            {
                var btn = children as Button;
                if (btn != null)
                {
                    btn.IsEnabled = true;
                }
            }
        }

        #region Hint Button Logic

        private void HintButton_Clicked(object sender, EventArgs e)
        {
            // Only allow hint if there are enough errors left
            if (mistakes + 3 <= maxWrong)
            {
                mistakes += 3;  // Deduct 3 mistakes for using the hint
                UpdateStatus();

                // Reveal one of the hidden letters (random letter not yet guessed)
                var remainingLetters = answer.Where(c => !guessed.Contains(c)).ToList();
                if (remainingLetters.Any())
                {
                    var hintLetter = remainingLetters[new Random().Next(remainingLetters.Count)];
                    guessed.Add(hintLetter);  // Add the hint letter to the guessed list
                    CalculateWord(answer, guessed);  // Update Spotlight
                }

                // Update the image based on the new number of mistakes after using the hint
                CurrentImage = $"hang{mistakes}.png";
            }
            else
            {
                Message = "Not enough errors left for a hint!";
            }
        }

        private async void ShowGameResultPopup(string resultMessage, string correctAnswer)
        {
            // Combine the result message with the correct answer
            var popupMessage = $"{resultMessage}\nThe correct answer was: {correctAnswer}";

            var popup = new Toast
            {
                Text = popupMessage,
                Duration = ToastDuration.Short
            };

            await popup.Show();  // Show the popup message
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion


        private async void PlayBackgroundMusic()
        {
            // Get the music file from resources
            var file = await FileSystem.OpenAppPackageFileAsync("Resources/Audio/backgroundmusic.mp3");

            // Create the audio player
            _audioPlayer = AudioManager.Current.CreatePlayer(file);

            // Play the music
            _audioPlayer.Play();

            // Start a timer to check if the music has finished
            _loopTimer = new Timer(CheckAudioPlayback, null, 0, 1000);  // Check every second
        }

        // Method to check if the audio has finished
        private void CheckAudioPlayback(object state)
        {
            // The Plugin.Maui.Audio does not expose the state, so we rely on the time position check instead
            // If the audio has finished playing, restart it
            if (_audioPlayer.CurrentPosition >= _audioPlayer.Duration)
            {
                _audioPlayer.Play(); // Restart the music
            }
        }

        private void StopBackgroundMusic()
        {
            // Stop the music and dispose of the timer
            _audioPlayer?.Stop();
            _loopTimer?.Dispose();
        }
    }
}
