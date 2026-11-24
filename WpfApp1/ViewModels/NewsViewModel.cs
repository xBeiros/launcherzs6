using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// Repräsentiert einen einzelnen Beitrag auf der Neuigkeiten-Seite.
    /// </summary>
    public sealed class NewsArticleItem
    {
        public string NumberLabel { get; }
        public string FilterKey { get; }
        public string Category { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string ImageUri { get; }
        public string PublishedDate { get; }
        public string ReadTime { get; }
        public string HighlightQuote { get; }
        public string FullText { get; }

        public NewsArticleItem(
            string numberLabel,
            string filterKey,
            string category,
            string title,
            string subtitle,
            string imageUri,
            string publishedDate,
            string readTime,
            string highlightQuote,
            string fullText)
        {
            NumberLabel = numberLabel;
            FilterKey = filterKey;
            Category = category;
            Title = title;
            Subtitle = subtitle;
            ImageUri = imageUri;
            PublishedDate = publishedDate;
            ReadTime = readTime;
            HighlightQuote = highlightQuote;
            FullText = fullText;
        }
    }

    /// <summary>
    /// ViewModel für den Neuigkeiten-Tab im ZODIAC SIX Launcher mit Kategorie-Filter & Detail-Navigation.
    /// </summary>
    public sealed class NewsViewModel : ObservableObject
    {
        private readonly List<NewsArticleItem> _allArticles;
        private NewsArticleItem? _selectedArticle;
        private string _selectedFilter = "Alle";

        public NewsViewModel()
        {
            _allArticles = new List<NewsArticleItem>
            {
                new(
                    "01",
                    "Vision",
                    "VISION · KONZEPTEINBLICK",
                    "Eine Stadt. Unendlich viele Wege.",
                    "Wie wir Leonida als lebendige, konsequenzgetriebene Roleplay-Welt aufbauen.",
                    "pack://application:,,,/Assets/social_4.png",
                    "ZODIACSIX · LEONIDA",
                    "2 MIN LESEZEIT",
                    "„Jede Entscheidung hinterlässt Spuren – vom Ocean Drive bis in die hintersten Gassen der Stadt.“",
                    "Leonida ist keine statische Kulisse, sondern eine lebendige Metropole voller Konsequenzen, Geschichten und Möglichkeiten. Vom ersten Schritt auf dem Ocean Drive bis zu langfristigen Storylines bestimmen echte Entscheidungen deiner Spielfigur die Entwicklung der Stadt.\n\nUnser Fokus liegt auf tiefgehendem Midcore-Roleplay, klaren Systemen ohne Ballast und einer lebendigen Community, in der neue Spieler genauso schnell Anschluss finden wie erfahrene Rollenspieler."),
                new(
                    "02",
                    "Fraktionen",
                    "FRAKTIONEN · KONZEPTEINBLICK",
                    "Für welche Seite entscheidest du dich?",
                    "Gesetz, Unterwelt oder eigene Gruppierung – echte Machtverhältnisse in Vice City.",
                    "pack://application:,,,/Assets/social_3.png",
                    "ZODIACSIX · FRAKTIONEN",
                    "3 MIN LESEZEIT",
                    "„Loyalität hat ihren Preis – und dein Ruf eilt dir in jedem Bezirk voraus.“",
                    "Gesetz, Unterwelt oder unabhängige Gruppierung – jede Fraktion auf ZODIACSIX besitzt eigene Strukturen, Einflussgebiete und Dynamiken. Ob im Dienst für Ordnung in Vice City oder in den Schatten der Nacht: Deine Loyalität prägt deinen Ruf.\n\nStatt starrer Skripte setzen wir auf dynamische Interaktionen zwischen Staatsfraktionen, Syndikaten und Zivilisten, sodass Konflikte und Bündnisse organisch aus dem RP heraus entstehen."),
                new(
                    "03",
                    "Wirtschaft",
                    "WIRTSCHAFT · KONZEPTEINBLICK",
                    "Von der Idee zum Imperium.",
                    "Spielergetriebene Unternehmen, Lieferketten und echte Karrierewege in Leonida.",
                    "pack://application:,,,/Assets/social_1.png",
                    "ZODIACSIX · WIRTSCHAFT",
                    "2 MIN LESEZEIT",
                    "„Starte klein, baue dein Netzwerk auf und präge die Skyline von Leonida.“",
                    "Ein spielergetriebenes Wirtschaftssystem bildet das Rückgrat von ZODIACSIX. Starte mit einer ersten Geschäftsidee, baue Lieferketten auf, stelle Personal ein und entwickle dein Unternehmen Schritt für Schritt zu einer festen Größe in der Stadt.\n\nVom ersten Nebenjob bis zur eigenen Firma mit Mitarbeitern greifen Handel, Handwerk und Dienstleistungen nahtlos ineinander.")
            };

            Articles = new ObservableCollection<NewsArticleItem>(_allArticles);

            SelectFilterCommand = new RelayCommand(param =>
            {
                if (param is string filter)
                {
                    SelectedFilter = filter;
                }
            });

            OpenArticleCommand = new RelayCommand(param =>
            {
                if (param is NewsArticleItem article)
                {
                    SelectedArticle = article;
                }
            });

            CloseArticleCommand = new RelayCommand(() => SelectedArticle = null);

            NextArticleCommand = new RelayCommand(() =>
            {
                if (SelectedArticle is null || _allArticles.Count == 0)
                {
                    return;
                }

                var currentIdx = _allArticles.IndexOf(SelectedArticle);
                var nextIdx = (currentIdx + 1) % _allArticles.Count;
                SelectedArticle = _allArticles[nextIdx];
            });
        }

        public ObservableCollection<NewsArticleItem> Articles { get; }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    ApplyFilter();
                    OnPropertyChanged(nameof(IsAlleFilter));
                    OnPropertyChanged(nameof(IsVisionFilter));
                    OnPropertyChanged(nameof(IsFraktionenFilter));
                    OnPropertyChanged(nameof(IsWirtschaftFilter));
                }
            }
        }

        public bool IsAlleFilter => SelectedFilter == "Alle";
        public bool IsVisionFilter => SelectedFilter == "Vision";
        public bool IsFraktionenFilter => SelectedFilter == "Fraktionen";
        public bool IsWirtschaftFilter => SelectedFilter == "Wirtschaft";

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

        public ICommand SelectFilterCommand { get; }
        public ICommand OpenArticleCommand { get; }
        public ICommand CloseArticleCommand { get; }
        public ICommand NextArticleCommand { get; }

        private void ApplyFilter()
        {
            Articles.Clear();
            var filtered = SelectedFilter == "Alle"
                ? _allArticles
                : _allArticles.Where(a => a.FilterKey.Equals(SelectedFilter, StringComparison.OrdinalIgnoreCase));

            foreach (var item in filtered)
            {
                Articles.Add(item);
            }
        }
    }
}
