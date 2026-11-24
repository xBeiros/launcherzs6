using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// Repräsentiert einen einzelnen Beitrag auf der Neuigkeiten-Seite.
    /// </summary>
    public sealed class NewsArticleItem
    {
        public string Category { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string ImageUri { get; }
        public string PublishedDate { get; }
        public string FullText { get; }

        public NewsArticleItem(
            string category,
            string title,
            string subtitle,
            string imageUri,
            string publishedDate,
            string fullText)
        {
            Category = category;
            Title = title;
            Subtitle = subtitle;
            ImageUri = imageUri;
            PublishedDate = publishedDate;
            FullText = fullText;
        }
    }

    /// <summary>
    /// ViewModel für den Neuigkeiten-Tab im ZODIAC SIX Launcher.
    /// </summary>
    public sealed class NewsViewModel : ObservableObject
    {
        private NewsArticleItem? _selectedArticle;

        public NewsViewModel()
        {
            Articles = new ObservableCollection<NewsArticleItem>
            {
                new(
                    "VISION · KONZEPTEINBLICK",
                    "Eine Stadt. Unendlich viele Wege.",
                    "Mehr über unsere Vision erfahren.",
                    "pack://application:,,,/Assets/social_4.png",
                    "ZODIACSIX · LEONIDA",
                    "Leonida ist keine statische Kulisse, sondern eine lebendige Metropole voller Konsequenzen, Geschichten und Möglichkeiten. Vom ersten Schritt auf dem Ocean Drive bis zu langfristigen Storylines bestimmen echte Entscheidungen deiner Spielfigur die Entwicklung der Stadt."),
                new(
                    "FRAKTIONEN · KONZEPTEINBLICK",
                    "Für welche Seite entscheidest du dich?",
                    "Mehr über unsere Vision erfahren.",
                    "pack://application:,,,/Assets/social_3.png",
                    "ZODIACSIX · FRAKTIONEN",
                    "Gesetz, Unterwelt oder unabhängige Gruppierung – jede Fraktion auf ZODIACSIX besitzt eigene Strukturen, Einflussgebiete und Dynamiken. Ob im Dienst für Ordnung in Vice City oder in den Schatten der Nacht: Deine Loyalität prägt deinen Ruf."),
                new(
                    "WIRTSCHAFT · KONZEPTEINBLICK",
                    "Von der Idee zum Imperium.",
                    "Mehr über unsere Vision erfahren.",
                    "pack://application:,,,/Assets/social_1.png",
                    "ZODIACSIX · WIRTSCHAFT",
                    "Ein spielergetriebenes Wirtschaftssystem bildet das Rückgrat von ZODIACSIX. Starte mit einer ersten Geschäftsidee, baue Lieferketten auf, stelle Personal ein und entwickle dein Unternehmen Schritt für Schritt zu einer festen Größe in der Stadt.")
            };

            OpenArticleCommand = new RelayCommand(param =>
            {
                if (param is NewsArticleItem article)
                {
                    SelectedArticle = article;
                }
            });

            CloseArticleCommand = new RelayCommand(() => SelectedArticle = null);
        }

        public ObservableCollection<NewsArticleItem> Articles { get; }

        public NewsArticleItem? SelectedArticle
        {
            get => _selectedArticle;
            set
            {
                if (SetProperty(ref _selectedArticle, value))
                {
                    OnPropertyChanged(nameof(HasSelectedArticle));
                }
            }
        }

        public bool HasSelectedArticle => SelectedArticle is not null;

        public ICommand OpenArticleCommand { get; }
        public ICommand CloseArticleCommand { get; }
    }
}
